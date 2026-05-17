# Operações — Microservices.Ecommerce

Guia para operação local demonstrável (sem Kafka/Alertmanager em produção).

## Validação automatizada

```powershell
.\scripts\validate-local.ps1
```

```bash
./scripts/validate-local.sh
./scripts/validate-local.sh --run-checkout-flow
```

Checkout E2E opcional: `-RunCheckoutFlow` (PowerShell) / `--run-checkout-flow` (Bash).

Smoke tests: [smoke-tests.md](./smoke-tests.md). Segurança: [security.md](./security.md). Testes: [testing.md](./testing.md). Exemplos curl: [examples/http-requests.md](./examples/http-requests.md).

## Endpoints operacionais

| Endpoint | APIs | Workers |
|----------|------|---------|
| `GET /health/live` | Sim | Sim |
| `GET /health/ready` | Sim | Sim |
| `GET /metrics` | Sim | Sim |

- **live**: processo responde (não valida dependências).
- **ready**: dependências reais do serviço (ver tabela abaixo).

## Health checks por serviço

| Serviço | ready valida |
|---------|----------------|
| Identity | PostgreSQL `identity_db` |
| ApiGateway | Configuração YARP (`gateway`) |
| Catalog | PostgreSQL `catalog_db` |
| Basket | Redis |
| Ordering | PostgreSQL + RabbitMQ |
| Inventory | PostgreSQL + RabbitMQ |
| Payment.Worker | PostgreSQL + RabbitMQ |
| Notification.Worker | PostgreSQL + RabbitMQ |

## Métricas (`ecommerce_*`)

Expostas em `/metrics` (Prometheus via OpenTelemetry).

| Métrica | Tipo | Labels |
|---------|------|--------|
| `ecommerce_consumer_messages_processed_total` | Counter | `service`, `consumer_name` |
| `ecommerce_consumer_messages_failed_total` | Counter | `service`, `consumer_name` |
| `ecommerce_outbox_messages_pending` | Observable gauge | `service` |
| `ecommerce_outbox_messages_publish_failures_total` | Counter | `service` |
| `ecommerce_outbox_messages_exhausted_total` | Counter | `service` |
| `ecommerce_outbox_messages_published_total` | Counter | `service` |
| `ecommerce_orders_created_total` | Counter | — |
| `ecommerce_orders_completed_total` | Counter | — |
| `ecommerce_orders_cancelled_total` | Counter | — |
| `ecommerce_orders_failed_total` | Counter | — |
| `ecommerce_stock_reservations_approved_total` | Counter | — |
| `ecommerce_stock_reservations_failed_total` | Counter | — |

## Configuração (precedência)

1. `appsettings.json`
2. `appsettings.{Environment}.json` (ex.: `Development`, `Docker`)
3. Variáveis de ambiente (docker-compose) — **maior precedência**

Exemplos no compose:

```yaml
ConnectionStrings__OrderingDb: Host=postgres;...
RabbitMq__Host: rabbitmq
ASPNETCORE_ENVIRONMENT: Docker
OpenTelemetry__PrometheusEnabled: "true"
```

## Stack local (Docker Compose)

| Serviço | URL |
|---------|-----|
| ApiGateway | http://localhost:5000 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (admin/admin) |
| Seq | http://localhost:5341 |
| RabbitMQ UI | http://localhost:15672 |

```bash
docker compose up --build
```

## Runbooks

- [Mensagem em `_error`](./runbooks/consumer-message-in-error-queue.md)
- [Outbox preso](./runbooks/outbox-messages-stuck.md)
- [Serviço unhealthy](./runbooks/service-unhealthy.md)
- [Banco indisponível](./runbooks/database-unavailable.md)
- [RabbitMQ indisponível](./runbooks/rabbitmq-unavailable.md)

## Observabilidade

- Logs: [observability-seq.md](./observability-seq.md)
- Métricas: [observability-prometheus.md](./observability-prometheus.md)

## Graceful shutdown

- `OutboxDispatcher` respeita `CancellationToken`.
- Cada mensagem do outbox é persistida **individualmente** após tentativa de publicação.
- Cancelamento **não** marca `ProcessedAtUtc` se a publicação não concluiu.
