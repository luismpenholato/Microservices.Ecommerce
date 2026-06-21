# Security

Overview of authentication and authorization in **Microservices.Ecommerce** (portfolio / local demo).

## IdentityService

| Endpoint | Auth |
|----------|------|
| `POST /identity/auth/register` | Public |
| `POST /identity/auth/login` | Public |
| `GET /identity/auth/me` | JWT |

- Passwords with **BCrypt** (work factor 12); password is **never** logged.
- JWT issued with `sub`, `email`, `customer_id`, `role`.
- Configuration: `Jwt:Secret`, `Issuer`, `Audience`, `ExpirationMinutes` (environment variables in Docker).

## ApiGateway (edge)

`GatewayAuthorizationMiddleware` after JWT validation:

| Route | Policy |
|-------|--------|
| `/identity/*` (register/login) | Public |
| `GET /catalog/products*` | Public |
| `POST/PUT /catalog/*` | JWT + **Admin** role |
| `/basket/*`, `/ordering/*` | JWT required |
| `/inventory/*` | Public (demo) |

## Services (defense in depth)

- **Basket** and **Ordering**: `[Authorize]` + validate URL `customerId` against `customer_id` claim (403 if mismatch).
- **Ordering** `POST /orders`: `customerId` comes **only** from the token; body does not define the customer.
- **Basket → Ordering**: `BearerTokenForwardingHandler` forwards JWT on checkout.
- **Catalog**: anonymous read; write requires Admin.

## Demo credentials (seed)

| Profile | Email | Password | Role |
|---------|-------|----------|------|
| Customer | `demo@ecommerce.local` | `Demo123!` | Customer |
| Admin | `admin@ecommerce.local` | `Admin123!` | Admin |

> Use only in local environment. Do not reuse in production.

## Secrets

- `Jwt:Secret` minimum 32 characters — via `appsettings`, `appsettings.Docker.json`, or `Jwt__Secret` in Compose (demo).
- Do not commit `.env` with real secrets (see [.gitignore](../.gitignore)).

## Intentional limitations (demo)

- No refresh token, OAuth, or IdentityServer.
- Shared symmetric JWT (HMAC) across services.
- Inventory is public to simplify smoke tests.

Planned evolutions: [ROADMAP.md](../ROADMAP.md).
