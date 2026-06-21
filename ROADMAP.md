# Roadmap

Evolution items for this portfolio project — **no delivery commitment**. Use this file to separate what exists today from what is planned or explicitly out of scope.

## Implemented

- YARP API Gateway as the single HTTP entry point
- IdentityService with register / login / JWT (HMAC, shared secret for demo)
- CatalogService with public GET and Admin-only writes
- BasketService with Redis-backed cart and HTTP checkout
- OrderingService with order lifecycle and transactional outbox
- Payment.Worker — simulated payment approval/rejection
- InventoryService — stock reservation via events
- Notification.Worker — simulated notification logging
- RabbitMQ + MassTransit with retry and `_error` queues
- Idempotent consumers (`EventId` + `ConsumerName`)
- Idempotency-Key on checkout (Basket → Ordering)
- PostgreSQL database-per-service
- Serilog → Seq, Prometheus `/metrics`, Grafana dashboards
- Health checks (`/health/live`, `/health/ready`)
- Docker Compose local stack
- Unit, integration (Testcontainers), and security test suites
- GitHub Actions CI (build + unit tests)

See [README.md](README.md) and [docs/](docs/) for details.

## Planned

### Authentication and security

- [ ] **Refresh token** — session renewal without re-login
- [ ] **Asymmetric JWT / JWKS** — key rotation without a shared secret on every service
- [ ] **Rate limiting at the Gateway** — basic protection against API abuse

### Operations and resilience

- [ ] **Alertmanager** — Prometheus alerts for stuck outbox, `_error` queues, degraded health
- [ ] **Assisted DLQ replay** — guided tool or runbook to reprocess `_error` queues with audit trail
- [ ] **Simple Admin UI** — minimal panel for products/stock (Admin role)

### Platform and delivery

- [ ] **Kubernetes manifests** or **.NET Aspire** — local/cloud orchestration
- [ ] **CD pipeline** — automated deploy after green CI

### Quality and contracts

- [ ] **Contract tests** — HTTP/messaging event schema validation
- [ ] **Consumer-driven contracts** — Pact or equivalent between producers and consumers
- [ ] **Saga orchestration** — explicit compensations for payment/inventory failure paths

## Not planned for this demo

- Production-grade multi-region deployment
- Real payment provider integration (Stripe, PayPal, etc.)
- Customer-facing web or mobile frontend
- Full e-commerce feature set (search, recommendations, promotions, etc.)
- OpenTelemetry distributed tracing (Serilog correlation exists; full tracing is roadmap-only)
- Kafka or other brokers replacing RabbitMQ for this repository
