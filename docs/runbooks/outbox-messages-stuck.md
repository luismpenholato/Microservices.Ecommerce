# Runbook: messages stuck in outbox

## Symptom

- Order created, but `OrderCreatedEvent` (or other event) does not appear in RabbitMQ.
- Downstream (Payment, Inventory) does not react.
- `OutboxDispatcher` logs with publish failures or prolonged silence.

## How to identify

### Publishing service database

```sql
SELECT id, event_id, event_type, retry_count, last_error, created_at_utc, processed_at_utc
FROM outbox_messages
WHERE processed_at_utc IS NULL
ORDER BY created_at_utc;
```

| Situation | Meaning |
|-----------|---------|
| `processed_at_utc IS NULL` and `retry_count < MaxPublishRetries` | Pending, still retrying |
| `retry_count >= MaxPublishRetries` | Publish retries **exhausted** |
| `last_error` populated | Last broker/serialization failure |

### Prometheus

- `ecommerce_outbox_messages_pending{service="OrderingService"}` > 0 for extended time.
- `ecommerce_outbox_messages_exhausted_total` increasing.

### Logs (Seq)

```sql
@Message like '%outbox publish failed%' or @Message like '%outbox publish exhausted%'
```

## Impact

- Events **do not** reach the broker; async flow stops at the outbox point.
- HTTP/checkout may succeed while the async process is stalled.

## Recommended action

1. Check **RabbitMQ** ([rabbitmq runbook](./rabbitmq-unavailable.md)).
2. Check service **PostgreSQL** ([database runbook](./database-unavailable.md)).
3. Read `last_error` on the outbox row.
4. After fixing broker/network:
   - `OutboxDispatcher` should publish on the next iteration (`Outbox:PollIntervalSeconds`).
   - No row change needed if `retry_count` still allows retry.
5. If retries exhausted:
   - Fix the cause.
   - Optional (advanced manual operation): controlled reset of `retry_count` and `last_error` **only** after confirming the event was not published (avoid broker duplication — unique `EventId` in outbox helps).

## How to validate recovery

1. `processed_at_utc` populated on the outbox row.
2. Log: `outbox message published` with matching `EventId`.
3. `ecommerce_outbox_messages_pending` decreases.
4. Downstream consumer processes (see logs/metrics).

## Related configuration

| Section | Key | Local default |
|---------|-----|---------------|
| `Outbox` | `MaxPublishRetries` | 5 |
| `Outbox` | `PollIntervalSeconds` | 2 |
