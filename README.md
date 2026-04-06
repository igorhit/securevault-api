# SecureVault API

![CI](https://github.com/igorhit/securevault-api/actions/workflows/ci.yml/badge.svg)

API REST para gerenciamento seguro de credenciais pessoais, construída com .NET 8, Clean Architecture e SQLite.

O foco deste projeto é demonstrar backend com segurança aplicada de forma visível:
- autenticação com JWT + refresh token com rotação
- hash de senha com Argon2id
- criptografia de credenciais com AES-256-GCM
- testes automatizados e execução reproduzível

## O que este projeto resolve

O usuário pode registrar uma conta, autenticar-se e armazenar credenciais de forma segura em um cofre pessoal. Os dados sensíveis não são persistidos em plaintext no banco: username, password e notes são cifrados antes de serem gravados.

## Por que foi construído assim

- **SQLite**: elimina a necessidade de instalar e configurar um banco separado
- **Clean Architecture**: deixa regras de negócio e infraestrutura desacopladas
- **CQRS com MediatR**: separa leitura de escrita e simplifica os handlers
- **Docker Compose com um serviço**: cumpre o fluxo de `clone + um comando` sem adicionar complexidade artificial; o banco continua sendo SQLite embutido, com volume persistente

Detalhes arquiteturais: [docs/architecture.md](docs/architecture.md)  
Detalhes de segurança: [docs/security-decisions.md](docs/security-decisions.md)

## Como rodar

### Opção principal: Docker Compose

Pré-requisito: Docker Desktop

```bash
git clone <repo>
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

### Opção local: .NET SDK

Pré-requisito: .NET 8 SDK

```bash
cd src/SecureVault.API
dotnet run
```

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

Hoje elas são:

- `APP_PORT`: porta publicada localmente pelo container
- `ASPNETCORE_ENVIRONMENT`: ambiente ASP.NET usado no container

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

## Decisões arquiteturais principais

- Banco embutido com SQLite para reduzir fricção operacional
- Migrations aplicadas automaticamente no startup
- Secrets locais gerados automaticamente via bootstrap de `.env`
- Testes de integração com SQLite temporário em arquivo, sem dependências externas
- Volume Docker para persistir banco e secrets locais da aplicação

## Segurança visível

- `Argon2id` para hash de senhas
- `AES-256-GCM` para cifrar credenciais
- `JWT` com expiração curta e refresh token com rotação
- `Rate limiting` nos endpoints sensíveis
- `OWASP security headers`
- `404` em vez de `403` ao acessar recurso de outro usuário

## CI

O workflow de CI está em [.github/workflows/ci.yml](.github/workflows/ci.yml) e executa build + testes automatizados.
