# SecureVault API — Contexto do Projeto

> **Instrução para agentes IA:** Leia este arquivo no início de toda sessão. Ao final de toda sessão ou ao fazer mudanças significativas, atualize as seções relevantes antes de encerrar.

---

## Desenvolvedor

- Fullstack júnior, foco em backend e cibersegurança (cibersec em fase inicial)
- Stack: .NET 8 C# (backend), Next.js (frontend), SQLite/PostgreSQL, Python (automações)
- Comunicação em português brasileiro, código em inglês

## Objetivo do portfólio

Projetos GitHub que funcionam com `git clone + um comando`, sem nenhuma configuração manual. Recrutadores devem conseguir rodar, testar e entender as decisões arquiteturais sem perguntar nada ao desenvolvedor.

---

## Status atual

**Build:** compilando, 0 erros  
**Testes unitários:** 7/7 passando  
**Testes de integração:** 10/10 passando  
**API rodando:** confirmado em 2026-04-06 com `dotnet run`  
**Health check:** `GET /health` → `200 Healthy`  
**Swagger:** disponível na raiz `http://localhost:5000/`
**Teste manual no Swagger:** validado em 2026-04-06 (auth + vault)
**Docker Compose:** arquivos criados e `docker compose config` validado; `docker compose up` ainda não executado nesta sessão porque o daemon Docker local estava desligado
**Git local:** repositório inicializado em `main` com commit inicial criado

### Último problema investigado
Pendências validadas e corrigidas nesta sessão:
1. `appsettings.Development.json` tinha connection string PostgreSQL sobrescrevendo SQLite → removida
2. `launchSettings.json` tinha `launchBrowser: true` para rota `/weatherforecast` (deletada) → corrigido para `launchBrowser: false`, porta `http://localhost:5000`
3. `Program.cs` gerava/carregava `.env` também nos testes de integração → agora ignora bootstrap de `.env` no ambiente `Testing`
4. `IntegrationTestFactory` usava SQLite em memória incompatível com o ciclo de vida do `WebApplicationFactory` + migrations → trocado para SQLite temporário em arquivo
5. Chave `Encryption:Key` dos testes tinha tamanho inválido → corrigida para 32 bytes reais
6. `ValidationBehavior` retornava `Result.Fail(...)` não tipado para `Result<T>` → corrigido para usar o overload genérico correto
7. Rate limiting interferia na suíte de integração → desabilitado no ambiente de testes
8. Swagger podia abrir em branco no navegador por causa do header `Content-Security-Policy: default-src 'self'` aplicado à UI → CSP relaxada apenas para `/` e `/index.html`
9. Projeto foi alinhado ao requisito de portfólio com `Dockerfile`, `docker-compose.yml`, `.env.example` e documentação atualizada
10. `.gitignore` passou a ignorar arquivos SQLite locais (`*.db`, `*.db-shm`, `*.db-wal`) para evitar commit acidental do banco
11. Repositório Git local foi inicializado com commit inicial semântico

**Próxima ação:** publicar no GitHub, validar `docker compose up` com o daemon ativo e atualizar badge do CI com a URL real do repositório

---

## Como rodar

```bash
cd "C:\Users\User\Desktop\Coding\Projects\SecureVaultApi\src\SecureVault.API"
dotnet run
```

- Na primeira execução: `.env` gerado automaticamente com chaves seguras
- Banco SQLite `securevault.db` criado automaticamente
- Migrations aplicadas automaticamente
- Swagger em `http://localhost:5000`

```bash
# Rodar todos os testes
cd "C:\Users\User\Desktop\Coding\Projects\SecureVaultApi"
dotnet test
```

---

## Estrutura

```
SecureVaultApi/
├── src/
│   ├── SecureVault.Domain/           # Entidades, interfaces, DomainErrors
│   ├── SecureVault.Application/      # CQRS handlers, validators, DTOs, ValidationBehavior
│   ├── SecureVault.Infrastructure/   # EF Core + SQLite, repositórios, Argon2, AES, JWT
│   └── SecureVault.API/              # Controllers, middlewares, Program.cs, EnvBootstrap
├── tests/
│   ├── SecureVault.UnitTests/        # 7 testes (NSubstitute + FluentAssertions)
│   └── SecureVault.IntegrationTests/ # SQLite temporário em arquivo, sem Docker
├── docs/
│   ├── architecture.md
│   └── security-decisions.md
├── .github/workflows/ci.yml
├── CONTEXT.md                        # este arquivo
└── .gitignore
```

---

## Arquivos críticos

| Arquivo | Papel |
|---|---|
| `src/SecureVault.API/Program.cs` | Entry point: EnvBootstrap, migrations, middlewares, Swagger |
| `src/SecureVault.API/Infrastructure/EnvBootstrap.cs` | Gera `.env` com chaves seguras se não existir, carrega via env vars |
| `src/SecureVault.Infrastructure/DependencyInjection.cs` | Registra SQLite, repositórios, Argon2, AES, JWT |
| `src/SecureVault.API/appsettings.json` | SQLite connection string, JWT issuer/audience, rate limiting |
| `src/SecureVault.API/appsettings.Development.json` | Apenas override Serilog (sem connection string — foi removida) |
| `src/SecureVault.API/Properties/launchSettings.json` | Porta 5000, launchBrowser: false |
| `src/SecureVault.Infrastructure/Persistence/Migrations/` | Migration SQLite gerada |

---

## Decisões arquiteturais principais

- **SQLite** (não PostgreSQL): banco embutido, zero instalação, zero servidor. Troca para Postgres é uma linha em `DependencyInjection.cs`
- **Sem Docker**: removido — adicionava complexidade sem valor para o objetivo de portfólio
- **EnvBootstrap**: gera chaves criptográficas automaticamente → recrutador não precisa saber gerar chaves
- **Clean Architecture**: Domain → Application → Infrastructure → API (dependências só apontam para dentro)
- **CQRS com MediatR**: commands e queries separados, pipeline de validação automático
- **Argon2id**: hash de senhas (OWASP recomendado, resistente a GPU)
- **AES-256-GCM**: criptografia autenticada das credenciais (confidencialidade + integridade)
- **JWT 15min + refresh token 7 dias** com rotação a cada renovação
- **404 em vez de 403** ao acessar recurso de outro usuário (não revela existência)
- **Testes de integração com SQLite temporário em arquivo**: sem Docker, sem serviços externos

---

## Packages instalados

**Domain:** FluentResults 3.15.2

**Application:** MediatR 12.2.0, FluentValidation 11.9.0, FluentValidation.DI 11.9.0, FluentResults 3.15.2

**Infrastructure:** EF Core 8.0.0, EF Core SQLite 8.0.0, EF Core Design 8.0.0, Konscious.Security.Cryptography.Argon2 1.3.1, Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0

**API:** Swashbuckle.AspNetCore 6.5.0, Serilog.AspNetCore 8.0.0, Serilog.Sinks.Console 5.0.1, AspNetCoreRateLimit 5.0.0, EF Core Design 8.0.0, HealthChecks.EntityFrameworkCore 8.0.0

**UnitTests:** xUnit, FluentAssertions 6.12.0, NSubstitute 5.1.0

**IntegrationTests:** xUnit, Microsoft.AspNetCore.Mvc.Testing 8.0.0, EF Core SQLite 8.0.0, FluentAssertions 6.12.0

---

## Endpoints implementados

```
POST   /auth/register
POST   /auth/login
POST   /auth/refresh
POST   /auth/logout        [Authorize]

GET    /vault              [Authorize]
GET    /vault/search?q=    [Authorize]
GET    /vault/{id}         [Authorize]
POST   /vault              [Authorize]
PUT    /vault/{id}         [Authorize]
DELETE /vault/{id}         [Authorize]

GET    /health
```

---

## Tarefas pendentes

- [x] Confirmar que `dotnet run` sobe sem erros
- [x] Confirmar que testes de integração passam (`dotnet test`)
- [x] Testar fluxo completo no Swagger manualmente
- [x] Inicializar repositório Git (`git init && git add . && git commit`)
- [x] Criar `docker-compose.yml`
- [x] Criar `.env.example`
- [ ] Validar `docker compose up` com Docker daemon ativo
- [ ] Criar repositório no GitHub e fazer push
- [ ] Atualizar badge de CI no README com URL real do repositório

---

## Portfólio completo (contexto maior)

Este é o **Projeto 1 de 3**:

| # | Projeto | Stack | Status |
|---|---|---|---|
| 1 | SecureVault API | .NET 8 + SQLite | Em desenvolvimento |
| 2 | Aplicação fullstack | .NET + Next.js + SQLite | Planejado |
| 3 | Ferramenta de segurança | Python | Aguardando evolução em cibersec |

Todos os projetos vivem em `C:\Users\User\Desktop\Coding\Projects\` e seguem os mesmos requisitos de portfólio definidos em `C:\Users\User\Desktop\Coding\Projects\CLAUDE.md`.
