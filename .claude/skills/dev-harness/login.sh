#!/bin/bash
# Authenticate with the Feirb API and store JWT access token
# Usage: login.sh [username] [password]
#   Defaults to admin / password
# Note: Refresh token is stored as HttpOnly cookie by the server,
#       not in the response body.
set -euo pipefail

USERNAME="${1:-admin}"
PASSWORD="${2:-password}"

curl -sk -X POST https://localhost:7272/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}" \
    > /tmp/feirb-auth.json

python3 -c "
import sys, json
d = json.load(sys.stdin)
with open('/tmp/feirb-token.txt', 'w') as f:
    f.write(d['accessToken'])
print('Login OK (' + '$USERNAME' + '), access token stored')
" < /tmp/feirb-auth.json
