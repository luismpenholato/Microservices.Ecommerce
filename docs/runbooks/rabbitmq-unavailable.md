# Runbook: RabbitMQ indisponível

## Sintoma

- `/health/ready` Unhealthy com check `rabbitmq`.
- MassTransit não conecta; logs de connection refused.
- Outbox com mensagens pendentes e `last_error` relacionado ao broker.
- Consumers param de processar.

## Como identificar

### Health

```bash
curl -s http://localhost:5003/health/ready
curl -s http://localhost:5004/health/ready
```

Serviços com RabbitMQ no ready: Ordering, Inventory, Payment.Worker, Notification.Worker.

### Docker

```bash
docker compose ps rabbitmq
docker compose logs rabbitmq --tail 50
```

Management UI: `http://localhost:15672` — se não abre, broker down.

### Métricas e logs

- `ecommerce_outbox_messages_publish_failures_total` sobe.
- `ecommerce_outbox_messages_pending` sobe.
- Seq: `outbox publish failed`, `Broker unreachable`, MassTransit errors.

## Impacto

- **Consumo** de eventos interrompido (após esgotar retry → `_error`).
- **Publicação** via outbox falha; pedidos podem ficar sem Payment/Inventory/Notification.
- HTTP de criação de pedido pode funcionar (Ordering grava DB + outbox local).

## Ação recomendada

1. Suba o broker: `docker compose up -d rabbitmq`.
2. Aguarde healthcheck `healthy`.
3. Reinicie serviços que cacheiam conexão falha:
   ```bash
   docker compose restart ordering-api inventory-api payment-worker notification-worker
   ```
4. **Não** apague filas sem entender impacto.
5. Monitore outbox pendente diminuir após broker voltar.

## Como validar recuperação

1. Management UI acessível; filas de consumers existem.
2. `/health/ready` → rabbitmq Healthy.
3. `ecommerce_outbox_messages_published_total` aumenta.
4. `ecommerce_consumer_messages_processed_total` aumenta.
5. Pedido de teste progride além de `Pending`.

## Configuração (Docker)

| Variável | Exemplo |
|----------|---------|
| `RabbitMq__Host` | `rabbitmq` |
| `RabbitMq__Username` | `guest` |
| `RabbitMq__Password` | `guest` |

Ver [operations.md](../operations.md) para precedência de configuração.
