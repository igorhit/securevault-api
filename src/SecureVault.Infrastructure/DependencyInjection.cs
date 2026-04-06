using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Domain.Interfaces;
using SecureVault.Infrastructure.Persistence;
using SecureVault.Infrastructure.Persistence.Repositories;
using SecureVault.Infrastructure.Security;
using SecureVault.Infrastructure.Services;
using System.Text;

namespace SecureVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var runtimePath = configuration["Storage:RuntimePath"];
        var defaultConnectionString = string.IsNullOrWhiteSpace(runtimePath)
            ? "Data Source=securevault.db"
            : $"Data Source={Path.Combine(runtimePath, "securevault.db")}";

        // SQLite: banco embutido, zero instalação, zero servidor.
        // O arquivo securevault.db é criado automaticamente na primeira execução.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection") ?? defaultConnectionString,
                b => b.MigrationsAssembly("SecureVault.Infrastructure")
            ));

        // Repositórios e Unit of Work
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Serviços de segurança
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]
                            ?? throw new InvalidOperationException("JWT Secret não configurado."))),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
