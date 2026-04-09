#!/bin/bash
# Start Aspire AppHost, capture logs
# Usage: start.sh [--seeding]
#   --seeding   Seed database with test data (users, mailboxes, labels, jobs)
#   (no flags)  Start bare — fresh environment, no seed data
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/../../.."

if curl -sk https://localhost:7272/health 2>/dev/null; then
    echo "API already running"
    exit 0
fi

ENV_VARS=""
for arg in "$@"; do
    case "$arg" in
        --seeding) ENV_VARS="FEIRB_SEED_DATA=true" ;;
        *) echo "Unknown argument: $arg"; exit 1 ;;
    esac
done

if [ -n "$ENV_VARS" ]; then
    echo "Starting Aspire with database seeding..."
    env $ENV_VARS dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
else
    echo "Starting Aspire without seeding..."
    dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
fi

echo $! > /tmp/feirb-aspire.pid
echo "Started Aspire (PID $(cat /tmp/feirb-aspire.pid))"

for i in $(seq 1 40); do
    if curl -sk https://localhost:7272/health 2>/dev/null; then
        echo "Healthy"
        echo "API ready (attempt $i)"
        exit 0
    fi
    sleep 3
done

echo "TIMEOUT: API did not become ready"
exit 1
