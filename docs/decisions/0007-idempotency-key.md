# ADR 0007: Idempotency-Key on checkout

## Status

Accepted

## Context

HTTP retries on checkout can create duplicate orders.

## Decision

Basket generates a deterministic `Idempotency-Key` from cart content and Ordering persists `order_idempotency_records`.

- Same key + same payload → returns the same `OrderId`
- Same key + different payload → HTTP 409 Conflict

## Consequences

- Safe checkout for retry
- Need to store payload hash per key
