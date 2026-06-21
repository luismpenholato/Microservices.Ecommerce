# Testing

Test strategy for this repository.

## Overview

| Layer | Project | Tools | Docker |
|-------|---------|-------|--------|
| Unit | `Catalog.UnitTests`, `Basket.UnitTests`, `Ordering.UnitTests` | xUnit, FluentAssertions, Moq | No |
| Integration | `IntegrationTests` | WebApplicationFactory, Testcontainers | **Yes** |
| Security | `Security.IntegrationTests` | WebApplicationFactory, Testcontainers | **Yes** |

## CI (GitHub Actions)

Workflow [`.github/workflows/ci.yml`](../.github/workflows/ci.yml):

1. **build** — `dotnet build`
2. **unit-tests** — `Catalog.UnitTests`, `Basket.UnitTests`, `Ordering.UnitTests`

Integration and security tests are **not** executed in CI. They require Docker (Testcontainers) and are intended for local validation only.

## Run locally

```bash
# All (requires Docker for integration/security)
dotnet test

# Unit tests only
dotnet test tests/Catalog.UnitTests tests/Basket.UnitTests tests/Ordering.UnitTests

# Integration + security
dotnet test tests/IntegrationTests tests/Security.IntegrationTests
```

## What each suite covers

### Unit tests

- Domain rules and validations (FluentValidation) in isolation.

### Integration (`IntegrationTests`)

- Outbox + idempotency in Ordering
- `OutboxDispatcher` resilience (transient publish failure)
- Consumer with simulated failure + MassTransit retry
- Stock concurrency (two orders, stock of 1)
- Basket → Ordering checkout (with JWT)

### Security (`Security.IntegrationTests`)

- Register / login / invalid login
- Basket without token → 401; valid token → 200; mismatched `customerId` → 403
- Gateway: basket protected; catalog GET public

## Operational validation (manual)

Does not replace automated tests, but validates the full stack. Follow [smoke-tests.md](./smoke-tests.md) after `docker compose up`.

## Recommended manual tests

- `docker compose up` + flow in [smoke-tests.md](./smoke-tests.md)
- Grafana / Seq dashboards after checkout
- RabbitMQ UI — `_error` queues empty on happy path
- Runbooks in [runbooks/](./runbooks/) for simulated failure scenarios

## Helpers

- `tests/IntegrationTests/Infrastructure/IntegrationTestAuthHelper.cs` — JWT for Ordering/Basket tests
- `tests/Security.IntegrationTests/Infrastructure/TestJwtTokenFactory.cs` — JWT for security tests
