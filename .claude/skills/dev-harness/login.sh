#!/bin/bash
# Authenticate with the Feirb API and store JWT tokens
# Usage: login.sh [username] [password]
#   Defaults to admin / admin@feirb.local
set -euo pipefail

USERNAME="${1:-admin}"
PASSWORD="${2:-admin@feirb.local}"

curl -sk -X POST https://localhost:7272/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}" \
    > /tmp/feirb-auth.json

python3 -c "
import sys, json
d = json.load(sys.stdin)
with open('/tmp/feirb-token.txt', 'w') as f:
    f.write(d['accessToken'])
with open('/tmp/feirb-refresh-token.txt', 'w') as f:
    f.write(d['refreshToken'])
print('Login OK (' + '$USERNAME' + '), tokens stored')
" < /tmp/feirb-auth.json
