# Metrics with Prometheus and Grafana

## Start the stack

```bash
docker compose up --build
```

Services expose metrics at `GET /metrics` (Prometheus format via OpenTelemetry).

## Prometheus

- URL: http://localhost:9090
- Config: `infra/prometheus/prometheus.yml`
- Jobs: `catalog-api`, `basket-api`, `ordering-api`, `inventory-api`, `payment-worker`, `notification-worker`

### Query examples

```promql
# Orders created per second
sum(rate(ecommerce_orders_created_total[1m]))

# Pending outbox by service
sum by (service) (ecommerce_outbox_messages_pending)

# Consumer failures
sum by (service, consumer_name) (rate(ecommerce_consumer_messages_failed_total[5m]))

# Exhausted outbox publications
sum(rate(ecommerce_outbox_messages_exhausted_total[5m]))
```

## Grafana

- URL: http://localhost:3000
- Default user/password: `admin` / `admin`
- Provisioned dashboard: **Microservices.Ecommerce** (`infra/grafana/dashboards/microservices-ecommerce.json`)

Included panels:

- Order created / completed / failed / cancelled rate
- Pending outbox and publish failures
- Consumers processed vs failures
- Stock reservations approved vs failed

## Enable/disable Prometheus export

`appsettings.json` or environment variable:

```json
"OpenTelemetry": {
  "PrometheusEnabled": true
}
```

```bash
OpenTelemetry__PrometheusEnabled=false
```

OTLP remains optional via `OpenTelemetry:OtlpEndpoint`.

## Correlate with Seq

Use `CorrelationId` and `OrderId` in logs ([observability-seq.md](./observability-seq.md)) and cross-reference with spikes in the metrics above.

## Limitations (local demo)

- No Alertmanager.
- Static scrape (no service discovery).
- Outbox gauge reflects a database query at scrape time (1–2 dispatcher cycle delay).
