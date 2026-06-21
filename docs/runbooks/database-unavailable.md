# Runbook: database unavailable

## Symptom

- `/health/ready` Unhealthy with `*-db` or `npgsql` check.
- `Npgsql` exceptions, connection timeout, `57P03` (cannot connect).
- APIs return 500 on persistence operations.

## How to identify

### Health

```bash
curl -s http://localhost:5003/health/ready | jq
```

Look for entries `ordering-db`, `catalog-db`, `inventory-db`, etc.

### Docker

```bash
docker compose ps postgres
docker compose logs postgres --tail 50
```

### Logs

```sql
@Message like '%database%' and @Level = 'Error'
```

## Databases per service

| Service | Database | Connection string (Docker) |
|---------|----------|----------------------------|
| Catalog | `catalog_db` | `Host=postgres;Database=catalog_db;...` |
| Ordering | `ordering_db` | `Host=postgres;Database=ordering_db;...` |
| Payment | `payment_db` | `Host=postgres;Database=payment_db;...` |
| Inventory | `inventory_db` | `Host=postgres;Database=inventory_db;...` |
| Notification | `notification_db` | `Host=postgres;Database=notification_db;...` |

## Impact

- Affected services **cannot** write state or outbox.
- Consumers fail and may go to `_error` after retries.
- Outbox accumulates pending messages when DB recovers (if broker OK).

## Recommended action

1. Start Postgres: `docker compose up -d postgres`.
2. Wait for container health `healthy`.
3. Verify databases exist (`infra/postgres/init-databases.sql` on first startup).
4. Restart APIs/workers that failed migration: `docker compose restart ordering-api payment-worker`.
5. If migration pending: check startup logs — run `dotnet ef database update` only in local dev outside compose.

## How to validate recovery

1. `pg_isready` / postgres container health OK.
2. `/health/ready` for DB services → Healthy.
3. Write operation: create order or query catalog.
4. Pending outbox decreases after recovery ([outbox runbook](./outbox-messages-stuck.md)).
