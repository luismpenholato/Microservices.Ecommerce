# Segurança

Visão da autenticação e autorização no **Microservices.Ecommerce** (portfólio / demo local).

## IdentityService

| Endpoint | Auth |
|----------|------|
| `POST /identity/auth/register` | Público |
| `POST /identity/auth/login` | Público |
| `GET /identity/auth/me` | JWT |

- Senhas com **BCrypt** (work factor 12); senha **nunca** é logada.
- JWT emitido com `sub`, `email`, `customer_id`, `role`.
- Configuração: `Jwt:Secret`, `Issuer`, `Audience`, `ExpirationMinutes` (variáveis de ambiente no Docker).

## ApiGateway (borda)

`GatewayAuthorizationMiddleware` após validação JWT:

| Rota | Política |
|------|----------|
| `/identity/*` (register/login) | Público |
| `GET /catalog/products*` | Público |
| `POST/PUT /catalog/*` | JWT + role **Admin** |
| `/basket/*`, `/ordering/*` | JWT obrigatório |
| `/inventory/*` | Público (demo) |

## Serviços (defesa em profundidade)

- **Basket** e **Ordering**: `[Authorize]` + validação `customerId` da URL contra claim `customer_id` (403 se divergir).
- **Ordering** `POST /orders`: `customerId` vem **apenas** do token; body não define cliente.
- **Basket → Ordering**: `BearerTokenForwardingHandler` repassa o JWT no checkout.
- **Catalog**: leitura anônima; escrita exige Admin.

## Credenciais demo (seed)

| Perfil | E-mail | Senha | Role |
|--------|--------|-------|------|
| Cliente | `demo@ecommerce.local` | `Demo123!` | Customer |
| Admin | `admin@ecommerce.local` | `Admin123!` | Admin |

> Use apenas em ambiente local. Não reutilize em produção.

## Segredos

- `Jwt:Secret` mínimo 32 caracteres — via `appsettings`, `appsettings.Docker.json` ou `Jwt__Secret` no Compose (demo).
- Não commitar `.env` com segredos reais (ver [.gitignore](../.gitignore)).

## Limitações intencionais (demo)

- Sem refresh token, OAuth ou IdentityServer.
- JWT simétrico (HMAC) compartilhado entre serviços.
- Inventory público para facilitar smoke tests.

Evoluções planejadas: [ROADMAP.md](../ROADMAP.md).
