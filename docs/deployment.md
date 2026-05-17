# Deploy local

## Pré-requisitos

- Docker Desktop
- .NET 10 SDK (desenvolvimento local sem Docker)

## Docker Compose

```bash
docker compose up --build
```

Portas:

| Serviço | Porta |
|---------|-------|
| ApiGateway | 5000 |
| Catalog | 5001 |
| Basket | 5002 |
| Ordering | 5003 |
| RabbitMQ Management | 15672 |
| Seq | 5341 |
| PostgreSQL | 5432 |
| Redis | 6379 |

## Exemplos via Gateway

- `GET http://localhost:5000/catalog/products`
- `GET http://localhost:5000/basket/baskets/{customerId}`
- `POST http://localhost:5000/ordering/orders`
