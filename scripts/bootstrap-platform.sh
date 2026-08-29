#!/bin/sh
set -eu

: "${PLATFORM_OWNER_PASSWORD:?PLATFORM_OWNER_PASSWORD is required}"
: "${PLATFORM_APP_PASSWORD:?PLATFORM_APP_PASSWORD is required}"

export PGPASSWORD="$PLATFORM_OWNER_PASSWORD"

platform_owner_connection="Host=platform-store;Database=pdg_platform_store;Username=pdg_platform_owner;Password=${PLATFORM_OWNER_PASSWORD}"

echo "Applying Platform Store EF Core migrations..."
/bootstrap/pdg-platform-migrate \
    --connection "$platform_owner_connection"

echo "Applying Platform Store demo seed..."
psql \
    -h platform-store \
    -U pdg_platform_owner \
    -d pdg_platform_store \
    -v ON_ERROR_STOP=1 \
    --single-transaction \
    -f /bootstrap/db/platform/10-platform-seed.sql

echo "Creating or updating pdg_platform_app..."
psql \
    -h platform-store \
    -U pdg_platform_owner \
    -d pdg_platform_store \
    -v ON_ERROR_STOP=1 \
    -v platform_app_password="$PLATFORM_APP_PASSWORD" \
    -f /bootstrap/db/platform/20-create-platform-runtime-role.sql

echo "Applying Platform Store runtime grants..."
psql \
    -h platform-store \
    -U pdg_platform_owner \
    -d pdg_platform_store \
    -v ON_ERROR_STOP=1 \
    -f /bootstrap/db/platform/30-platform-grants.sql

echo "Verifying Platform Store security boundary..."
/bin/sh /bootstrap/scripts/verify-platform-security.sh

echo "Platform Store bootstrap completed successfully."