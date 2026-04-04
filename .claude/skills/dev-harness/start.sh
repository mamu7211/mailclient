#!/bin/bash
# Start Aspire AppHost with seed data, capture logs
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/../../.."

if curl -sk https://localhost:7272/health 2>/dev/null; then
    echo "API already running"
    exit 0
fi

FEIRB_SEED_DATA=true dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
echo $! > /tmp/feirb-aspire.pid
echo "Started Aspire (PID $(cat /tmp/feirb-aspire.pid))"

for i in $(seq 1 40); do
    if curl -sk https://localhost:7272/health 2>/dev/null; then
        echo "API ready (attempt $i)"
        exit 0
    fi
    sleep 3
done

echo "TIMEOUT: API did not become ready"
exit 1
