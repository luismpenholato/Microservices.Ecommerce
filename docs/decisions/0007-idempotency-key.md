# ADR 0007: Idempotency-Key no checkout

## Status

Aceito

## Contexto

Retries HTTP no checkout podem criar pedidos duplicados.

## Decisão

Basket gera `Idempotency-Key` determinística por conteúdo do carrinho e Ordering persiste `order_idempotency_records`.

- Mesma chave + mesmo payload → retorna o mesmo `OrderId`
- Mesma chave + payload diferente → HTTP 409 Conflict

## Consequências

- Checkout seguro para retry
- Necessidade de armazenar hash do payload por chave
