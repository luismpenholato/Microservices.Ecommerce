# HTTP examples (curl)

**ApiGateway** base URL: `http://localhost:5000`

End-to-end manual flow: [smoke-tests.md](../smoke-tests.md).

Direct service URLs (debug): Catalog `5001`, Basket `5002`, Ordering `5003`, Inventory `5004`, Payment.Worker `5010`, Notification.Worker `5011`.

Catalog/inventory seed IDs:

| Product | ProductId |
|---------|-----------|
| Notebook Pro | `11111111-1111-1111-1111-111111111101` |
| Teclado Mecânico | `11111111-1111-1111-1111-111111111102` |

---

## Identity (JWT)

### Login (demo seed)

```bash
curl -s -X POST http://localhost:5000/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@ecommerce.local","password":"Demo123!"}'
```

Save `accessToken` and `customerId` from the response.

### Register

```bash
curl -s -X POST http://localhost:5000/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"me@email.com","password":"MyPassword123!"}'
```

### Me (authenticated)

```bash
TOKEN="paste_access_token_here"
curl -s http://localhost:5000/identity/auth/me \
  -H "Authorization: Bearer $TOKEN"
```

---

## Health

### Live (process)

```bash
curl -s http://localhost:5000/health/live
curl -s http://localhost:5003/health/live
curl -s http://localhost:5010/health/live
```

### Ready (dependencies)

```bash
curl -s http://localhost:5000/health/ready
curl -s http://localhost:5001/health/ready
curl -s http://localhost:5002/health/ready
curl -s http://localhost:5003/health/ready
curl -s http://localhost:5004/health/ready
curl -s http://localhost:5010/health/ready
curl -s http://localhost:5011/health/ready
```

---

## Metrics (Prometheus)

```bash
curl -s http://localhost:5000/metrics | head
curl -s http://localhost:5003/metrics | grep ecommerce_
curl -s http://localhost:5010/metrics | grep ecommerce_consumer
```

---

## Catalog (via Gateway)

### List products

```bash
curl -s http://localhost:5000/catalog/products
```

### Direct to service

```bash
curl -s http://localhost:5001/api/products
```

---

## Basket (via Gateway)

Requires `Authorization: Bearer`. Use the `customerId` returned on login (`customer_id` claim).

```bash
TOKEN="paste_access_token_here"
CUSTOMER_ID="paste_customer_id_from_login"
```

### Get cart

```bash
curl -s http://localhost:5000/basket/baskets/$CUSTOMER_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Add item

```bash
curl -s -X POST http://localhost:5000/basket/baskets/$CUSTOMER_ID/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "11111111-1111-1111-1111-111111111101",
    "productName": "Notebook Pro",
    "unitPrice": 5499.90,
    "quantity": 1
  }'
```

### Remove item

```bash
curl -s -X DELETE \
  http://localhost:5000/basket/baskets/22222222-2222-2222-2222-222222222201/items/11111111-1111-1111-1111-111111111101
```

### Checkout

Basket computes and sends `Idempotency-Key` to Ordering.

```bash
curl -s -X POST http://localhost:5000/basket/baskets/$CUSTOMER_ID/checkout \
  -H "Authorization: Bearer $TOKEN"
```

---

## Checkout with Idempotency-Key (direct to Ordering)

Direct call to Ordering (useful for idempotency tests without going through Basket):

```bash
curl -s -X POST http://localhost:5003/api/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-key-001" \
  -H "X-Correlation-Id: demo-correlation-001" \
  -d '{
    "customerId": "22222222-2222-2222-2222-222222222201",
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111101",
        "productName": "Notebook Pro",
        "quantity": 1,
        "unitPrice": 5499.90
      }
    ]
  }'
```

Repeating the same request with the same `Idempotency-Key` should return the same order (identical `id`):

```bash
KEY="idempotency-demo-001"
BODY='{"customerId":"22222222-2222-2222-2222-222222222299","items":[{"productId":"11111111-1111-1111-1111-111111111101","productName":"Notebook Pro","quantity":1,"unitPrice":5499.90}]}'

curl -s -X POST http://localhost:5000/ordering/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: $KEY" -d "$BODY"

curl -s -X POST http://localhost:5000/ordering/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: $KEY" -d "$BODY"
```

Both responses should return the same order `id`.

Via Gateway:

```bash
curl -s -X POST http://localhost:5000/ordering/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-key-002" \
  -d '{
    "customerId": "22222222-2222-2222-2222-222222222201",
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111101",
        "productName": "Notebook Pro",
        "quantity": 1,
        "unitPrice": 5499.90
      }
    ]
  }'
```

---

## Ordering

### Order by id

```bash
curl -s http://localhost:5000/ordering/orders/{orderId}
```

### Orders by customer

```bash
curl -s http://localhost:5000/ordering/orders/customer/22222222-2222-2222-2222-222222222201
```

---

## Inventory

### Query stock (Gateway)

```bash
curl -s http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101
```

### Update quantity (Gateway)

```bash
curl -s -X PUT http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101 \
  -H "Content-Type: application/json" \
  -d '{ "availableQuantity": 50 }'
```

### Direct to service

```bash
curl -s http://localhost:5004/api/inventory/11111111-1111-1111-1111-111111111101
```

---

## Correlation id (optional)

```bash
curl -s http://localhost:5000/catalog/products \
  -H "X-Correlation-Id: my-trace-123"
```

Seq log propagation: filter by `CorrelationId = 'my-trace-123'`.
