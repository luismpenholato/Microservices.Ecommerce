# Contributing

Thank you for your interest in contributing to **Microservices.Ecommerce**.

## Before opening a PR

1. Open an issue describing the change (bug, documentation, improvement).
2. Fork the repository and create a branch from `main`.
3. Keep the scope focused — avoid mixing broad refactors with feature work.

## Local environment

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (integration tests and local Docker Compose)

```bash
dotnet restore Microservices.Ecommerce.sln
dotnet build Microservices.Ecommerce.sln -c Release
dotnet test
docker compose up -d --build
```

Manual stack validation: [docs/smoke-tests.md](docs/smoke-tests.md).

## Code standards

- Follow [.editorconfig](.editorconfig).
- Clean Architecture per service: business rules in **Application/Domain**, not in controllers or consumers.
- Do not commit secrets (`.env`, production JWT keys).
- `TreatWarningsAsErrors` is **disabled** solution-wide; fix warnings relevant to code you change.

## Tests

| Type | Project | Docker |
|------|---------|--------|
| Unit | `*UnitTests` | No |
| Integration | `IntegrationTests` | Yes |
| Security | `Security.IntegrationTests` | Yes |

Include or update tests when changing observable behavior.

## Commits

Prefer clear messages in English, imperative mood:

- `fix: validate customerId ownership in Basket checkout`
- `docs: update smoke test guide`

## Pull requests

Use the PR template. Describe what changed, how you tested, and whether runbooks or documentation are affected.
