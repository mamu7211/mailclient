#!/bin/bash
# Start Aspire AppHost, capture logs
# Usage: start.sh [--seeding] [--restart]
#   --seeding   Seed database with test data (users, mailboxes, labels, jobs)
#   --restart   Stop running instance, rebuild, and restart (keeps volumes)
#   (no flags)  Start bare — fresh environment, no seed data
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/../../.."

SEED=false
RESTART=false

for arg in "$@"; do
    case "$arg" in
        --seeding) SEED=true ;;
        --restart) RESTART=true ;;
        *) echo "Unknown argument: $arg"; exit 1 ;;
    esac
done

if [ "$RESTART" = true ]; then
    "$SCRIPT_DIR/stop.sh"
    sleep 1
elif curl -sk https://localhost:7272/health 2>/dev/null; then
    echo "API already running"
    exit 0
fi

ENV_VARS=""
if [ "$SEED" = true ]; then
    ENV_VARS="FEIRB_SEED_DATA=true"
    echo "Starting Aspire with database seeding..."
else
    echo "Starting Aspire without seeding..."
fi

if [ -n "$ENV_VARS" ]; then
    env $ENV_VARS dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
else
    dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
fi

echo $! > /tmp/feirb-aspire.pid
echo "Started Aspire (PID $(cat /tmp/feirb-aspire.pid))"

for i in $(seq 1 40); do
    if curl -sk https://localhost:7272/health 2>/dev/null; then
        echo "Healthy"
        echo "API ready (attempt $i)"
        DASHBOARD_URL=$(grep -oE 'https://localhost:[0-9]+/login\?t=[a-f0-9]+' /tmp/feirb-aspire.log | head -1)
        if [ -n "$DASHBOARD_URL" ]; then
            echo "Aspire dashboard: $DASHBOARD_URL"
        fi
        exit 0
    fi
    sleep 3
done

echo "TIMEOUT: API did not become ready"
exit 1
