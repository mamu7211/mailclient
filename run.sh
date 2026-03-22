#!/bin/bash
# Starts Feirb via Aspire with database seeding enabled by default.
# Use --no-seeding to skip seeding.

SEED="true"

for arg in "$@"; do
    case "$arg" in
        --no-seeding) SEED="" ;;
        *) echo "Unknown argument: $arg"; exit 1 ;;
    esac
done

if [ -n "$SEED" ]; then
    echo "Starting Feirb with database seeding enabled ..."
    FEIRB_SEED_DATA=true dotnet run --project src/Feirb.AppHost
else
    echo "Starting Feirb without database seeding ..."
    dotnet run --project src/Feirb.AppHost
fi
