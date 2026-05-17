#!/usr/bin/env bash
# Valida ambiente local Docker Compose (Linux/macOS/WSL).
# Uso: ./scripts/validate-local.sh [--skip-compose] [--run-checkout-flow]
set -euo pipefail

SKIP_COMPOSE=0
RUN_CHECKOUT_FLOW=0
HEALTH_TIMEOUT_MINUTES="${HEALTH_TIMEOUT_MINUTES:-12}"
CHECKOUT_POLL_TIMEOUT_SECONDS="${CHECKOUT_POLL_TIMEOUT_SECONDS:-120}"

for arg in "$@"; do
  case "$arg" in
    --skip-compose) SKIP_COMPOSE=1 ;;
    --run-checkout-flow) RUN_CHECKOUT_FLOW=1 ;;
    -h|--help)
      echo "Uso: $0 [--skip-compose] [--run-checkout-flow]"
      exit 0
      ;;
    *) echo "Argumento desconhecido: $arg" >&2; exit 2 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/docker-compose.yml"

declare -a RESULT_NAMES=()
declare -a RESULT_OK=()
declare -a RESULT_DETAIL=()

add_result() {
  local name="$1" ok="$2" detail="$3"
  RESULT_NAMES+=("$name")
  RESULT_OK+=("$ok")
  RESULT_DETAIL+=("$detail")
  if [[ "$ok" == "1" ]]; then
    printf '  [OK] %s — %s\n' "$name" "$detail"
  else
    printf '  [FAIL] %s — %s\n' "$name" "$detail" >&2
  fi
}

http_check() {
  local name="$1" url="$2" pattern="${3:-}"
  local code body
  code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 45 "$url" || echo "000")
  body="$(cat /tmp/validate-local-body.txt 2>/dev/null || true)"
  if [[ "$code" =~ ^2 ]]; then
    if [[ -n "$pattern" ]] && ! grep -qE "$pattern" <<<"$body"; then
      add_result "$name" 0 "HTTP $code sem conteúdo esperado — $url"
      return
    fi
    add_result "$name" 1 "HTTP $code — $url"
  else
    add_result "$name" 0 "HTTP $code — $url"
  fi
}

json_get_first_guid() {
  grep -oEi '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' <<<"$1" | head -n1
}

json_get_field() {
  local json="$1" field="$2"
  local pattern="\"${field}\"[[:space:]]*:[[:space:]]*\"?([^\",}]+)\"?"
  if [[ "$json" =~ $pattern ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
}

new_guid() {
  if command -v uuidgen >/dev/null 2>&1; then
    uuidgen | tr '[:upper:]' '[:lower:]'
  else
    printf '%04x%04x-%04x-%04x-%04x-%04x%04x%04x\n' \
      $RANDOM $RANDOM $RANDOM $((RANDOM & 0x0fff | 0x4000)) \
      $((RANDOM & 0x3fff | 0x8000)) $RANDOM $RANDOM $RANDOM
  fi
}

login_demo_auth() {
  local gateway="$1"
  curl -sS -o /tmp/validate-local-login.txt -w '%{http_code}' --max-time 45 \
    -X POST -H "Content-Type: application/json" \
    -d '{"email":"demo@ecommerce.local","password":"Demo123!"}' \
    "$gateway/identity/auth/login"
}

run_checkout_flow() {
  local gateway="http://localhost:5000"
  local attempt max_attempts=3
  local product_id product_name unit_price customer_id order_id final_status
  local completed=0
  local auth_token=""

  echo ""
  echo "==> Fluxo E2E de checkout (Gateway)"

  local login_code
  login_code="$(login_demo_auth "$gateway" | tr -d '\n')"
  local login_body
  login_body="$(cat /tmp/validate-local-login.txt 2>/dev/null || true)"
  if [[ ! "$login_code" =~ ^2 ]]; then
    add_result "E2E identity login" 0 "HTTP $login_code"
    return 1
  fi
  auth_token="$(json_get_field "$login_body" "accessToken")"
  customer_id="$(json_get_field "$login_body" "customerId")"
  if [[ -z "$auth_token" || -z "$customer_id" ]]; then
    add_result "E2E identity login" 0 "token/customerId ausentes"
    return 1
  fi
  add_result "E2E identity login" 1 "customerId=$customer_id"

  for (( attempt=1; attempt<=max_attempts; attempt++ )); do
    if [[ "$attempt" -gt 1 ]]; then
      echo "  Nova tentativa ($attempt/$max_attempts) após estado final inesperado."
    fi

    local catalog_body code
    code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 45 "$gateway/catalog/products" || echo "000")
    catalog_body="$(cat /tmp/validate-local-body.txt 2>/dev/null || true)"
    if [[ ! "$code" =~ ^2 ]]; then
      add_result "E2E catalog products" 0 "HTTP $code"
      return 1
    fi

    product_id="$(json_get_first_guid "$catalog_body")"
    product_name="$(json_get_field "$catalog_body" "name")"
    unit_price="$(json_get_field "$catalog_body" "price")"
    if [[ -z "$product_id" ]]; then
      product_id="11111111-1111-1111-1111-111111111101"
      product_name="Notebook Pro"
      unit_price="5499.90"
    fi
    add_result "E2E catalog products" 1 "produto $product_id"

    code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 45 \
      "$gateway/inventory/inventory/$product_id" || echo "000")
    local inv_body
    inv_body="$(cat /tmp/validate-local-body.txt 2>/dev/null || true)"
    if [[ ! "$code" =~ ^2 ]] || ! grep -qE 'availableQuantity|AvailableQuantity' <<<"$inv_body"; then
      add_result "E2E inventory" 0 "HTTP $code ou estoque inválido"
      return 1
    fi
    add_result "E2E inventory" 1 "produto $product_id"

    local add_payload
    add_payload=$(printf '{"productId":"%s","productName":"%s","unitPrice":%s,"quantity":1}' \
      "$product_id" "${product_name:-Product}" "${unit_price:-1}")

    code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 45 \
      -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $auth_token" \
      -d "$add_payload" "$gateway/basket/baskets/$customer_id/items" || echo "000")
    if [[ ! "$code" =~ ^2 ]]; then
      add_result "E2E basket add item" 0 "HTTP $code"
      return 1
    fi
    add_result "E2E basket add item" 1 "customerId=$customer_id"

    code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 60 \
      -X POST -H "Authorization: Bearer $auth_token" \
      "$gateway/basket/baskets/$customer_id/checkout" || echo "000")
    local checkout_body
    checkout_body="$(cat /tmp/validate-local-body.txt 2>/dev/null || true)"
    if [[ ! "$code" =~ ^2 ]]; then
      add_result "E2E basket checkout" 0 "HTTP $code"
      return 1
    fi
    order_id="$(json_get_field "$checkout_body" "orderId")"
    if [[ -z "$order_id" ]]; then
      order_id="$(json_get_field "$checkout_body" "id")"
    fi
    if [[ -z "$order_id" ]]; then
      add_result "E2E basket checkout" 0 "OrderId ausente"
      return 1
    fi
    add_result "E2E basket checkout" 1 "orderId=$order_id"

    final_status=""
    local deadline=$((SECONDS + CHECKOUT_POLL_TIMEOUT_SECONDS))
    while (( SECONDS < deadline )); do
      code=$(curl -sS -o /tmp/validate-local-body.txt -w '%{http_code}' --max-time 45 \
        -H "Authorization: Bearer $auth_token" \
        "$gateway/ordering/orders/$order_id" || echo "000")
      local order_body
      order_body="$(cat /tmp/validate-local-body.txt 2>/dev/null || true)"
      if [[ "$code" =~ ^2 ]]; then
        final_status="$(json_get_field "$order_body" "status")"
        case "$final_status" in
          Completed|Failed|Cancelled|PaymentRejected) break ;;
        esac
      fi
      sleep 3
    done

    if [[ -z "$final_status" ]] || [[ ! "$final_status" =~ ^(Completed|Failed|Cancelled|PaymentRejected)$ ]]; then
      add_result "E2E order polling" 0 "timeout ${CHECKOUT_POLL_TIMEOUT_SECONDS}s — último status: ${final_status:-unknown}"
      return 1
    fi

    echo "  Status final do pedido $order_id : $final_status"

    if [[ "$final_status" == "Completed" ]]; then
      add_result "E2E order polling" 1 "status=$final_status"
      completed=1
      break
    fi
    add_result "E2E order polling (tentativa $attempt)" 0 "status=$final_status (esperado Completed)"
  done

  if [[ "$completed" -ne 1 ]]; then
    add_result "E2E order completed" 0 "não atingiu Completed após $max_attempts tentativas"
    return 1
  fi

  echo ""
  echo "==> Validando idempotência HTTP (Ordering)"
  local idempotency_key register_email register_body idempotency_token order_payload
  register_email="validate-local-$(new_guid)@ecommerce.local"
  register_body=$(printf '{"email":"%s","password":"ValidateLocal123!"}' "$register_email")
  curl -sS -o /tmp/validate-local-register.txt -w '%{http_code}' --max-time 45 \
    -X POST -H "Content-Type: application/json" \
    -d "$register_body" "$gateway/identity/auth/register" > /tmp/validate-local-register-code.txt || true
  idempotency_token="$(json_get_field "$(cat /tmp/validate-local-register.txt 2>/dev/null || true)" "accessToken")"
  idempotency_key="validate-local-$(new_guid)"
  order_payload=$(printf '{"items":[{"productId":"%s","productName":"%s","quantity":1,"unitPrice":%s}]}' \
    "$product_id" "${product_name:-Product}" "${unit_price:-1}")

  curl -sS -o /tmp/validate-local-body-1.txt -w '%{http_code}' --max-time 45 \
    -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $idempotency_token" -H "Idempotency-Key: $idempotency_key" \
    -d "$order_payload" "$gateway/ordering/orders" > /tmp/validate-local-code-1.txt || true
  local code1 code2 body1 body2 first_id second_id
  code1="$(tr -d '\n' < /tmp/validate-local-code-1.txt)"
  body1="$(cat /tmp/validate-local-body-1.txt 2>/dev/null || true)"

  curl -sS -o /tmp/validate-local-body-2.txt -w '%{http_code}' --max-time 45 \
    -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $idempotency_token" -H "Idempotency-Key: $idempotency_key" \
    -d "$order_payload" "$gateway/ordering/orders" > /tmp/validate-local-code-2.txt || true
  code2="$(tr -d '\n' < /tmp/validate-local-code-2.txt)"
  body2="$(cat /tmp/validate-local-body-2.txt 2>/dev/null || true)"

  if [[ ! "$code1" =~ ^2 ]] || [[ ! "$code2" =~ ^2 ]]; then
    add_result "E2E idempotency HTTP" 0 "HTTP $code1 / $code2"
    return 1
  fi

  first_id="$(json_get_field "$body1" "id")"
  second_id="$(json_get_field "$body2" "id")"
  if [[ -n "$first_id" && "$first_id" == "$second_id" ]]; then
    add_result "E2E idempotency HTTP" 1 "orderId=$first_id"
    return 0
  fi

  add_result "E2E idempotency HTTP" 0 "OrderIds diferentes: $first_id vs $second_id"
  return 1
}

echo "Microservices.Ecommerce — validação local"
echo "Repositório: $REPO_ROOT"

if ! command -v docker >/dev/null 2>&1; then
  add_result "Docker CLI" 0 "docker não encontrado no PATH"
  exit 1
fi
if ! docker compose version >/dev/null 2>&1; then
  add_result "Docker CLI" 0 "docker compose não disponível"
  exit 1
fi
add_result "Docker CLI" 1 "docker e docker compose disponíveis"

if [[ "$SKIP_COMPOSE" -eq 0 ]]; then
  echo ""
  echo "==> Executando docker compose up -d --build"
  (cd "$REPO_ROOT" && docker compose -f "$COMPOSE_FILE" up -d --build)
  add_result "docker compose up" 1 "containers iniciados"
else
  echo ""
  echo "==> --skip-compose: pulando docker compose up"
fi

services=(postgres redis rabbitmq identity-api catalog-api basket-api ordering-api inventory-api payment-worker notification-worker api-gateway)
deadline=$((SECONDS + HEALTH_TIMEOUT_MINUTES * 60))
echo ""
echo "==> Aguardando healthchecks (timeout ${HEALTH_TIMEOUT_MINUTES} min)"

health_ok=0
while (( SECONDS < deadline )); do
  pending=()
  for svc in "${services[@]}"; do
    health="$(docker compose -f "$COMPOSE_FILE" ps "$svc" --format '{{.Health}}' 2>/dev/null | head -n1)"
    if [[ -z "$health" ]]; then
      state="$(docker compose -f "$COMPOSE_FILE" ps "$svc" --format '{{.State}}' 2>/dev/null | head -n1)"
      pending+=("$svc (${state:-ausente})")
    elif [[ "$health" != "healthy" ]]; then
      pending+=("$svc ($health)")
    fi
  done
  if [[ ${#pending[@]} -eq 0 ]]; then
    add_result "Compose healthchecks" 1 "todos healthy"
    health_ok=1
    break
  fi
  echo "  Aguardando: ${pending[*]}"
  sleep 8
done

if [[ "$health_ok" -ne 1 ]]; then
  add_result "Compose healthchecks" 0 "timeout após ${HEALTH_TIMEOUT_MINUTES} min"
fi

echo ""
echo "==> Validando /health/ready"
for port in 5000 5001 5002 5003 5004 5005 5010 5011; do
  http_check "health/ready :$port" "http://localhost:${port}/health/ready"
done

echo ""
echo "==> Validando /metrics"
for port in 5000 5001 5002 5003 5004 5010 5011; do
  http_check "metrics :$port" "http://localhost:${port}/metrics" '(ecommerce_|# HELP|# TYPE)'
done

SAMPLE_PRODUCT_ID="11111111-1111-1111-1111-111111111101"
echo ""
echo "==> Validando ApiGateway"
http_check "Gateway GET /catalog/products" "http://localhost:5000/catalog/products" '(Notebook|'"$SAMPLE_PRODUCT_ID"')'
http_check "Gateway GET /inventory" "http://localhost:5000/inventory/inventory/$SAMPLE_PRODUCT_ID" '(availableQuantity|AvailableQuantity|'"$SAMPLE_PRODUCT_ID"')'

checkout_flow_ok=1
if [[ "$RUN_CHECKOUT_FLOW" -eq 1 ]]; then
  if ! run_checkout_flow; then
    checkout_flow_ok=0
  fi
else
  echo ""
  echo "(Dica: use --run-checkout-flow para validar checkout ponta a ponta)"
fi

echo ""
echo "==> Resumo"
failures=0
for i in "${!RESULT_NAMES[@]}"; do
  if [[ "${RESULT_OK[$i]}" != "1" ]]; then
    failures=$((failures + 1))
  fi
done
total="${#RESULT_NAMES[@]}"
ok_count=$((total - failures))
echo "Total: $total | OK: $ok_count | Falhas: $failures"

if [[ "$failures" -gt 0 ]] || [[ "$health_ok" -ne 1 ]] || { [[ "$RUN_CHECKOUT_FLOW" -eq 1 ]] && [[ "$checkout_flow_ok" -ne 1 ]]; }; then
  echo ""
  echo "Validação concluída com FALHAS."
  exit 1
fi

echo ""
echo "Validação concluída com SUCESSO."
if [[ "$RUN_CHECKOUT_FLOW" -eq 1 ]]; then
  echo "Checkout E2E e idempotência HTTP validados."
else
  echo "Próximo passo: --run-checkout-flow ou docs/smoke-tests.md"
fi
exit 0
