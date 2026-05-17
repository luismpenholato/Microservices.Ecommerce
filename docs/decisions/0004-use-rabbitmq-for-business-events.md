# ADR 0004: RabbitMQ para eventos de negócio

## Status

Aceito

## Decisão

MassTransit + RabbitMQ com retry configurável (`MessageBusOptions`), error queues `{endpoint}_error` e consumidores idempotentes. Ver também [ADR 0008](./0008-retry-dlq-error-queues.md).

## Motivo

Maturidade, simplicidade local via Docker e adequação a workflows de pedido.
