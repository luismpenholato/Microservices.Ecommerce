# ADR 0008: Retry, error queues e reprocessamento manual

## Status

Aceito

## Contexto

Consumidores MassTransit e `OutboxDispatcher` podem falhar por indisponibilidade transitória (rede, banco, broker). Precisamos de limites claros de retry e destino das mensagens que não recuperam automaticamente.

## Decisão

### 1. Retry de consumer (MassTransit)

Configuração em `MessageBusOptions` (`MessageBus:RetryLimit`, `MessageBus:RetryIntervalSeconds`):

- Aplicado via `UseMessageRetry` nos endpoints RabbitMQ.
- **Não** marca `processed_integration_events` em falha (transação revertida no `IntegrationEventUnitOfWork`).
- Após esgotar retries, a mensagem vai para a fila de erro do endpoint.

### 2. Retry de outbox (publicação)

Configuração separada em `OutboxOptions` (`Outbox:MaxPublishRetries`, `Outbox:PollIntervalSeconds`, etc.):

- Independente do retry de consumer.
- Incrementa `RetryCount` na tabela `outbox_messages`.
- Não confundir com retry MassTransit.

### 3. Error queues (`{endpoint}_error`)

Com RabbitMQ + MassTransit (kebab-case):

- Após falhas no consumer, a mensagem é movida para `{nome-do-endpoint}_error`.
- Exemplo: `payment-approved-consumer` → `payment-approved-consumer_error`.
- **Não há compensação automática** nem replay automático para o fluxo de negócio.
- Reprocessamento é **ação operacional manual** (requeue, correção e republicação, ou descarte consciente).

### 4. Observabilidade de falhas técnicas

`IntegrationEventConsumeObserver` registra em log estruturado:

- `MessageType`, `ConsumerName`, `EventId`, `CorrelationId`, `OrderId` (quando aplicável), exceção.
- Não substitui logs de negócio nos handlers Application.

### 5. Hook de teste

`IConsumerExecutionFaultHook` com implementação `NoOp` em produção; testes substituem para simular falha transitória sem afetar Application.

## Consequências

### Positivas

- Separação clara entre falha de consumo e falha de publicação.
- Idempotência transacional + retry reduz duplicidade de efeito.
- Error queues permitem inspeção no RabbitMQ Management.

### Negativas / trade-offs

- Mensagens em `_error` exigem runbook operacional.
- Poison messages permanecem até intervenção.
- Retry aumenta latência sob falha.
- Concorrência de estoque pode gerar `DbUpdateConcurrencyException` e retries adicionais (esperado).

## Alternativas consideradas

- **Kafka DLQ**: fora de escopo.
- **Replay automático da error queue**: complexidade e risco de reprocessar sem idempotência; adiado.
- **Saga centralizada**: adiado; compensação explícita futura.
