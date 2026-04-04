#!/bin/bash
# Show recent job execution logs from the database
# Usage: logs.sh [job-type-filter]
# Examples: logs.sh              (all jobs)
#           logs.sh classification
set -euo pipefail

FILTER="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -n "$FILTER" ]; then
    WHERE="WHERE js.\"JobType\" LIKE '%${FILTER}%'"
else
    WHERE=""
fi

"$SCRIPT_DIR/query.sh" "
SELECT
    js.\"JobName\",
    je.\"Status\",
    je.\"StartedAt\",
    je.\"FinishedAt\",
    LEFT(je.\"Error\", 200) as \"Error\"
FROM \"JobExecutions\" je
JOIN \"JobSettings\" js ON je.\"JobSettingsId\" = js.\"Id\"
${WHERE}
ORDER BY je.\"StartedAt\" DESC
LIMIT 20;
"
