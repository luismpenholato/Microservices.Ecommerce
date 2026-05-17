# Roadmap

Itens planejados para evolução do portfólio — **sem compromisso de prazo**. Não fazem parte do escopo atual.

## Autenticação e segurança

- [ ] **Refresh token** — renovação de sessão sem novo login
- [ ] **JWT assimétrico / JWKS** — rotação de chaves e validação sem segredo compartilhado em todos os serviços
- [ ] **Rate limiting no Gateway** — proteção básica contra abuso de APIs públicas

## Operação e resiliência

- [ ] **Alertmanager** — alertas Prometheus para outbox pendente, filas `_error`, health degradado
- [ ] **Replay manual assistido de DLQ** — ferramenta ou runbook guiado para reprocessar filas `_error` com auditoria
- [ ] **Admin UI simples** — painel mínimo para produtos/estoque (role Admin)

## Plataforma e entrega

- [ ] **Kubernetes manifests** ou **.NET Aspire** — orquestração local/cloud
- [ ] **Pipeline CD** — deploy automatizado após CI verde

## Qualidade e contratos

- [ ] **Contract tests** — validação de schemas de eventos HTTP/mensageria
- [ ] **Consumer-driven contracts** — Pact ou equivalente entre Basket/Ordering e consumidores
- [ ] **Saga orchestration** — compensações explícitas em fluxos de falha de pagamento/estoque

## Referências atuais

O que já está implementado está documentado em [README.md](README.md) e [docs/](docs/).
