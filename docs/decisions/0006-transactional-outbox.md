# ADR 0006: Transactional Outbox

## Status

Aceito

## Contexto

Publicar eventos diretamente após `SaveChanges` pode deixar o sistema inconsistente se o broker falhar.

## Decisão

Persistir eventos em `outbox_messages` na mesma transação do estado de negócio e publicar via `OutboxDispatcher` (BackgroundService) com retry.

## Consequências

- Publicação eventual (latência de segundos)
- Consistência forte entre banco e intenção de publicação
- Dispatcher idempotente por `EventId` único na outbox
