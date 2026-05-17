# Métricas com Prometheus e Grafana

## Subir a stack

```bash
docker compose up --build
```

Serviços expõem métricas em `GET /metrics` (formato Prometheus via OpenTelemetry).

## Prometheus

- URL: http://localhost:9090
- Config: `infra/prometheus/prometheus.yml`
- Jobs: `catalog-api`, `basket-api`, `ordering-api`, `inventory-api`, `payment-worker`, `notification-worker`

### Exemplos de consulta

```promql
# Pedidos criados por segundo
sum(rate(ecommerce_orders_created_total[1m]))

# Outbox pendente por serviço
sum by (service) (ecommerce_outbox_messages_pending)

# Falhas de consumer
sum by (service, consumer_name) (rate(ecommerce_consumer_messages_failed_total[5m]))

# Publicações de outbox esgotadas
sum(rate(ecommerce_outbox_messages_exhausted_total[5m]))
```

## Grafana

- URL: http://localhost:3000
- Usuário/senha padrão: `admin` / `admin`
- Dashboard provisionado: **Microservices.Ecommerce** (`infra/grafana/dashboards/microservices-ecommerce.json`)

Painéis incluídos:

- Taxa de pedidos criados / completados / falhos / cancelados
- Outbox pendente e falhas de publicação
- Consumers processados vs falhas
- Reservas de estoque aprovadas vs falhadas

## Habilitar/desabilitar export Prometheus

`appsettings.json` ou variável de ambiente:

```json
"OpenTelemetry": {
  "PrometheusEnabled": true
}
```

```bash
OpenTelemetry__PrometheusEnabled=false
```

OTLP continua opcional via `OpenTelemetry:OtlpEndpoint`.

## Correlacionar com Seq

Use `CorrelationId` e `OrderId` nos logs ([observability-seq.md](./observability-seq.md)) e cruze com picos nas métricas acima.

## Limitações (demo local)

- Sem Alertmanager.
- Scrape estático (sem service discovery).
- Gauge de outbox reflete consulta ao banco no momento do scrape (atraso de 1–2 ciclos do dispatcher).
