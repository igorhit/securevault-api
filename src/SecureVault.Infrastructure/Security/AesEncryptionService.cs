using Microsoft.Extensions.Configuration;
using SecureVault.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.Infrastructure.Security;

// AES-256-GCM fornece criptografia autenticada (AEAD):
// - Confidencialidade: ninguém lê sem a chave
// - Integridade: qualquer alteração no ciphertext é detectada (authentication tag)
// - Nonce único por operação evita ataques de replay e análise de padrões
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private const int NonceSize = 12;   // 96 bits — recomendado para GCM
    private const int TagSize = 16;     // 128 bits

    public AesEncryptionService(IConfiguration configuration)
    {
        var base64Key = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Chave de criptografia não configurada.");

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new InvalidOperationException("Chave de criptografia deve ter 256 bits (32 bytes).");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Formato: base64(nonce + ciphertext + tag)
        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextBase64)
    {
        var data = Convert.FromBase64String(ciphertextBase64);

        var nonce = data[..NonceSize];
        var tag = data[^TagSize..];
        var ciphertext = data[NonceSize..^TagSize];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
