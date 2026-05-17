# Observabilidade

## Logs

- **Serilog** com enrichers de serviço, correlation id e thread
- **Seq** em `http://localhost:5341` (Docker Compose)

## Health checks

- `GET /health/live` — processo vivo
- `GET /health/ready` — dependências (banco, Redis, RabbitMQ)

## Tracing

OpenTelemetry configurado em `BuildingBlocks.Observability` com export OTLP opcional (`OpenTelemetry:OtlpEndpoint`).
