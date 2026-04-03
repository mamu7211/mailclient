#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Detect compose command
if command -v docker &>/dev/null && docker compose version &>/dev/null 2>&1; then
    COMPOSE_CMD="docker compose"
elif command -v podman-compose &>/dev/null; then
    COMPOSE_CMD="podman-compose"
else
    echo "Error: neither docker compose nor podman-compose found"
    exit 1
fi

echo "Using: $COMPOSE_CMD"

cleanup() {
    echo ""
    echo "Tearing down test stack..."
    $COMPOSE_CMD -f docker-compose.test.yml down -v --remove-orphans 2>/dev/null || true
}
trap cleanup EXIT

# Build all images
echo "Building images..."
$COMPOSE_CMD -f docker-compose.test.yml build

# Start infrastructure services (postgres, greenmail, api)
echo "Starting infrastructure..."
$COMPOSE_CMD -f docker-compose.test.yml up -d postgres greenmail api

# Wait for API to become healthy
echo "Waiting for API..."
MAX_RETRIES=60
for i in $(seq 1 $MAX_RETRIES); do
    # Use the API container to check its own health
    if $COMPOSE_CMD -f docker-compose.test.yml exec -T api curl -sf http://localhost:8080/health > /dev/null 2>&1; then
        echo "API is ready (attempt $i)"
        break
    fi
    if [ "$i" -eq "$MAX_RETRIES" ]; then
        echo "ERROR: API did not become ready"
        $COMPOSE_CMD -f docker-compose.test.yml logs api
        exit 1
    fi
    echo "  attempt $i/$MAX_RETRIES"
    sleep 3
done

# Run test suites sequentially
FAILED=0

echo ""
echo "=== Running Bruno API tests ==="
set +e
$COMPOSE_CMD -f docker-compose.test.yml run --rm bruno
BRUNO_EXIT=$?
set -e
echo "Bruno exit code: $BRUNO_EXIT"
[ "$BRUNO_EXIT" -ne 0 ] && FAILED=1

echo ""
echo "=== Running Playwright E2E tests ==="
set +e
$COMPOSE_CMD -f docker-compose.test.yml run --rm playwright
PLAYWRIGHT_EXIT=$?
set -e
echo "Playwright exit code: $PLAYWRIGHT_EXIT"
[ "$PLAYWRIGHT_EXIT" -ne 0 ] && FAILED=1

echo ""
echo "=== Results ==="
echo "Bruno:      $([ "$BRUNO_EXIT" -eq 0 ] && echo 'PASS' || echo 'FAIL')"
echo "Playwright: $([ "$PLAYWRIGHT_EXIT" -eq 0 ] && echo 'PASS' || echo 'FAIL')"

if [ "$FAILED" -eq 0 ]; then
    echo ""
    echo "All tests passed."
    exit 0
else
    echo ""
    echo "Test run failed."
    exit 1
fi
