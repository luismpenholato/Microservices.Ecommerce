# Runbook: message in `_error` queue

## Symptom

- Order stuck in intermediate status (`Pending`, `PaymentApproved`, etc.).
- Expected event does not reach the next service.
- Normal consumer queue empty, but messages in `*_error` queue in RabbitMQ.

## How to identify

### RabbitMQ Management

1. Open `http://localhost:15672` (guest/guest in local environment).
2. **Queues** tab.
3. Look for queues ending in `_error`, for example:
   - `ordering-service-payment-approved-consumer_error`
   - `inventory-service-payment-approved-consumer_error`
4. **Ready** > 0 indicates messages awaiting manual intervention.

### Logs (Seq)

```sql
@Message like '%Consume fault%' or @Level = 'Error'
```

Filter by `ConsumerName`, `EventId`, `CorrelationId`, `OrderId`.

## Useful logs and queries

| Where | What to search |
|-------|----------------|
| Seq | `Consume fault`, `MessageType`, `ConsumerName` |
| PostgreSQL (consuming service) | `processed_integration_events` **without** row for `EventId` |
| Prometheus metric | `ecommerce_consumer_messages_failed_total` increasing |

```sql
-- Ordering: event not processed
SELECT * FROM processed_integration_events
WHERE event_id = '<EventId>';
```

## Impact

- Order flow **interrupted** for that event.
- **No automatic compensation** or replay.
- Other services' outbox may keep publishing, causing partial inconsistency.

## Recommended action

1. Identify **root cause** in exception log (constraint, timeout, permanent bug).
2. If **transient failure already resolved** (DB/broker recovered):
   - In Management UI: open the `_error` queue.
   - Use **Get messages** to inspect payload (`EventId`, `OrderId`).
   - **Requeue** or manually republish **only** if idempotency is guaranteed (`EventId` + `ConsumerName`).
3. If **permanent bug** (poison message):
   - Fix code/data.
   - Deploy.
   - Reprocess or discard the message with `EventId` recorded.
4. **Do not** delete the `_error` queue without analyzing messages.

## How to validate recovery

1. `_error` queue has no new messages for the handled case (or message reprocessed successfully).
2. Log: `Integration event committed` with same `ConsumerName` and `EventId` (or idempotent skip if already processed).
3. Order status progressed in Ordering (API or database query).
4. Metric `ecommerce_consumer_messages_processed_total` increases for the consumer.

## References

- [ADR 0008 — Retry and error queues](../decisions/0008-retry-dlq-error-queues.md)
- [service-communication.md](../service-communication.md)
