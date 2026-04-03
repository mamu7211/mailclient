#!/bin/sh
# Wait for the API to become healthy before running tests.
# Uses the /health endpoint exposed in Development mode.

API_URL="${API_URL:-http://api:8080}"
MAX_RETRIES=60
RETRY_INTERVAL=3

echo "Waiting for API at $API_URL/health ..."

for i in $(seq 1 $MAX_RETRIES); do
    if curl -sf "$API_URL/health" > /dev/null 2>&1; then
        echo "API is ready (attempt $i/$MAX_RETRIES)"
        exit 0
    fi
    echo "  attempt $i/$MAX_RETRIES - not ready yet"
    sleep $RETRY_INTERVAL
done

echo "ERROR: API did not become ready within $((MAX_RETRIES * RETRY_INTERVAL)) seconds"
exit 1
