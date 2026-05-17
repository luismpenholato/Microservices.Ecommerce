# Contribuindo

Obrigado pelo interesse em contribuir com o **Microservices.Ecommerce**.

## Antes de abrir um PR

1. Abra uma issue descrevendo a mudança (bug, doc, melhoria).
2. Faça fork e crie uma branch a partir de `main`.
3. Mantenha o escopo focado — evite refactors amplos misturados com feature.

## Ambiente local

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (testes de integração e `validate-local`)

```bash
dotnet restore
dotnet build
dotnet test
docker compose up -d --build
./scripts/validate-local.sh --run-checkout-flow   # Linux/macOS/WSL
```

## Padrões de código

- Siga o [.editorconfig](.editorconfig).
- Clean Architecture por serviço: regra de negócio em **Application/Domain**, não em controllers ou consumers.
- Não commitar segredos (`.env`, chaves JWT de produção).
- `TreatWarningsAsErrors` permanece **desligado** na solução; corrija warnings relevantes no código que você alterar.

## Testes

| Tipo | Projeto | Docker |
|------|---------|--------|
| Unitário | `*UnitTests` | Não |
| Integração | `IntegrationTests` | Sim |
| Segurança | `Security.IntegrationTests` | Sim |

Inclua ou atualize testes quando alterar comportamento observável.

## Commits

Prefira mensagens claras em português ou inglês, no imperativo:

- `fix: corrige validação de customerId no Basket`
- `docs: atualiza guia de smoke tests`

## Pull requests

Use o template de PR. Descreva o que mudou, como testou e se há impacto em runbooks ou scripts `validate-local`.
