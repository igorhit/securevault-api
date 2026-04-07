# Arquitetura — SecureVault API

## Visão geral

O projeto foi estruturado para demonstrar um backend realista, mas com fricção operacional mínima para quem avalia o portfólio. A prioridade foi equilibrar:

> Problemas encontrados durante a implementação e como foram resolvidos estão documentados em [implementation-notes.md](implementation-notes.md).

- segurança visível
- separação clara de responsabilidades
- execução local simples
- testes confiáveis sem infraestrutura externa

## Por que Clean Architecture

A solução foi dividida em quatro camadas:

```text
API
Infrastructure
Application
Domain
```

As dependências apontam sempre para dentro:

- `Domain` contém entidades, interfaces e erros de domínio
- `Application` contém casos de uso, validators, DTOs e pipeline behaviors
- `Infrastructure` implementa persistência, criptografia, hashing e JWT
- `API` expõe HTTP, middlewares, Swagger e bootstrap da aplicação

Essa separação melhora três coisas importantes para portfólio:

1. facilita explicar o sistema em entrevista
2. permite testar regras sem depender do transporte HTTP
3. deixa a troca de detalhes de infraestrutura mais isolada

## Por que CQRS com MediatR

Os fluxos de escrita e leitura foram separados em commands e queries.

Exemplos:

- `RegisterCommand`
- `LoginCommand`
- `CreateCredentialCommand`
- `GetCredentialsQuery`
- `SearchCredentialsQuery`

Isso evita concentrar regras em services grandes e difusos. Cada handler tem uma responsabilidade única, o que melhora legibilidade, teste e manutenção.

## Por que FluentValidation em pipeline

A validação roda antes do handler através de `ValidationBehavior<TRequest, TResponse>`.

Consequências práticas:

- handlers recebem dados já validados
- as regras ficam centralizadas e previsíveis
- o controller não precisa repetir validação manual

Também foi corrigido o retorno de falhas tipadas para `Result<T>`, evitando exceções de runtime no pipeline de validação.

## Por que FluentResults em vez de exceptions para fluxo normal

Casos como:

- e-mail já cadastrado
- credencial não encontrada
- credenciais inválidas

não são falhas excepcionais de infraestrutura. São resultados esperados do domínio. Por isso, os handlers retornam `Result` ou `Result<T>` e o controller mapeia isso para `400`, `401`, `404` ou `409`.

Exceptions ficam reservadas para cenários realmente anômalos.

## Banco de dados

### Por que SQLite

O projeto prioriza execução simples. SQLite resolve isso melhor que PostgreSQL ou SQL Server neste contexto porque:

- não exige serviço separado
- não exige credenciais
- não exige setup manual
- permite persistência real em arquivo

O trade-off aceito é abrir mão de simular um banco servidor para ganhar reprodutibilidade imediata.

### Onde o banco fica

No modo local, o banco fica em:

```text
src/SecureVault.API/securevault.db
```

No Docker Compose, o caminho de runtime é configurado por `Storage__RuntimePath=/app/runtime`, e o banco fica dentro do volume persistente montado no container.

## Execução reproduzível com Docker Compose

O `docker compose` foi definido com apenas um serviço de aplicação.

Isso é intencional:

- o requisito é `clone + um comando`
- o banco é SQLite embutido, então um serviço separado de banco não adicionaria valor
- a persistência é atendida com volume Docker

O volume armazena:

- `securevault.db`
- `.env` gerado automaticamente pela aplicação

Isso evita perda de dados e troca involuntária de segredos a cada restart do container.

## Seed de dados demo

Para reduzir atrito na avaliação, o ambiente Docker ativa um seed idempotente no startup.

Esse seed:

- cria um usuário demo
- cria credenciais de exemplo
- não duplica dados se o volume já tiver sido inicializado

O seed é controlado por configuração:

- `DemoData:Enabled`
- `DemoData:UserEmail`
- `DemoData:UserPassword`

No fluxo local com `dotnet run`, ele permanece desligado por padrão. No `docker-compose.yml`, ele é ativado para que o recrutador consiga validar login e listagem imediatamente.

## Bootstrap de ambiente

No startup:

1. a aplicação resolve o runtime path
2. garante que a pasta exista
3. gera um `.env` se ele ainda não existir
4. carrega os segredos para o processo
5. aplica as migrations automaticamente

Esse fluxo foi mantido fora do ambiente `Testing`, onde os testes de integração injetam configuração controlada.

## Por que migrations automáticas no startup

O projeto foi pensado para ser executado sem scripts manuais. Aplicar `db.Database.MigrateAsync()` no startup reduz a chance de erro operacional e melhora a experiência de avaliação.

Trade-off aceito:

- em produção com múltiplas instâncias, migrations automáticas no startup poderiam exigir uma estratégia mais controlada

Para um projeto de portfólio com foco em reprodutibilidade local, o ganho compensa.

## Testes automatizados

### Testes unitários

Cobrem regras e handlers isolados, sem dependência de infraestrutura real.

### Testes de integração

Usam `WebApplicationFactory` com SQLite temporário em arquivo.

Essa abordagem foi escolhida porque:

- mantém os testes rápidos
- evita dependência de Docker no pipeline de testes
- permite validar a API completa, incluindo middleware, autenticação e persistência

O setup anterior com SQLite em memória foi substituído porque ele era mais frágil com o ciclo de vida da aplicação de teste e com as migrations do EF Core.

## Persistência e segurança das credenciais

Os campos sensíveis da entidade `Credential` não são persistidos como plaintext.

No banco, o que é salvo são campos como:

- `EncryptedUsername`
- `EncryptedPassword`
- `EncryptedNotes`

O valor é cifrado com AES-256-GCM antes de persistir e descriptografado apenas quando a resposta precisa ser montada para o usuário autenticado.

## Trade-offs aceitos conscientemente

- **SQLite em vez de banco servidor**: menos realismo operacional, mais reprodutibilidade
- **um único serviço no Compose**: menos complexidade, mesma experiência para este escopo
- **migrations automáticas no startup**: ótima DX local, menos controle fino em cenários distribuídos
- **Swagger habilitado**: útil para validação manual e demonstração, desde que acompanhado de headers e autenticação adequados
