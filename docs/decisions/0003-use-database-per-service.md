# ADR 0003: Database per Service

## Status

Accepted

## Decision

Each service with its own persistence: `catalog_db`, `ordering_db`, Redis for Basket.

## Rule

No service accesses another service's database directly.
