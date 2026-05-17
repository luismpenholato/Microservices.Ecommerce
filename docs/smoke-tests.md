# Smoke tests — ambiente local

Roteiro manual para validar o stack após clonar o repositório. Para checagens automatizadas de infraestrutura, use os scripts em [scripts/](../scripts/).

## Pré-requisitos

- Docker Desktop (ou Docker Engine + Compose v2)
- Portas livres: `5000`–`5004`, `5010`–`5011`, `5432`, `6379`, `5672`, `15672`, `5341`, `9090`, `3000`
- (Opcional) `curl` ou PowerShell para chamadas HTTP manuais

## 1. Subir o ambiente

```bash
docker compose up -d --build
```

Ou validação automatizada:

```powershell
# Infraestrutura + health + metrics + rotas básicas
.\scripts\validate-local.ps1

# Inclui checkout ponta a ponta + idempotência HTTP no Ordering
.\scripts\validate-local.ps1 -RunCheckoutFlow
```

```bash
chmod +x scripts/validate-local.sh
./scripts/validate-local.sh
./scripts/validate-local.sh --run-checkout-flow
```

O modo E2E do script:

1. Obtém o primeiro produto do Catalog via Gateway
2. Valida estoque no Inventory
3. Adiciona item ao Basket e faz checkout
4. Poll em `GET /ordering/orders/{orderId}` a cada 3s (padrão: timeout 120s)
5. Exige status final `Completed` (até 3 tentativas se pagamento simulado rejeitar)
6. Repete `POST /ordering/orders` com a mesma `Idempotency-Key` e compara `id`

Aguarde todos os healthchecks ficarem `healthy` (`docker compose ps`).

## 2. Autenticar

```bash
curl -s -X POST http://localhost:5000/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@ecommerce.local","password":"Demo123!"}'
```

Guarde `accessToken` e `customerId` para os passos de Basket/Ordering.

## 3. Consultar catálogo

Via Gateway:

```bash
curl -s http://localhost:5000/catalog/products
```

Esperado: lista JSON com produtos seed (ex.: `Notebook Pro`, id `11111111-1111-1111-1111-111111111101`).

## 4. Consultar estoque

```bash
curl -s http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101
```

Esperado: `availableQuantity` > 0 (seed do Inventory).

## 4. Carrinho e checkout

Defina um `customerId` (GUID qualquer):

```bash
CUSTOMER_ID="<customerId do login>"
TOKEN="<accessToken do login>"
PRODUCT_ID=11111111-1111-1111-1111-111111111101

curl -s -X POST "http://localhost:5000/basket/baskets/$CUSTOMER_ID/items" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"productId\":\"$PRODUCT_ID\",\"productName\":\"Notebook Pro\",\"unitPrice\":5499.90,\"quantity\":1}"

curl -s -X POST "http://localhost:5000/basket/baskets/$CUSTOMER_ID/checkout" \
  -H "Authorization: Bearer $TOKEN"
```

Esperado no checkout: JSON com `orderId`. O Basket gera o header `Idempotency-Key` automaticamente para o Ordering.

## 6. Acompanhar pedido

```bash
# substitua ORDER_ID pelo retorno do checkout
curl -s http://localhost:5000/ordering/orders/ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

Fluxo assíncrono esperado (alguns segundos):

1. `Pending` → após pagamento simulado
2. `PaymentApproved` / processamento de estoque
3. `Completed` (ou `Failed` / `Cancelled` em cenários de falha)

## 6. Logs no Seq

1. Abra http://localhost:5341
2. Filtros úteis:
   - `Service = 'OrderingService'` e `OrderId` do checkout
   - `ConsumerName` em workers (`PaymentService`, `InventoryService`, …)
   - `@Level = 'Error'` para falhas de consumer

Guia detalhado: [observability-seq.md](./observability-seq.md)

## 7. Métricas no Prometheus e Grafana

1. Prometheus: http://localhost:9090 — targets `up` para APIs e workers
2. Grafana: http://localhost:3000 (admin/admin) — dashboard **Microservices Ecommerce**

Métricas a observar após checkout:

| Métrica | O que indica |
|---------|----------------|
| `ecommerce_orders_created_total` | Pedido criado |
| `ecommerce_orders_completed_total` | Fluxo concluído |
| `ecommerce_consumer_messages_processed_total` | Consumers processando eventos |
| `ecommerce_outbox_messages_pending` | Fila de outbox (deve voltar a ~0) |

Guia: [observability-prometheus.md](./observability-prometheus.md)

## 8. RabbitMQ (opcional)

- UI: http://localhost:15672 (guest/guest)
- Verifique filas dos endpoints após checkout; filas `*_error` devem permanecer vazias em fluxo feliz.

## Validação automatizada E2E

```powershell
.\scripts\validate-local.ps1 -SkipCompose -RunCheckoutFlow
```

Variável de ambiente (Bash): `CHECKOUT_POLL_TIMEOUT_SECONDS=180`

## Critérios de sucesso

- [ ] `validate-local.ps1` ou `.sh` termina com sucesso
- [ ] Com `-RunCheckoutFlow` / `--run-checkout-flow`: pedido em `Completed` e idempotência OK
- [ ] Checkout retorna `orderId`
- [ ] Pedido evolui para `Completed` no Ordering
- [ ] Seq mostra logs de transição sem `Error` inesperado
- [ ] Grafana/Prometheus exibem incremento nas métricas `ecommerce_*`

## Se algo falhar

Consulte os [runbooks](./runbooks/):

- [service-unhealthy.md](./runbooks/service-unhealthy.md)
- [outbox-messages-stuck.md](./runbooks/outbox-messages-stuck.md)
- [consumer-message-in-error-queue.md](./runbooks/consumer-message-in-error-queue.md)
- [database-unavailable.md](./runbooks/database-unavailable.md)
- [rabbitmq-unavailable.md](./runbooks/rabbitmq-unavailable.md)
