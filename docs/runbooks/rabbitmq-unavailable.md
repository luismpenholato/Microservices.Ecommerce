# Runbook: RabbitMQ unavailable

## Symptom

- `/health/ready` Unhealthy with `rabbitmq` check.
- MassTransit cannot connect; connection refused logs.
- Outbox with pending messages and `last_error` related to the broker.
- Consumers stop processing.

## How to identify

### Health

```bash
curl -s http://localhost:5003/health/ready
curl -s http://localhost:5004/health/ready
```

Services with RabbitMQ in ready: Ordering, Inventory, Payment.Worker, Notification.Worker.

### Docker

```bash
docker compose ps rabbitmq
docker compose logs rabbitmq --tail 50
```

Management UI: `http://localhost:15672` — if it does not open, broker is down.

### Metrics and logs

- `ecommerce_outbox_messages_publish_failures_total` increases.
- `ecommerce_outbox_messages_pending` increases.
- Seq: `outbox publish failed`, `Broker unreachable`, MassTransit errors.

## Impact

- **Consumption** of events stops (after retry exhaustion → `_error`).
- **Publishing** via outbox fails; orders may stall without Payment/Inventory/Notification.
- HTTP order creation may still work (Ordering writes DB + local outbox).

## Recommended action

1. Start the broker: `docker compose up -d rabbitmq`.
2. Wait for healthcheck `healthy`.
3. Restart services that cached a failed connection:
   ```bash
   docker compose restart ordering-api inventory-api payment-worker notification-worker
   ```
4. **Do not** delete queues without understanding the impact.
5. Monitor pending outbox decreasing after the broker recovers.

## How to validate recovery

1. Management UI accessible; consumer queues exist.
2. `/health/ready` → rabbitmq Healthy.
3. `ecommerce_outbox_messages_published_total` increases.
4. `ecommerce_consumer_messages_processed_total` increases.
5. Test order progresses beyond `Pending`.

## Configuration (Docker)

| Variable | Example |
|----------|---------|
| `RabbitMq__Host` | `rabbitmq` |
| `RabbitMq__Username` | `guest` |
| `RabbitMq__Password` | `guest` |

See [operations.md](../operations.md) for configuration precedence.
