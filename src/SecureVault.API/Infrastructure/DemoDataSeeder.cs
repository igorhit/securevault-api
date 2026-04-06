using Microsoft.EntityFrameworkCore;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Interfaces;
using SecureVault.Infrastructure.Persistence;

namespace SecureVault.API.Infrastructure;

public static class DemoDataSeeder
{
    private const string DefaultEmail = "demo@securevault.local";
    private const string DefaultPassword = "Demo@123456";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("DemoData:Enabled"))
            return;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        var db = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var encryptionService = services.GetRequiredService<IEncryptionService>();

        var email = (configuration["DemoData:UserEmail"] ?? DefaultEmail).Trim().ToLowerInvariant();
        var password = configuration["DemoData:UserPassword"] ?? DefaultPassword;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        var changesMade = false;

        if (user is null)
        {
            user = User.Create(email, passwordHasher.Hash(password));
            await db.Users.AddAsync(user, cancellationToken);
            changesMade = true;
        }

        var demoCredentials = new[]
        {
            new
            {
                Title = "GitHub Demo",
                Url = "https://github.com",
                Username = "demo-user",
                Password = "ghp_demo_token_123",
                Notes = "Credencial de demonstração para validar o fluxo completo da API."
            },
            new
            {
                Title = "Azure Portal Demo",
                Url = "https://portal.azure.com",
                Username = "demo@securevault.local",
                Password = "AzureDemo!2026",
                Notes = "Exemplo adicional para listar, buscar, atualizar e remover."
            }
        };

        foreach (var item in demoCredentials)
        {
            var exists = await db.Credentials
                .AnyAsync(c => c.UserId == user.Id && c.Title == item.Title, cancellationToken);

            if (exists)
                continue;

            var credential = Credential.Create(
                user.Id,
                item.Title,
                encryptionService.Encrypt(item.Username),
                encryptionService.Encrypt(item.Password),
                item.Url,
                encryptionService.Encrypt(item.Notes));

            await db.Credentials.AddAsync(credential, cancellationToken);
            changesMade = true;
        }

        if (!changesMade)
        {
            logger.LogInformation("Demo data já existia no banco. Seed ignorado.");
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo data aplicada com sucesso. Usuário demo: {Email}", email);
    }
}
