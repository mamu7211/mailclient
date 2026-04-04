#!/bin/bash
# Query PostgreSQL via local psql
# Usage: query.sh '<SQL>'
set -euo pipefail

SQL="${1:?Usage: query.sh '<SQL>'}"

CRUNTIME=$(command -v podman &>/dev/null && echo podman || echo docker)
PG_CONTAINER=$($CRUNTIME ps --format '{{.Names}}' | grep postgres | head -1)
PG_PASS=$($CRUNTIME exec "$PG_CONTAINER" printenv POSTGRES_PASSWORD)
PGPASSWORD="$PG_PASS" psql -h localhost -p 15432 -U postgres mailclientdb -c "$SQL"
