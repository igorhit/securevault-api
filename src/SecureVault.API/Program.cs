using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using SecureVault.API.Extensions;
using SecureVault.API.Infrastructure;
using SecureVault.API.Middleware;
using SecureVault.Application;
using SecureVault.Infrastructure;
using SecureVault.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var appRoot = builder.Configuration["Storage:RuntimePath"] ?? Directory.GetCurrentDirectory();
Directory.CreateDirectory(appRoot);

// Gera o .env com valores seguros se não existir, depois carrega as variáveis.
// Nos testes de integração, a configuração é injetada pela WebApplicationFactory.
if (!builder.Environment.IsEnvironment("Testing"))
{
    EnvBootstrap.EnsureEnvFileExists(appRoot);
    EnvBootstrap.LoadEnvFile(appRoot);
}

// Injeta as variáveis de ambiente no IConfiguration para que os serviços as leiam normalmente
builder.Configuration.AddEnvironmentVariables();

// Serilog: logging estruturado
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Camadas da aplicação
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Rate limiting: proteção contra brute force e DDoS no nível da aplicação
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Executa migrations automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// OWASP Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    var isSwaggerUiRequest =
        context.Request.Path == "/" ||
        context.Request.Path.Equals("/index.html", StringComparison.OrdinalIgnoreCase);

    context.Response.Headers["Content-Security-Policy"] = isSwaggerUiRequest
        ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:;"
        : "default-src 'self'";

    await next();
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseIpRateLimiting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecureVault API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:5000
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para referenciar o Program nos testes de integração
public partial class Program { }
