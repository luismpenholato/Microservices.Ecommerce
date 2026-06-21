# Operations — Microservices.Ecommerce

Guide for demonstrable local operation (no Kafka/Alertmanager in production).

## Local validation

Use the manual checklist in [smoke-tests.md](./smoke-tests.md). Security: [security.md](./security.md). Testing: [testing.md](./testing.md). curl examples: [examples/http-requests.md](./examples/http-requests.md).

## Operational endpoints

| Endpoint | APIs | Workers |
|----------|------|---------|
| `GET /health/live` | Yes | Yes |
| `GET /health/ready` | Yes | Yes |
| `GET /metrics` | Yes | Yes |

- **live**: process responds (does not validate dependencies).
- **ready**: real dependencies for the service (see table below).

## Health checks per service

| Service | ready validates |
|---------|-----------------|
| Identity | PostgreSQL `identity_db` |
| ApiGateway | YARP configuration (`gateway`) |
| Catalog | PostgreSQL `catalog_db` |
| Basket | Redis |
| Ordering | PostgreSQL + RabbitMQ |
| Inventory | PostgreSQL + RabbitMQ |
| Payment.Worker | PostgreSQL + RabbitMQ |
| Notification.Worker | PostgreSQL + RabbitMQ |

## Metrics (`ecommerce_*`)

Exposed at `/metrics` (Prometheus via OpenTelemetry).

| Metric | Type | Labels |
|--------|------|--------|
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

## Configuration (precedence)

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g. `Development`, `Docker`)
3. Environment variables (docker-compose) — **highest precedence**

Examples in compose:

```yaml
ConnectionStrings__OrderingDb: Host=postgres;...
RabbitMq__Host: rabbitmq
ASPNETCORE_ENVIRONMENT: Docker
OpenTelemetry__PrometheusEnabled: "true"
```

## Local stack (Docker Compose)

| Service | URL |
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

- [Message in `_error`](./runbooks/consumer-message-in-error-queue.md)
- [Stuck outbox](./runbooks/outbox-messages-stuck.md)
- [Unhealthy service](./runbooks/service-unhealthy.md)
- [Database unavailable](./runbooks/database-unavailable.md)
- [RabbitMQ unavailable](./runbooks/rabbitmq-unavailable.md)

## Observability

- Logs: [observability-seq.md](./observability-seq.md)
- Metrics: [observability-prometheus.md](./observability-prometheus.md)

## Graceful shutdown

- `OutboxDispatcher` respects `CancellationToken`.
- Each outbox message is persisted **individually** after a publish attempt.
- Cancellation **does not** set `ProcessedAtUtc` if publication did not complete.
