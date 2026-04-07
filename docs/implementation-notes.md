# Notas de Implementação — SecureVault API

Este documento registra problemas reais encontrados durante o desenvolvimento, como cada um foi solucionado e por que a solução escolhida foi a mais adequada.

O objetivo é tornar o processo de desenvolvimento transparente: qualquer desenvolvedor experiente sabe que esses problemas são esperados e a forma de resolvê-los diz mais sobre a qualidade do trabalho do que a ausência deles.

---

## 1. Connection string PostgreSQL sobrescrevendo SQLite em Development

**Problema**

O arquivo `appsettings.Development.json` tinha uma `ConnectionStrings.DefaultConnection` apontando para `Host=db;Database=securevault;...` (sobra de uma versão anterior que usava PostgreSQL). Como o ASP.NET Core aplica os arquivos de configuração por ordem de prioridade — `appsettings.json` primeiro, depois `appsettings.{Environment}.json` —, o arquivo de desenvolvimento sobrescrevia silenciosamente a connection string correta do SQLite.

**O que causava**

`dotnet run` iniciava, mas falhava na primeira operação do EF Core com `Connection string keyword 'host' is not supported`. O SQLite não reconhece esse formato de connection string. O erro só aparecia em runtime, após o startup parecer normal.

**Como foi solucionado**

A seção `ConnectionStrings` foi removida completamente do `appsettings.Development.json`. Esse arquivo passou a ter apenas overrides de Serilog (nível de log mais verboso em Development), que é seu único propósito legítimo.

**Por que essa solução**

A regra é simples: configurações que devem ser iguais em todos os ambientes ficam apenas em `appsettings.json`. Overrides de ambiente só existem quando o comportamento precisa ser diferente. A connection string não muda entre Development e Production neste projeto — ambos usam SQLite local.

---

## 2. `dotnet run` travando em "Compilando..." sem progredir

**Problema**

O build funcionava normalmente com `dotnet build`, mas `dotnet run` ficava parado indefinidamente após compilar, sem subir o servidor nem exibir nenhum erro.

**O que causava**

Dois fatores simultâneos:

1. A connection string PostgreSQL do item anterior fazia a aplicação falhar silenciosamente na inicialização ao tentar aplicar migrations.
2. O `launchSettings.json` tinha `launchBrowser: true` com `launchUrl: "weatherforecast"` — rota que havia sido deletada. O processo do dotnet run ficava esperando o navegador abrir antes de reportar o estado da aplicação.

**Como foi solucionado**

A connection string foi corrigida (item 1). O `launchSettings.json` foi ajustado para `launchBrowser: false`, a `launchUrl` foi removida e a porta foi fixada em `http://localhost:5000`.

**Por que essa solução**

`launchBrowser: true` faz sentido durante desenvolvimento ativo de uma interface web, mas cria comportamento confuso em uma API pura — o processo trava esperando um browser abrir uma rota que pode não existir. Para portfólio, o comportamento esperado é o servidor subir e logar o endereço no terminal.

---

## 3. PostgreSQL como dependência que quebrava o requisito de "clone + um comando"

**Problema**

A versão inicial do projeto usava PostgreSQL, exigindo um servidor de banco rodando antes de qualquer teste ou execução. Isso quebrava diretamente o objetivo do portfólio: qualquer recrutador que clonasse o projeto precisaria instalar e configurar um servidor de banco antes de conseguir rodar qualquer coisa.

**O que causava**

A aplicação falhava imediatamente ao tentar conectar sem que houvesse um servidor PostgreSQL disponível. Não havia mensagem de erro útil — apenas timeout ou connection refused.

**Como foi solucionado**

PostgreSQL foi substituído por SQLite. A troca foi uma única linha no `DependencyInjection.cs`:

```csharp
// antes
options.UseNpgsql(connectionString);

// depois
options.UseSqlite(connectionString, b => b.MigrationsAssembly("..."));
```

Todos os pacotes do Npgsql, Testcontainers e referências a PostgreSQL no `docker-compose.yml` foram removidos.

**Por que essa solução**

SQLite é embutido na aplicação: nenhum processo separado, nenhuma instalação, nenhuma credencial. O banco é criado automaticamente como arquivo na primeira execução. O trade-off — abrir mão de simular um banco servidor — é consciente e documentado. Para um projeto de portfólio onde reprodutibilidade local é o requisito principal, SQLite é claramente a escolha certa.

---

## 4. Testes de integração quebrando com SQLite em memória

**Problema**

A `IntegrationTestFactory` original configurava o banco de testes como SQLite em memória (`Data Source=:memory:`). Os testes falhavam com erros relacionados a migrations e a tabelas não encontradas, mesmo quando as migrations eram aplicadas corretamente.

**O que causava**

SQLite em memória em .NET tem um comportamento específico: cada conexão abre um banco diferente. O `WebApplicationFactory` usa o `DbContext` com seu ciclo de vida de injeção de dependência, o que significa que diferentes partes da aplicação podiam acabar olhando para bancos distintos durante o mesmo teste. As migrations criavam as tabelas em uma conexão, mas os repositórios operavam em outra.

**Como foi solucionado**

O banco de testes foi trocado para SQLite em arquivo temporário. Cada instância da factory recebe um nome único via `Guid.NewGuid()`:

```csharp
private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
```

O arquivo é removido no `DisposeAsync` da factory.

**Por que essa solução**

SQLite em arquivo resolve o problema de isolamento de conexão sem adicionar nenhuma dependência externa. O arquivo temporário garante isolamento entre suítes de teste. O custo é mínimo: um arquivo de alguns KB que existe apenas durante a execução dos testes.

---

## 5. `ValidationBehavior` retornando tipo errado para `Result<T>`

**Problema**

O pipeline de validação do MediatR retornava `Result.Fail(errors)` sem tipagem genérica quando o handler esperava `Result<T>`. Isso causava `InvalidCastException` em runtime nos handlers que retornavam valores (como `RegisterCommand → Result<RegisterResponse>`).

**O que causava**

`Result.Fail(errors)` retorna `Result` (não genérico). Quando o pipeline tentava fazer o cast para `Result<RegisterResponse>`, falhava porque os dois tipos não são compatíveis diretamente em FluentResults.

**Como foi solucionado**

O `ValidationBehavior` foi ajustado para detectar em runtime se o tipo de resposta é genérico ou não, e invocar o overload correto de `Result.Fail<T>` via reflection:

```csharp
if (typeof(TResponse) == typeof(Result))
    return (TResponse)(object)Result.Fail(errors);

var valueType = typeof(TResponse).GenericTypeArguments.Single();
var failMethod = typeof(Result).GetMethods()
    .Single(m => m.Name == nameof(Result.Fail) && m.IsGenericMethodDefinition && ...);

return (TResponse)failMethod.MakeGenericMethod(valueType).Invoke(null, new object[] { errors })!;
```

**Por que essa solução**

É o único caminho que respeita o contrato do pipeline do MediatR sem duplicar o `ValidationBehavior` em duas versões (uma para `Result` e outra para `Result<T>`). Reflection tem custo, mas é executado apenas nos caminhos de erro de validação — que são muito menos frequentes que os caminhos de sucesso.

---

## 6. Rate limiting interferindo nos testes de integração

**Problema**

Os testes de integração executavam múltiplos requests em sequência rápida contra o mesmo endpoint, o que disparava as regras de rate limiting configuradas para os endpoints de auth. Testes que deveriam retornar `201` ou `200` começavam a retornar `429`.

**O que causava**

O `AspNetCoreRateLimit` usa a biblioteca com contadores em memória. No ambiente de testes, os contadores não eram resetados entre testes, e a execução rápida dos testes esgotava os limites configurados (ex: 5 registros por hora).

**Como foi solucionado**

O registro do rate limiting passou a ser condicional ao ambiente:

```csharp
if (!isTesting)
{
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(...);
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}
```

**Por que essa solução**

Rate limiting é uma preocupação de infraestrutura de produção, não uma regra de negócio que precisa ser testada nos testes de integração comuns. Os testes cobrem os comportamentos de domínio. Testes específicos de rate limiting — se necessários — deveriam ser isolados e explícitos, não uma consequência acidental de rodar a suíte inteira.

---

## 7. Content-Security-Policy bloqueando o Swagger UI

**Problema**

Depois de adicionar o header `Content-Security-Policy: default-src 'self'` a todas as respostas, o Swagger UI abria em branco no navegador.

**O que causava**

O Swagger UI (Swashbuckle) carrega scripts e estilos inline, além de workers em blob. A CSP `default-src 'self'` bloqueava todos esses recursos porque eles não passavam como scripts externos servidos pelo próprio domínio.

**Como foi solucionado**

A CSP foi tornada condicional por rota. Para `/` e `/index.html` (onde o Swagger UI é servido), a CSP é relaxada para permitir os recursos necessários:

```csharp
if (path == "/" || path == "/index.html")
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; ...";
else
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
```

**Por que essa solução**

O objetivo do Swagger é ser uma interface de validação para recrutadores. Bloquear o Swagger para manter uma CSP rigorosa seria contraproducente para o objetivo do projeto. A solução minimiza a superfície de relaxamento: apenas as rotas do Swagger têm CSP relaxada, todos os endpoints de API continuam com `default-src 'self'`.

---

## 8. `.env` sendo gerado durante os testes de integração

**Problema**

O `EnvBootstrap` era chamado no início de `Program.cs` incondicionalmente. Durante os testes de integração, o `WebApplicationFactory` subia a aplicação em modo `Testing`, mas o bootstrap tentava criar e ler um arquivo `.env` no diretório de execução dos testes.

**O que causava**

Dois sintomas: (1) múltiplas instâncias de `WebApplicationFactory` tentando criar o mesmo arquivo `.env` simultaneamente, gerando `System.IO.IOException: The process cannot access the file because it is being used by another process`; (2) os testes carregavam segredos do `.env` em vez das configurações injetadas pelo factory, causando inconsistências.

**Como foi solucionado**

O bootstrap passou a verificar o ambiente antes de executar:

```csharp
var isTesting = builder.Environment.EnvironmentName == "Testing";

if (!isTesting)
{
    EnvBootstrap.EnsureEnvFileExists(appRoot);
    EnvBootstrap.LoadEnvFile(appRoot);
}
```

**Por que essa solução**

Ambientes de teste precisam de controle total sobre a configuração injetada. O `WebApplicationFactory` já tem um mecanismo (`ConfigureAppConfiguration`) para isso. O `EnvBootstrap` é uma conveniência para o desenvolvedor local e para o container Docker — não deve interferir em nenhum outro contexto.
