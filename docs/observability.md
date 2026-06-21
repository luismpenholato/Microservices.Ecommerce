# Observability

## Logs

- **Serilog** with service, correlation id, and thread enrichers
- **Seq** at `http://localhost:5341` (Docker Compose)

## Health checks

- `GET /health/live` — process alive
- `GET /health/ready` — dependencies (database, Redis, RabbitMQ)

## Tracing

OpenTelemetry configured in `BuildingBlocks.Observability` with optional OTLP export (`OpenTelemetry:OtlpEndpoint`).
