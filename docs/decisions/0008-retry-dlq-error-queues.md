# ADR 0008: Retry, error queues, and manual reprocessing

## Status

Accepted

## Context

MassTransit consumers and `OutboxDispatcher` can fail due to transient unavailability (network, database, broker). We need clear retry limits and a destination for messages that do not recover automatically.

## Decision

### 1. Consumer retry (MassTransit)

Configuration in `MessageBusOptions` (`MessageBus:RetryLimit`, `MessageBus:RetryIntervalSeconds`):

- Applied via `UseMessageRetry` on RabbitMQ endpoints.
- **Does not** write `processed_integration_events` on failure (transaction rolled back in `IntegrationEventUnitOfWork`).
- After retries exhausted, message goes to the endpoint error queue.

### 2. Outbox retry (publishing)

Separate configuration in `OutboxOptions` (`Outbox:MaxPublishRetries`, `Outbox:PollIntervalSeconds`, etc.):

- Independent from consumer retry.
- Increments `RetryCount` in `outbox_messages` table.
- Do not confuse with MassTransit retry.

### 3. Error queues (`{endpoint}_error`)

With RabbitMQ + MassTransit (kebab-case):

- After consumer failures, message moves to `{endpoint-name}_error`.
- Example: `payment-approved-consumer` → `payment-approved-consumer_error`.
- **No automatic compensation** or automatic replay for the business flow.
- Reprocessing is a **manual operational action** (requeue, fix and republish, or conscious discard).

### 4. Technical failure observability

`IntegrationEventConsumeObserver` logs in structured form:

- `MessageType`, `ConsumerName`, `EventId`, `CorrelationId`, `OrderId` (when applicable), exception.
- Does not replace business logs in Application handlers.

### 5. Test hook

`IConsumerExecutionFaultHook` with `NoOp` implementation in production; tests substitute to simulate transient failure without affecting Application.

## Consequences

### Positive

- Clear separation between consumption failure and publish failure.
- Transactional idempotency + retry reduces duplicate effects.
- Error queues allow inspection in RabbitMQ Management.

### Negative / trade-offs

- Messages in `_error` require operational runbook.
- Poison messages remain until intervention.
- Retry increases latency under failure.
- Stock concurrency may cause `DbUpdateConcurrencyException` and additional retries (expected).

## Alternatives considered

- **Kafka DLQ**: out of scope.
- **Automatic error queue replay**: complexity and risk of reprocessing without idempotency; deferred.
- **Centralized saga**: deferred; explicit compensation in the future.
