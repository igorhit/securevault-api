# Decisões de Segurança — SecureVault API

## Hash de senhas: Argon2id

**Por que não bcrypt ou PBKDF2?**

bcrypt é amplamente usado e seguro, mas tem limitações:
- Trunca senhas em 72 bytes
- Não é resistente a ataques com hardware especializado (FPGAs, GPUs)

Argon2id ganhou o [Password Hashing Competition](https://www.password-hashing.net/) em 2015 e é a recomendação atual do OWASP. Ele combina resistência a:
- **Ataques de GPU**: o parâmetro `MemorySize` força uso intenso de memória, que é cara em GPU
- **Side-channel attacks**: a variante `id` (hybrid de `i` + `d`) é resistente a timing attacks

**Parâmetros utilizados:**

```csharp
Iterations = 4           // Número de passagens
MemorySize = 65536       // 64 MB de memória RAM por hash
DegreeOfParallelism = 2  // Threads por operação
```

Esses valores foram calibrados para ~300ms em hardware moderno, conforme recomendação OWASP.

**Comparação em tempo constante:**

```csharp
return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
```

A comparação direta (`actualHash == expectedHash`) vaza informação através do tempo de execução (timing attack). `FixedTimeEquals` sempre compara todos os bytes, independente de onde a diferença está.

---

## Criptografia de credenciais: AES-256-GCM

**Por que criptografia simétrica?**

As credenciais precisam ser descriptografadas para serem exibidas ao usuário. Criptografia assimétrica (RSA) não seria adequada aqui — é muito lenta para dados de tamanho variável e exigiria a chave privada acessível no servidor de qualquer forma.

**Por que GCM e não CBC?**

AES-CBC (modo padrão mais antigo) oferece apenas confidencialidade. AES-GCM é um modo AEAD (Authenticated Encryption with Associated Data):

- **Confidencialidade**: ninguém lê sem a chave
- **Integridade**: qualquer alteração no ciphertext é detectada via authentication tag (128 bits)
- **Autenticidade**: garante que o dado foi cifrado pela chave correta

Com CBC, um atacante poderia alterar bits do ciphertext de formas específicas sem ser detectado (padding oracle attacks). Com GCM, qualquer alteração invalida a tag.

**Nonce único por operação:**

```csharp
var nonce = RandomNumberGenerator.GetBytes(NonceSize); // 96 bits
```

Reutilizar nonces em AES-GCM é catastrófico para a segurança (quebra confidencialidade e autenticidade). Por isso, um nonce aleatório é gerado para cada operação de cifragem. O nonce é armazenado junto com o ciphertext (não é secreto, mas deve ser único).

**Formato de armazenamento:**

```
base64(nonce[12 bytes] + ciphertext[N bytes] + tag[16 bytes])
```

---

## Autenticação: JWT + Refresh Token

**Por que JWT de curta duração (15 min)?**

JWTs são stateless: o servidor não armazena estado de sessão. Isso tem uma desvantagem: um token comprometido não pode ser revogado antes de expirar.

A solução é manter os access tokens com validade curta (15 min). O impacto para o usuário é mitigado pelo refresh token.

**Rotação de refresh tokens:**

```csharp
storedToken.Revoke();  // Invalida o token atual
var newRefreshToken = RefreshToken.Create(...);  // Emite um novo
```

A cada renovação, o refresh token anterior é revogado e um novo é emitido. Isso evita que um token roubado seja usado indefinidamente.

**ClockSkew = Zero:**

```csharp
ClockSkew = TimeSpan.Zero
```

Por padrão, o middleware JWT do .NET aceita tokens com até 5 minutos de atraso de clock. Isso efetivamente transforma um token de 15 min em um de 20 min. Zeramos isso para que os tokens expirem exatamente no tempo definido.

---

## Proteção de endpoints: Rate Limiting

**Por que rate limiting?**

Sem rate limiting, um atacante pode fazer ataques de força bruta contra o endpoint de login indefinidamente.

**Regras configuradas:**

```json
POST /auth/login    → 10 requisições/minuto por IP
POST /auth/register → 5 requisições/hora por IP
*                   → 20 requisições/segundo por IP
```

O limite de registro é mais restritivo para dificultar a criação em massa de contas.

---

## OWASP Security Headers

Headers adicionados a todas as respostas:

| Header | Valor | Protege contra |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | MIME type sniffing |
| `X-Frame-Options` | `DENY` | Clickjacking |
| `X-XSS-Protection` | `1; mode=block` | XSS em browsers antigos |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Vazamento de URL via Referer |
| `Content-Security-Policy` | `default-src 'self'` | Injeção de conteúdo externo |

**Exceção controlada para o Swagger UI:**

O Swagger UI usa scripts e estilos inline. Por isso, a CSP foi relaxada apenas para `/` e `/index.html`, permitindo `unsafe-inline` para script/style nessa superfície específica. O restante da API continua com CSP restritiva.

---

## Não revelar informações sensíveis

**Mensagem genérica no login:**

```csharp
// Tanto e-mail inexistente quanto senha incorreta retornam o mesmo erro
return Result.Fail(new Error(DomainErrors.User.InvalidCredentials));
```

Se retornássemos "e-mail não encontrado" vs "senha incorreta", um atacante poderia usar o endpoint de login para enumerar e-mails válidos do sistema.

**404 em vez de 403 para credenciais de outro usuário:**

```csharp
var credential = await _credentialRepository.GetByIdAndUserIdAsync(id, userId);
if (credential is null)
    return NotFound(); // Não revela que a credencial existe para outro usuário
```

Retornar 403 informaria ao atacante que o recurso existe, apenas não está autorizado. Com 404, não é possível distinguir "não existe" de "existe mas não é seu".

---

## Princípio do menor privilégio no container

```dockerfile
RUN adduser --disabled-password --gecos "" appuser
USER appuser
```

O processo da API roda com um usuário sem privilégios dentro do container. Se houver uma vulnerabilidade que permita execução de código, o atacante terá acesso apenas às permissões desse usuário limitado.
