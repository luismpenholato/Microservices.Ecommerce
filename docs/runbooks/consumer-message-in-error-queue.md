# Runbook: mensagem na fila `_error`

## Sintoma

- Pedido parado em status intermediário (`Pending`, `PaymentApproved`, etc.).
- Evento esperado não chega ao próximo serviço.
- Fila de consumo normal vazia, mas há mensagens em fila `*_error` no RabbitMQ.

## Como identificar

### RabbitMQ Management

1. Acesse `http://localhost:15672` (guest/guest no ambiente local).
2. Aba **Queues**.
3. Procure filas terminando em `_error`, por exemplo:
   - `ordering-service-payment-approved-consumer_error`
   - `inventory-service-payment-approved-consumer_error`
4. **Ready** > 0 indica mensagens aguardando intervenção manual.

### Logs (Seq)

```sql
@Message like '%Consume fault%' or @Level = 'Error'
```

Filtre por `ConsumerName`, `EventId`, `CorrelationId`, `OrderId`.

## Logs e queries úteis

| Onde | O que buscar |
|------|----------------|
| Seq | `Consume fault`, `MessageType`, `ConsumerName` |
| PostgreSQL (serviço consumidor) | `processed_integration_events` **sem** linha para o `EventId` |
| Métrica Prometheus | `ecommerce_consumer_messages_failed_total` aumentando |

```sql
-- Ordering: evento não processado
SELECT * FROM processed_integration_events
WHERE event_id = '<EventId>';
```

## Impacto

- Fluxo do pedido **interrompido** para aquele evento.
- **Não há compensação automática** nem replay.
- Outbox de outros serviços pode continuar publicando, gerando inconsistência parcial.

## Ação recomendada

1. Identifique a **causa raiz** no log da exceção (constraint, timeout, bug permanente).
2. Se for falha **transitória já resolvida** (DB/broker voltou):
   - No Management UI: abra a fila `_error`.
   - Use **Get messages** para inspecionar payload (`EventId`, `OrderId`).
   - **Requeue** ou republicar manualmente **somente** se idempotência estiver garantida (`EventId` + `ConsumerName`).
3. Se for **bug permanente** (poison message):
   - Corrija o código/dados.
   - Faça deploy.
   - Reprocesse ou descarte a mensagem com registro do `EventId`.
4. **Não** delete a fila `_error` sem analisar as mensagens.

## Como validar recuperação

1. Fila `_error` sem mensagens novas para o caso tratado (ou mensagem reprocessada com sucesso).
2. Log: `Integration event committed` com mesmo `ConsumerName` e `EventId` (ou skip idempotente se já processado).
3. Status do pedido evoluiu no Ordering (consulta API ou banco).
4. Métrica `ecommerce_consumer_messages_processed_total` aumenta para o consumer.

## Referências

- [ADR 0008 — Retry e error queues](../decisions/0008-retry-dlq-error-queues.md)
- [service-communication.md](../service-communication.md)
