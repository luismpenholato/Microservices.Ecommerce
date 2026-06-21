# Runbook: unhealthy service

## Symptom

- `docker compose ps` shows service `unhealthy`.
- API returns 503 on `/health/ready`.
- Gateway or client receives timeout.

## How to identify

### Health endpoints

| Endpoint | Meaning |
|----------|---------|
| `GET /health/live` | Process responds (does not validate DB/broker) |
| `GET /health/ready` | Required dependencies OK |

```bash
curl -s http://localhost:5003/health/live
curl -s http://localhost:5003/health/ready
```

Workers (Payment, Notification):

```bash
curl -s http://localhost:8080/health/live   # port per compose
curl -s http://localhost:8080/health/ready
```

### Docker Compose

```bash
docker compose ps
docker compose logs ordering-api --tail 100
```

### Per service (ready)

| Service | Ready validates |
|---------|-----------------|
| Catalog | PostgreSQL |
| Basket | Redis |
| Ordering | PostgreSQL + RabbitMQ |
| Inventory | PostgreSQL + RabbitMQ |
| Payment.Worker | PostgreSQL + RabbitMQ |
| Notification.Worker | PostgreSQL + RabbitMQ |
| ApiGateway | self/config (does not block on downstream) |

## Impact

- Orchestrator/compose may not route traffic.
- Checkout or event consumption unavailable for the affected service.

## Recommended action

1. If **live** fails: process stuck — `docker compose restart <service>`.
2. If **live** OK and **ready** fails: identify dependency in health JSON (Unhealthy description).
3. Follow specific runbook: [database](./database-unavailable.md) or [rabbitmq](./rabbitmq-unavailable.md).
4. ApiGateway unhealthy: check YARP configuration, not downstream microservices.

## How to validate recovery

1. `GET /health/live` → 200 Healthy.
2. `GET /health/ready` → 200 Healthy.
3. `docker compose ps` → `healthy` (if healthcheck configured).
4. Smoke business flow: list products, checkout, or metrics rising again.
