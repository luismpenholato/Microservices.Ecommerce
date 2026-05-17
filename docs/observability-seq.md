# Rastreamento de pedidos no Seq

## Pré-requisitos

- `docker compose up` com o serviço **Seq** (`http://localhost:5341`).
- Serviços configurados com Serilog (APIs via `AddObservability`, workers via `AddWorkerObservability`).

## Propriedades estruturadas principais

| Propriedade | Origem | Uso |
|-------------|--------|-----|
| `Service` | Enricher global | Filtrar por microserviço |
| `CorrelationId` | Header `X-Correlation-Id` / eventos | Rastrear fluxo ponta a ponta |
| `OrderId` | Handlers e eventos com pedido | Foco no pedido |
| `EventId` | `IntegrationEvent` | Idempotência / reprocessamento |
| `ConsumerName` | Consumer MassTransit | Qual handler processou |
| `MessageType` | Tipo do evento | Ex.: `PaymentApprovedEvent` |
| `OutboxId` | OutboxDispatcher | Publicação pendente/falha |

## Consultas úteis no Seq

### 1. Seguir um pedido pelo `OrderId`

```sql
OrderId = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
```

Ordene por `@Timestamp` ascendente para ver a linha do tempo.

### 2. Seguir o fluxo distribuído pelo `CorrelationId`

```sql
CorrelationId = 'yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy'
```

Inclui checkout HTTP, consumers e outbox do mesmo fluxo.

### 3. Investigar falha de consumer (antes da error queue)

```sql
@Level = 'Error' and ConsumerName is not null
```

Ou:

```sql
@Message like '%Consume fault%'
```

Campos esperados: `MessageType`, `ConsumerName`, `EventId`, `CorrelationId`, `OrderId`.

### 4. Outbox pendente ou com falha de publicação

```sql
Service = 'OrderingService' and (@Message like '%outbox publish failed%' or @Message like '%outbox publish exhausted%')
```

Verifique `OutboxId`, `EventId`, `RetryCount` no texto ou propriedades.

### 5. Evento já processado (idempotência)

```sql
@Message like '%already processed%' and ConsumerName is not null
```

Indica reentrega ignorada com segurança.

## Fluxo típico de logs (pedido feliz)

1. **BasketService** — checkout com `CorrelationId`.
2. **OrderingService** — pedido criado + outbox `OrderCreatedEvent` publicado.
3. **Payment.Worker** — pagamento aprovado/rejeitado (outbox).
4. **OrderingService** — consumer `PaymentApprovedConsumer` (transição de status).
5. **InventoryService** — reserva de estoque (outbox `StockReservedEvent`).
6. **OrderingService** — `StockReservedConsumer` → `Completed` + outbox `OrderCompletedEvent`.
7. **Notification.Worker** — notificação simulada.

## O que não aparece no Seq

- Mensagens na fila `{endpoint}_error` do RabbitMQ (inspecionar no Management UI: `http://localhost:15672`).
- Replay manual de error queue (operação fora da aplicação).

## Métricas (Prometheus)

Correlacione picos em Seq com dashboards Grafana — ver [observability-prometheus.md](./observability-prometheus.md).

## Boas práticas

- Propague `X-Correlation-Id` em chamadas HTTP de teste e Postman.
- Use `OrderId` após o checkout para correlacionar com eventos.
- Falhas técnicas de consumer: log `Consume fault` (nível Error) + métrica `ecommerce_consumer_messages_failed_total`.
- Transições de negócio: nível Information nos handlers Application.
- Não use `EventId`/`OrderId` como labels de métricas (alta cardinalidade).
