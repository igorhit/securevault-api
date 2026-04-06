using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureVault.Infrastructure.Persistence;

namespace SecureVault.IntegrationTests;

// WebApplicationFactory com SQLite temporário em arquivo.
// Cada instância tem seu próprio banco isolado — sem estado compartilhado entre test classes.
// Não precisa de Docker, serviços externos ou configuração.
public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    // Arquivo temporário por factory evita problemas de ciclo de vida do SQLite em memória.
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"securevault-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test_secret_key_at_least_32_chars_long!!",
                ["Jwt:Issuer"] = "SecureVaultAPI",
                ["Jwt:Audience"] = "SecureVaultClients",
                ["Encryption:Key"] = "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Substitui o DbContext por um SQLite em memória
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={_dbPath}",
                    sqlite => sqlite.MigrationsAssembly("SecureVault.Infrastructure")));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !File.Exists(_dbPath))
        {
            return;
        }

        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // O arquivo temporário pode permanecer bloqueado por pouco tempo no encerramento do host de teste.
        }
    }
}
