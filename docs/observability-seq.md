# Order tracing in Seq

## Prerequisites

- `docker compose up` with the **Seq** service (`http://localhost:5341`).
- Services configured with Serilog (APIs via `AddObservability`, workers via `AddWorkerObservability`).

## Main structured properties

| Property | Source | Use |
|----------|--------|-----|
| `Service` | Global enricher | Filter by microservice |
| `CorrelationId` | Header `X-Correlation-Id` / events | End-to-end flow tracing |
| `OrderId` | Handlers and events with order | Focus on the order |
| `EventId` | `IntegrationEvent` | Idempotency / reprocessing |
| `ConsumerName` | MassTransit consumer | Which handler processed |
| `MessageType` | Event type | e.g. `PaymentApprovedEvent` |
| `OutboxId` | OutboxDispatcher | Pending/failed publication |

## Useful Seq queries

### 1. Follow an order by `OrderId`

```sql
OrderId = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
```

Sort by `@Timestamp` ascending to see the timeline.

### 2. Follow the distributed flow by `CorrelationId`

```sql
CorrelationId = 'yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy'
```

Includes HTTP checkout, consumers, and outbox for the same flow.

### 3. Investigate consumer failure (before error queue)

```sql
@Level = 'Error' and ConsumerName is not null
```

Or:

```sql
@Message like '%Consume fault%'
```

Expected fields: `MessageType`, `ConsumerName`, `EventId`, `CorrelationId`, `OrderId`.

### 4. Pending outbox or publish failure

```sql
Service = 'OrderingService' and (@Message like '%outbox publish failed%' or @Message like '%outbox publish exhausted%')
```

Check `OutboxId`, `EventId`, `RetryCount` in text or properties.

### 5. Already processed event (idempotency)

```sql
@Message like '%already processed%' and ConsumerName is not null
```

Indicates safe redelivery was ignored.

## Typical log flow (happy path order)

1. **BasketService** — checkout with `CorrelationId`.
2. **OrderingService** — order created + outbox `OrderCreatedEvent` published.
3. **Payment.Worker** — payment approved/rejected (outbox).
4. **OrderingService** — consumer `PaymentApprovedConsumer` (status transition).
5. **InventoryService** — stock reservation (outbox `StockReservedEvent`).
6. **OrderingService** — `StockReservedConsumer` → `Completed` + outbox `OrderCompletedEvent`.
7. **Notification.Worker** — simulated notification.

## What does not appear in Seq

- Messages in RabbitMQ `{endpoint}_error` queue (inspect in Management UI: `http://localhost:15672`).
- Manual error queue replay (operation outside the application).

## Metrics (Prometheus)

Correlate spikes in Seq with Grafana dashboards — see [observability-prometheus.md](./observability-prometheus.md).

## Best practices

- Propagate `X-Correlation-Id` in HTTP test calls and Postman.
- Use `OrderId` after checkout to correlate with events.
- Consumer technical failures: `Consume fault` log (Error level) + `ecommerce_consumer_messages_failed_total` metric.
- Business transitions: Information level in Application handlers.
- Do not use `EventId`/`OrderId` as metric labels (high cardinality).
