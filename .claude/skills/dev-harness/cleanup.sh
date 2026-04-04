#!/bin/bash
# Stop Aspire and remove Postgres container + volumes for fresh start
set -uo pipefail

if [ -f /tmp/feirb-aspire.pid ]; then
    kill $(cat /tmp/feirb-aspire.pid) 2>/dev/null || true
    rm /tmp/feirb-aspire.pid
fi

sleep 2

CRUNTIME=$(command -v podman &>/dev/null && echo podman || echo docker)
$CRUNTIME rm -f $($CRUNTIME ps -a --format '{{.Names}}' | grep postgres) 2>/dev/null || true
$CRUNTIME volume prune -f 2>/dev/null || true

echo "Cleaned up"
