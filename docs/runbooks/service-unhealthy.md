# Runbook: serviço unhealthy

## Sintoma

- `docker compose ps` mostra serviço `unhealthy`.
- API retorna 503 em `/health/ready`.
- Gateway ou cliente recebe timeout.

## Como identificar

### Health endpoints

| Endpoint | Significado |
|----------|-------------|
| `GET /health/live` | Processo responde (não valida DB/broker) |
| `GET /health/ready` | Dependências obrigatórias OK |

```bash
curl -s http://localhost:5003/health/live
curl -s http://localhost:5003/health/ready
```

Workers (Payment, Notification):

```bash
curl -s http://localhost:8080/health/live   # porta conforme compose
curl -s http://localhost:8080/health/ready
```

### Docker Compose

```bash
docker compose ps
docker compose logs ordering-api --tail 100
```

### Por serviço (ready)

| Serviço | Ready valida |
|---------|----------------|
| Catalog | PostgreSQL |
| Basket | Redis |
| Ordering | PostgreSQL + RabbitMQ |
| Inventory | PostgreSQL + RabbitMQ |
| Payment.Worker | PostgreSQL + RabbitMQ |
| Notification.Worker | PostgreSQL + RabbitMQ |
| ApiGateway | self/config (não bloqueia por downstream) |

## Impacto

- Orquestrador/compose pode não rotear tráfego.
- Checkout ou consumo de eventos indisponível para o serviço afetado.

## Ação recomendada

1. Se **live** falha: processo travado — `docker compose restart <serviço>`.
2. Se **live** OK e **ready** falha: identifique dependência no JSON do health (Unhealthy description).
3. Siga runbook específico: [database](./database-unavailable.md) ou [rabbitmq](./rabbitmq-unavailable.md).
4. ApiGateway unhealthy: verifique configuração YARP, não os microserviços downstream.

## Como validar recuperação

1. `GET /health/live` → 200 Healthy.
2. `GET /health/ready` → 200 Healthy.
3. `docker compose ps` → `healthy` (se healthcheck configurado).
4. Fluxo de negócio de fumaça: listar produtos, checkout, ou métricas voltando a subir.
