# Runbook: banco de dados indisponível

## Sintoma

- `/health/ready` Unhealthy com check `*-db` ou `npgsql`.
- Exceções `Npgsql`, timeout de conexão, `57P03` (cannot connect).
- APIs retornam 500 em operações com persistência.

## Como identificar

### Health

```bash
curl -s http://localhost:5003/health/ready | jq
```

Procure entradas `ordering-db`, `catalog-db`, `inventory-db`, etc.

### Docker

```bash
docker compose ps postgres
docker compose logs postgres --tail 50
```

### Logs

```sql
@Message like '%database%' and @Level = 'Error'
```

## Bancos por serviço

| Serviço | Database | Connection string (Docker) |
|---------|----------|----------------------------|
| Catalog | `catalog_db` | `Host=postgres;Database=catalog_db;...` |
| Ordering | `ordering_db` | `Host=postgres;Database=ordering_db;...` |
| Payment | `payment_db` | `Host=postgres;Database=payment_db;...` |
| Inventory | `inventory_db` | `Host=postgres;Database=inventory_db;...` |
| Notification | `notification_db` | `Host=postgres;Database=notification_db;...` |

## Impacto

- Serviços afetados **não** gravam estado nem outbox.
- Consumers falham e podem ir para `_error` após retries.
- Outbox acumula pendências quando o DB volta (se broker OK).

## Ação recomendada

1. Suba o Postgres: `docker compose up -d postgres`.
2. Aguarde health `healthy` do container.
3. Verifique se os databases existem (`infra/postgres/init-databases.sql` na primeira subida).
4. Reinicie APIs/workers que falharam na migração: `docker compose restart ordering-api payment-worker`.
5. Se migração pendente: logs na inicialização — executar `dotnet ef database update` apenas em dev local fora do compose.

## Como validar recuperação

1. `pg_isready` / health do container postgres OK.
2. `/health/ready` dos serviços com DB → Healthy.
3. Operação de escrita: criar pedido ou consultar catálogo.
4. Outbox pendente diminui após recuperação ([outbox runbook](./outbox-messages-stuck.md)).
