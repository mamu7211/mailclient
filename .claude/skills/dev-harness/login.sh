#!/bin/bash
# Authenticate with the Feirb API and store JWT token
set -euo pipefail

curl -sk -X POST https://localhost:7272/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"admin@feirb.local"}' \
    > /tmp/feirb-auth.json

TOKEN=$(python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])" < /tmp/feirb-auth.json)
echo "$TOKEN" > /tmp/feirb-token.txt
echo "Login OK, token stored in /tmp/feirb-token.txt"
