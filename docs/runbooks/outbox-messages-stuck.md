# Runbook: mensagens presas no outbox

## Sintoma

- Pedido criado, mas `OrderCreatedEvent` (ou outro evento) não aparece no RabbitMQ.
- Downstream (Payment, Inventory) não reage.
- Logs do `OutboxDispatcher` com falhas de publicação ou silêncio prolongado.

## Como identificar

### Banco do serviço publicador

```sql
SELECT id, event_id, event_type, retry_count, last_error, created_at_utc, processed_at_utc
FROM outbox_messages
WHERE processed_at_utc IS NULL
ORDER BY created_at_utc;
```

| Situação | Significado |
|----------|-------------|
| `processed_at_utc IS NULL` e `retry_count < MaxPublishRetries` | Pendente, ainda em retry |
| `retry_count >= MaxPublishRetries` | Retries de publicação **esgotados** |
| `last_error` preenchido | Última falha do broker/serialização |

### Prometheus

- `ecommerce_outbox_messages_pending{service="OrderingService"}` > 0 por tempo prolongado.
- `ecommerce_outbox_messages_exhausted_total` aumentando.

### Logs (Seq)

```sql
@Message like '%outbox publish failed%' or @Message like '%outbox publish exhausted%'
```

## Impacto

- Eventos **não** chegam ao broker; fluxo assíncrono para após o ponto do outbox.
- HTTP/checkout pode ter sucesso enquanto o processo assíncrono está parado.

## Ação recomendada

1. Verifique **RabbitMQ** ([runbook rabbitmq](./rabbitmq-unavailable.md)).
2. Verifique **PostgreSQL** do serviço ([runbook database](./database-unavailable.md)).
3. Leia `last_error` na linha do outbox.
4. Após corrigir broker/rede:
   - O `OutboxDispatcher` deve publicar na próxima iteração (`Outbox:PollIntervalSeconds`).
   - Não é necessário alterar a linha se `retry_count` ainda permitir retry.
5. Se retries esgotados:
   - Corrija a causa.
   - Opcional (operação manual avançada): reset controlado de `retry_count` e `last_error` **apenas** após garantir que o evento não foi publicado (sem duplicar no broker — `EventId` único no outbox ajuda).

## Como validar recuperação

1. `processed_at_utc` preenchido na linha do outbox.
2. Log: `outbox message published` com `EventId` correspondente.
3. `ecommerce_outbox_messages_pending` diminui.
4. Consumer downstream processa (ver logs/métricas).

## Configuração relacionada

| Seção | Chave | Padrão local |
|-------|-------|----------------|
| `Outbox` | `MaxPublishRetries` | 5 |
| `Outbox` | `PollIntervalSeconds` | 2 |
