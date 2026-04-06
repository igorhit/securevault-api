# SecureVault API

![CI](https://github.com/igorhit/securevault-api/actions/workflows/ci.yml/badge.svg)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4)
![Docker Compose](https://img.shields.io/badge/docker-compose-2496ED)
![SQLite](https://img.shields.io/badge/database-SQLite-003B57)
![Security](https://img.shields.io/badge/focus-security-2E7D32)

API REST para gerenciamento seguro de credenciais pessoais, construída com .NET 8, Clean Architecture e SQLite.

Este projeto foi desenhado para permitir validação rápida de backend com segurança aplicada:
- autenticação com JWT + refresh token com rotação
- hash de senha com Argon2id
- criptografia de credenciais com AES-256-GCM
- testes automatizados
- execução reproduzível com `docker compose up --build`

## O que este projeto resolve

O usuário pode registrar uma conta, autenticar-se e armazenar credenciais de forma segura em um cofre pessoal. Os dados sensíveis não são persistidos em plaintext no banco: `username`, `password` e `notes` são cifrados antes de serem gravados.

## Highlights

| Área | Implementação |
|---|---|
| Arquitetura | Clean Architecture com camadas Domain, Application, Infrastructure e API |
| Padrão de aplicação | CQRS com MediatR |
| Autenticação | JWT de 15 min + refresh token de 7 dias com rotação |
| Segurança | Argon2id, AES-256-GCM, rate limiting e OWASP headers |
| Banco | SQLite embutido com migrations automáticas |
| Testes | Unitários + integração |
| Execução | Docker Compose com volume persistente e seed demo |

Detalhes arquiteturais: [docs/architecture.md](docs/architecture.md)  
Detalhes de segurança: [docs/security-decisions.md](docs/security-decisions.md)

## Como rodar

### Opção principal: Docker Compose

Pré-requisito: Docker Desktop

```bash
git clone https://github.com/igorhit/securevault-api.git
cd securevault-api
docker compose up --build
```

Depois acesse:

- Swagger: `http://localhost:5000`
- Health check: `http://localhost:5000/health`

Observações:

- o banco SQLite fica persistido em um volume Docker
- o `.env` interno da aplicação é gerado automaticamente na primeira execução dentro do volume persistente
- não existe serviço separado de banco porque o projeto usa SQLite embutido
- dados demo são criados automaticamente no primeiro startup do ambiente Docker

### Parar o ambiente

```bash
docker compose down
```

### Resetar o banco/demo do zero

```bash
docker compose down -v
docker compose up --build
```

### Opção local: .NET SDK

Pré-requisito: .NET 8 SDK

```bash
cd src/SecureVault.API
dotnet run
```

No modo local via `dotnet run`, o seed demo não é ativado por padrão.

## Validação rápida no Swagger

No fluxo via Docker Compose, a aplicação já sobe com um usuário demo e credenciais de exemplo.

### Login demo

```text
email: demo@securevault.local
password: Demo@123456
```

### Fluxo recomendado para recrutador

1. Abrir `http://localhost:5000`
2. Executar `POST /auth/login` com o usuário demo
3. Copiar o `accessToken`
4. Clicar em `Authorize` e colar `Bearer <accessToken>`
5. Executar `GET /vault` para ver as credenciais seedadas
6. Testar `GET /vault/search`, `PUT /vault/{id}` e `DELETE /vault/{id}`

### Credenciais seedadas

- `GitHub Demo`
- `Azure Portal Demo`

## Como executar os testes

```bash
dotnet test
```

Ou separadamente:

```bash
dotnet test tests/SecureVault.UnitTests
dotnet test tests/SecureVault.IntegrationTests
```

## Variáveis de ambiente

As variáveis opcionais para o `docker compose` estão documentadas em [.env.example](.env.example).

| Variável | Papel |
|---|---|
| `APP_PORT` | Porta publicada localmente pelo container |
| `ASPNETCORE_ENVIRONMENT` | Ambiente ASP.NET usado no container |
| `DEMO_DATA_ENABLED` | Liga/desliga o seed de demonstração no Compose |
| `DEMO_USER_EMAIL` | E-mail do usuário demo |
| `DEMO_USER_PASSWORD` | Senha do usuário demo |

Observação importante:

- o arquivo `src/SecureVault.API/.env` não deve ser criado manualmente para o fluxo normal do projeto; ele é gerado automaticamente pela aplicação com segredos locais

## Endpoints principais

### Auth

```text
POST /auth/register
POST /auth/login
POST /auth/refresh
POST /auth/logout
```

### Vault

```text
GET    /vault
GET    /vault/{id}
GET    /vault/search?q=
POST   /vault
PUT    /vault/{id}
DELETE /vault/{id}
```

### Monitoramento

```text
GET /health
```

## Decisões arquiteturais principais

- banco embutido com SQLite para reduzir fricção operacional
- migrations aplicadas automaticamente no startup
- secrets locais gerados automaticamente via bootstrap de `.env`
- testes de integração com SQLite temporário em arquivo, sem dependências externas
- volume Docker para persistir banco e secrets locais da aplicação
- seed demo idempotente no startup do ambiente Docker para reduzir atrito na avaliação

## Segurança visível

- `Argon2id` para hash de senhas
- `AES-256-GCM` para cifrar credenciais
- `JWT` com expiração curta e refresh token com rotação
- `Rate limiting` nos endpoints sensíveis
- `OWASP security headers`
- `404` em vez de `403` ao acessar recurso de outro usuário

## Estrutura do projeto

```text
SecureVaultApi/
├── .github/workflows/ci.yml
├── docs/
├── src/
│   ├── SecureVault.Domain/
│   ├── SecureVault.Application/
│   ├── SecureVault.Infrastructure/
│   └── SecureVault.API/
├── tests/
│   ├── SecureVault.UnitTests/
│   └── SecureVault.IntegrationTests/
├── .env.example
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## CI

O workflow de CI está em [.github/workflows/ci.yml](.github/workflows/ci.yml) e executa build + testes automatizados a cada push e pull request.
