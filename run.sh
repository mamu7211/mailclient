#!/bin/bash
# Starts Feirb via Aspire with database seeding enabled by default.
# Use --no-seeding to skip seeding.
# Use --watch to enable hot reload via dotnet watch.
# Use --auto-login to auto-login as admin in development.
#
# Test runners (--bruno and --playwright require the API to be running):
# Use --bruno [folder] to run Bruno API tests.
# Use --playwright [spec] to run Playwright E2E tests.
# Use --unit to run xUnit tests (no running API needed).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="${BASE_URL:-https://localhost:7272}"

check_api() {
    local status
    status=$(curl -k -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/setup/status" 2>/dev/null || echo "000")
    if [ "$status" != "200" ]; then
        echo "Error: API is not reachable at $BASE_URL"
        echo "Start it with: ./run.sh"
        exit 1
    fi
}

run_bruno() {
    local folder="${1:-}"
    check_api
    cd "$SCRIPT_DIR/tests/bruno"
    if [ -n "$folder" ]; then
        echo "Running Bruno tests: 01-auth/login.bru + $folder/"
        npx bru run 01-auth/login.bru "$folder/" --env local
    else
        echo "Running all Bruno tests"
        npx bru run --env local
    fi
}

run_playwright() {
    local spec="${1:-}"
    check_api
    cd "$SCRIPT_DIR/tests/playwright"
    if [ ! -d "node_modules" ]; then
        npm ci --silent
    fi
    if [ -n "$spec" ]; then
        echo "Running Playwright tests: $spec"
        npx playwright test "$spec"
    else
        echo "Running all Playwright tests"
        npx playwright test
    fi
}

run_unit() {
    echo "Running xUnit tests"
    dotnet test "$SCRIPT_DIR/Feirb.sln" --verbosity normal
}

# Test runner modes
case "${1:-}" in
    --bruno)
        shift
        arg=""
        if [ $# -gt 0 ] && [[ ! "$1" =~ ^-- ]]; then
            arg="$1"
        fi
        run_bruno "$arg"
        exit 0
        ;;
    --playwright)
        shift
        arg=""
        if [ $# -gt 0 ] && [[ ! "$1" =~ ^-- ]]; then
            arg="$1"
        fi
        run_playwright "$arg"
        exit 0
        ;;
    --unit)
        run_unit
        exit 0
        ;;
esac

# Default: start Aspire
SEED="true"
WATCH=""
AUTO_LOGIN=""

for arg in "$@"; do
    case "$arg" in
        --no-seeding) SEED="" ;;
        --watch) WATCH="true" ;;
        --auto-login) AUTO_LOGIN="true" ;;
        *) echo "Unknown argument: $arg"; exit 1 ;;
    esac
done

CMD="dotnet run"
if [ -n "$WATCH" ]; then
    CMD="dotnet watch"
fi

ENV_VARS=""
if [ -n "$SEED" ]; then
    ENV_VARS="FEIRB_SEED_DATA=true"
    echo "Starting Feirb with database seeding enabled ..."
else
    echo "Starting Feirb without database seeding ..."
fi

if [ -n "$AUTO_LOGIN" ]; then
    ENV_VARS="$ENV_VARS AUTO_LOGIN=true"
    echo "Auto-login enabled."
fi

env $ENV_VARS $CMD --project src/Feirb.AppHost
