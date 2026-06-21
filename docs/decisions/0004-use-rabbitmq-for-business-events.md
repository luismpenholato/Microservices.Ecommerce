# ADR 0004: RabbitMQ for business events

## Status

Accepted

## Decision

MassTransit + RabbitMQ with configurable retry (`MessageBusOptions`), error queues `{endpoint}_error`, and idempotent consumers. See also [ADR 0008](./0008-retry-dlq-error-queues.md).

## Rationale

Maturity, local simplicity via Docker, and fit for order workflows.
