# ADR 0005: Eventual consistency

## Status

Accepted

## Decision

Order status updated by events (`PaymentApproved`, `StockReserved`, etc.).

## Trade-off

Latency between states acceptable in exchange for decoupling and resilience.
