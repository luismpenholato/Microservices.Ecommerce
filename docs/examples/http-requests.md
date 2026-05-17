# Exemplos HTTP (curl)

Base URL do **ApiGateway**: `http://localhost:5000`

Validação automatizada (inclui fluxo abaixo): `.\scripts\validate-local.ps1 -RunCheckoutFlow` ou `./scripts/validate-local.sh --run-checkout-flow`

URLs diretas dos serviços (debug): Catalog `5001`, Basket `5002`, Ordering `5003`, Inventory `5004`, Payment.Worker `5010`, Notification.Worker `5011`.

IDs de seed do catálogo/estoque:

| Produto | ProductId |
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

Salve `accessToken` e `customerId` da resposta.

### Register

```bash
curl -s -X POST http://localhost:5000/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"meu@email.com","password":"MinhaSenha123!"}'
```

### Me (autenticado)

```bash
TOKEN="cole_o_access_token_aqui"
curl -s http://localhost:5000/identity/auth/me \
  -H "Authorization: Bearer $TOKEN"
```

---

## Health

### Live (processo)

```bash
curl -s http://localhost:5000/health/live
curl -s http://localhost:5003/health/live
curl -s http://localhost:5010/health/live
```

### Ready (dependências)

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

### Listar produtos

```bash
curl -s http://localhost:5000/catalog/products
```

### Direto no serviço

```bash
curl -s http://localhost:5001/api/products
```

---

## Basket (via Gateway)

Requer `Authorization: Bearer`. Use o `customerId` retornado no login (claim `customer_id`).

```bash
TOKEN="cole_o_access_token_aqui"
CUSTOMER_ID="cole_o_customer_id_do_login"
```

### Obter carrinho

```bash
curl -s http://localhost:5000/basket/baskets/$CUSTOMER_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Adicionar item

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

### Remover item

```bash
curl -s -X DELETE \
  http://localhost:5000/basket/baskets/22222222-2222-2222-2222-222222222201/items/11111111-1111-1111-1111-111111111101
```

### Checkout

O Basket calcula e envia `Idempotency-Key` ao Ordering.

```bash
curl -s -X POST http://localhost:5000/basket/baskets/$CUSTOMER_ID/checkout \
  -H "Authorization: Bearer $TOKEN"
```

---

## Checkout com Idempotency-Key (Ordering direto)

Chamada direta ao Ordering (útil para testes de idempotência sem passar pelo Basket):

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

Repetir a mesma requisição com o mesmo `Idempotency-Key` deve retornar o mesmo pedido (`id` idêntico):

```bash
KEY="validate-local-demo-001"
BODY='{"customerId":"22222222-2222-2222-2222-222222222299","items":[{"productId":"11111111-1111-1111-1111-111111111101","productName":"Notebook Pro","quantity":1,"unitPrice":5499.90}]}'

curl -s -X POST http://localhost:5000/ordering/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: $KEY" -d "$BODY"

curl -s -X POST http://localhost:5000/ordering/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: $KEY" -d "$BODY"
```

O script `validate-local` executa esse par de chamadas e compara o campo `id` das duas respostas.

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

### Pedido por id

```bash
curl -s http://localhost:5000/ordering/orders/{orderId}
```

### Pedidos por cliente

```bash
curl -s http://localhost:5000/ordering/orders/customer/22222222-2222-2222-2222-222222222201
```

---

## Inventory

### Consultar estoque (Gateway)

```bash
curl -s http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101
```

### Atualizar quantidade (Gateway)

```bash
curl -s -X PUT http://localhost:5000/inventory/inventory/11111111-1111-1111-1111-111111111101 \
  -H "Content-Type: application/json" \
  -d '{ "availableQuantity": 50 }'
```

### Direto no serviço

```bash
curl -s http://localhost:5004/api/inventory/11111111-1111-1111-1111-111111111101
```

---

## Correlation id (opcional)

```bash
curl -s http://localhost:5000/catalog/products \
  -H "X-Correlation-Id: my-trace-123"
```

Propagação nos logs Seq: filtrar por `CorrelationId = 'my-trace-123'`.
