# Testes

Estratégia de testes do repositório.

## Visão geral

| Camada | Projeto | Ferramentas | Docker |
|--------|---------|-------------|--------|
| Unitário | `Catalog.UnitTests`, `Basket.UnitTests`, `Ordering.UnitTests` | xUnit, FluentAssertions, Moq | Não |
| Integração | `IntegrationTests` | WebApplicationFactory, Testcontainers | **Sim** |
| Segurança | `Security.IntegrationTests` | WebApplicationFactory, Testcontainers | **Sim** |

## CI (GitHub Actions)

Workflow [`.github/workflows/ci.yml`](../.github/workflows/ci.yml):

1. **build** — `dotnet build` + testes unitários (`*UnitTests`)
2. **integration** — testes `IntegrationTests` + `Security.IntegrationTests` (runner com Docker)

## Executar localmente

```bash
# Todos (requer Docker para integração/segurança)
dotnet test

# Apenas unitários
dotnet test tests/Catalog.UnitTests tests/Basket.UnitTests tests/Ordering.UnitTests

# Integração + segurança
dotnet test tests/IntegrationTests tests/Security.IntegrationTests
```

## O que cada suite cobre

### Unitários

- Regras de domínio e validações (FluentValidation) isoladas.

### Integração (`IntegrationTests`)

- Outbox + idempotência no Ordering
- Resiliência do `OutboxDispatcher` (falha transitória de publish)
- Consumer com falha simulada + retry MassTransit
- Concorrência de estoque (dois pedidos, estoque 1)
- Checkout Basket → Ordering (com JWT)

### Segurança (`Security.IntegrationTests`)

- Register / login / login inválido
- Basket sem token → 401; token válido → 200; `customerId` divergente → 403
- Gateway: basket protegido; catalog GET público

## Validação operacional (manual / script)

Não substitui testes automatizados, mas valida o stack completo:

```powershell
.\scripts\validate-local.ps1
.\scripts\validate-local.ps1 -RunCheckoutFlow
```

Ver [smoke-tests.md](./smoke-tests.md) e [operations.md](./operations.md).

## Testes manuais recomendados

- `docker compose up` + fluxo em [smoke-tests.md](./smoke-tests.md)
- Dashboards Grafana / Seq após checkout
- RabbitMQ UI — filas `_error` vazias em fluxo feliz
- Runbooks em [runbooks/](./runbooks/) em cenários de falha simulada

## Helpers

- `tests/IntegrationTests/Infrastructure/IntegrationTestAuthHelper.cs` — JWT para testes de Ordering/Basket
- `tests/Security.IntegrationTests/Infrastructure/TestJwtTokenFactory.cs` — JWT para testes de segurança
