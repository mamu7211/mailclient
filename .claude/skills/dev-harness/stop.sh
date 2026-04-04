#!/bin/bash
# Stop Aspire AppHost
set -euo pipefail

if [ -f /tmp/feirb-aspire.pid ]; then
    kill $(cat /tmp/feirb-aspire.pid) 2>/dev/null || true
    rm /tmp/feirb-aspire.pid
    echo "Aspire stopped"
else
    echo "No PID file found, trying pkill"
    pkill -f "Feirb.AppHost" 2>/dev/null || true
fi
