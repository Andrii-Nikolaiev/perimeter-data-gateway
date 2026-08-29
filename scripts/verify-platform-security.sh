#!/bin/sh
set -eu

: "${PLATFORM_OWNER_PASSWORD:?PLATFORM_OWNER_PASSWORD is required}"

export PGPASSWORD="$PLATFORM_OWNER_PASSWORD"

psql \
    -h platform-store \
    -U pdg_platform_owner \
    -d pdg_platform_store \
    -v ON_ERROR_STOP=1 \
    -f /bootstrap/db/platform/40-verify-platform-security.sql