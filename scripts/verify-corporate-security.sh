#!/bin/sh
set -eu

: "${CHINOOK_OWNER_PASSWORD:?CHINOOK_OWNER_PASSWORD is required}"

export PGPASSWORD="$CHINOOK_OWNER_PASSWORD"

psql \
    -h chinook-db \
    -U chinook_owner \
    -d chinook \
    -v ON_ERROR_STOP=1 \
    -f /bootstrap/db/chinook/50-verify-corporate-security.sql