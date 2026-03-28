#!/bin/bash
# Starts Feirb via Aspire with database seeding enabled by default.
# Use --no-seeding to skip seeding.
# Use --watch to enable hot reload via dotnet watch.
# Use --auto-login to auto-login as admin in development.

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
