using System.Security.Cryptography;

namespace SecureVault.API.Infrastructure;

// Gera o arquivo .env com valores seguros se ele não existir.
// Isso garante que "git clone + dotnet run" funciona sem nenhuma configuração manual.
//
// Convenção de nomes:
//   Jwt__Secret      → IConfiguration["Jwt:Secret"]      (__ = separador de seção no .NET)
//   Encryption__Key  → IConfiguration["Encryption:Key"]
public static class EnvBootstrap
{
    public static void EnsureEnvFileExists(string basePath)
    {
        Directory.CreateDirectory(basePath);

        var envPath = Path.Combine(basePath, ".env");
        if (File.Exists(envPath)) return;

        // Gera chaves criptograficamente seguras
        var jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var encryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var dbPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            .Replace("+", "X").Replace("/", "Y").Replace("=", "")[..16];

        var content = $"""
            # Gerado automaticamente em {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC
            # NÃO commitar — está no .gitignore
            # Para regenerar: delete este arquivo e reinicie.

            # Usado pelo Docker Compose (PostgreSQL)
            POSTGRES_PASSWORD={dbPassword}

            # Mapeados para IConfiguration["Jwt:Secret"] e IConfiguration["Encryption:Key"]
            # O duplo underscore (__) é a convenção do .NET para separador de seção em env vars
            Jwt__Secret={jwtSecret}
            Encryption__Key={encryptionKey}
            """;

        File.WriteAllText(envPath, content);
        Console.WriteLine($"[Setup] .env gerado com chaves seguras em: {envPath}");
    }

    public static void LoadEnvFile(string basePath)
    {
        var envPath = Path.Combine(basePath, ".env");
        if (!File.Exists(envPath)) return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Não sobrescreve variáveis já definidas no ambiente (ex: variáveis do Docker)
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
