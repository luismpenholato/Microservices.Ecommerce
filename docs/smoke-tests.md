# Smoke tests — local environment

Manual checklist to validate the stack after cloning the repository.

## Prerequisites

- Docker Desktop (or Docker Engine + Compose v2)
- Free ports: `5000`–`5004`, `5010`–`5011`, `5432`, `6379`, `5672`, `15672`, `5341`, `9090`, `3000`
- `curl` or PowerShell for HTTP calls

## 1. Start the environment

```bash
docker compose up -d --build
```

Wait for all healthchecks to become `healthy` (`docker compose ps`).

Quick sanity checks:

```bash
curl -s http://localhost:5000/health/ready
curl -s http://localhost:5000/catalog/products
curl -s http://localhost:5000/metrics | head
```

## 2. Authenticate

```bash
curl -s -X POST http://localhost:5000/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@ecommerce.local","password":"Demo123!"}'
```

Save `accessToken` and `customerId` for Basket/Ordering steps.

## 3. Query catalog

Via Gateway:

```bash
curl -s http://localhost:5000/catalog/products
```

Expected: JSON list with seed products (e.g. `Notebook Pro`, id `11111111-1111-1111-1111-111111111101`).

## 4. Query stock

```bash
curl -s http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101
```

Expected: `availableQuantity` > 0 (Inventory seed).

## 4. Cart and checkout

Set a `customerId` (any GUID):

```bash
CUSTOMER_ID="<customerId from login>"
TOKEN="<accessToken from login>"
PRODUCT_ID=11111111-1111-1111-1111-111111111101

curl -s -X POST "http://localhost:5000/basket/baskets/$CUSTOMER_ID/items" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"productId\":\"$PRODUCT_ID\",\"productName\":\"Notebook Pro\",\"unitPrice\":5499.90,\"quantity\":1}"

curl -s -X POST "http://localhost:5000/basket/baskets/$CUSTOMER_ID/checkout" \
  -H "Authorization: Bearer $TOKEN"
```

Expected on checkout: JSON with `orderId`. Basket generates the `Idempotency-Key` header automatically for Ordering.

## 6. Track order

```bash
# replace ORDER_ID with checkout response
curl -s http://localhost:5000/ordering/orders/ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

Expected async flow (a few seconds):

1. `Pending` → after simulated payment
2. `PaymentApproved` / stock processing
3. `Completed` (or `Failed` / `Cancelled` in failure scenarios)

## 6. Logs in Seq

1. Open http://localhost:5341
2. Useful filters:
   - `Service = 'OrderingService'` and checkout `OrderId`
   - `ConsumerName` in workers (`PaymentService`, `InventoryService`, …)
   - `@Level = 'Error'` for consumer failures

Detailed guide: [observability-seq.md](./observability-seq.md)

## 7. Metrics in Prometheus and Grafana

1. Prometheus: http://localhost:9090 — targets `up` for APIs and workers
2. Grafana: http://localhost:3000 (admin/admin) — **Microservices Ecommerce** dashboard

Metrics to observe after checkout:

| Metric | What it indicates |
|--------|-------------------|
| `ecommerce_orders_created_total` | Order created |
| `ecommerce_orders_completed_total` | Flow completed |
| `ecommerce_consumer_messages_processed_total` | Consumers processing events |
| `ecommerce_outbox_messages_pending` | Outbox queue (should return to ~0) |

Guide: [observability-prometheus.md](./observability-prometheus.md)

## 8. RabbitMQ (optional)

- UI: http://localhost:15672 (guest/guest)
- Check endpoint queues after checkout; `*_error` queues should remain empty on happy path.

## Success criteria

- [ ] All Compose services report `healthy` in `docker compose ps`
- [ ] Checkout returns `orderId`
- [ ] Order progresses to `Completed` in Ordering
- [ ] Seq shows transition logs without unexpected `Error`
- [ ] Grafana/Prometheus show increment in `ecommerce_*` metrics

## If something fails

See the [runbooks](./runbooks/):

- [service-unhealthy.md](./runbooks/service-unhealthy.md)
- [outbox-messages-stuck.md](./runbooks/outbox-messages-stuck.md)
- [consumer-message-in-error-queue.md](./runbooks/consumer-message-in-error-queue.md)
- [database-unavailable.md](./runbooks/database-unavailable.md)
- [rabbitmq-unavailable.md](./runbooks/rabbitmq-unavailable.md)
