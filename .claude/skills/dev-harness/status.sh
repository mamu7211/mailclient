#!/bin/bash
# Check dev environment status
set -uo pipefail

echo "=== Dev Environment Status ==="

# Aspire
if [ -f /tmp/feirb-aspire.pid ] && kill -0 $(cat /tmp/feirb-aspire.pid) 2>/dev/null; then
    echo "Aspire:    RUNNING (PID $(cat /tmp/feirb-aspire.pid))"
else
    echo "Aspire:    STOPPED"
fi

# API
if curl -sk https://localhost:7272/health 2>/dev/null; then
    echo "API:       HEALTHY"
else
    echo "API:       DOWN"
fi

# Token
if [ -f /tmp/feirb-token.txt ]; then
    CODE=$(curl -sk -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $(cat /tmp/feirb-token.txt)" "https://localhost:7272/api/jobs")
    if [ "$CODE" = "200" ]; then
        echo "Token:     VALID"
    else
        echo "Token:     EXPIRED (HTTP $CODE)"
    fi
else
    echo "Token:     NONE"
fi

# GreenMail
if curl -s http://localhost:8080 >/dev/null 2>&1; then
    echo "GreenMail: UP"
else
    echo "GreenMail: DOWN"
fi

# Ollama
if curl -s http://localhost:11434/api/tags >/dev/null 2>&1; then
    echo "Ollama:    UP"
else
    echo "Ollama:    DOWN"
fi
