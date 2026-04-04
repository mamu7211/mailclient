#!/bin/bash
# Trigger a job run by type name
# Usage: trigger-job.sh <job-type>
# Examples: trigger-job.sh classification
#           trigger-job.sh imap-sync
set -euo pipefail

JOB_TYPE="${1:?Usage: trigger-job.sh <job-type>}"
TOKEN=$(cat /tmp/feirb-token.txt)

JOB_IDS=$(curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/jobs" | \
    python3 -c "
import sys, json
data = json.load(sys.stdin)
for j in data:
    if '$JOB_TYPE' in j['jobType']:
        print(j['id'], j.get('description', j['jobName']))
")

if [ -z "$JOB_IDS" ]; then
    echo "No jobs found matching type '$JOB_TYPE'"
    exit 1
fi

while IFS= read -r line; do
    JOB_ID=$(echo "$line" | cut -d' ' -f1)
    JOB_DESC=$(echo "$line" | cut -d' ' -f2-)
    HTTP_CODE=$(curl -sk -o /dev/null -w '%{http_code}' -X POST \
        "https://localhost:7272/api/jobs/$JOB_ID/run" \
        -H "Authorization: Bearer $TOKEN")
    echo "Triggered: $JOB_DESC (HTTP $HTTP_CODE)"
done <<< "$JOB_IDS"
