using Konscious.Security.Cryptography;
using SecureVault.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.Infrastructure.Security;

// Argon2id é o algoritmo recomendado pelo OWASP para hash de senhas (2023).
// Vencedor do Password Hashing Competition, resistente a ataques de GPU e side-channel.
// Parâmetros calibrados para ~300ms em hardware moderno (equilíbrio entre segurança e UX).
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128 bits
    private const int HashSize = 32;       // 256 bits
    private const int Iterations = 4;      // degree of parallelism
    private const int MemorySize = 65536;  // 64 MB
    private const int DegreeOfParallelism = 2;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        // Formato: base64(salt):base64(hash)
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = ComputeHash(password, salt);

            // Comparação em tempo constante para evitar timing attacks
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            Iterations = Iterations,
            MemorySize = MemorySize,
            DegreeOfParallelism = DegreeOfParallelism
        };

        return argon2.GetBytes(HashSize);
    }
}
