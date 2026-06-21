# ADR 0006: Transactional Outbox

## Status

Accepted

## Context

Publishing events directly after `SaveChanges` can leave the system inconsistent if the broker fails.

## Decision

Persist events in `outbox_messages` in the same transaction as business state and publish via `OutboxDispatcher` (BackgroundService) with retry.

## Consequences

- Eventual publication (seconds of latency)
- Strong consistency between database and publish intent
- Idempotent dispatcher via unique `EventId` in outbox
