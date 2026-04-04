#!/bin/bash
# Check API endpoint with stored token
# Usage: check.sh <endpoint-path>
# Example: check.sh /api/jobs
set -euo pipefail

ENDPOINT="${1:?Usage: check.sh <endpoint-path>}"
TOKEN=$(cat /tmp/feirb-token.txt)
curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272${ENDPOINT}" | python3 -m json.tool
