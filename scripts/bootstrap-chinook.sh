#!/bin/sh
set -eu

: "${CHINOOK_OWNER_PASSWORD:?CHINOOK_OWNER_PASSWORD is required}"
: "${PDG_READER_PASSWORD:?PDG_READER_PASSWORD is required}"

export PGPASSWORD="$CHINOOK_OWNER_PASSWORD"

cd /bootstrap

echo "Verifying pinned Chinook artifact checksum..."
sha256sum -c db/chinook/10-chinook-1.4.5.sha256

table_count="$(
    psql \
        -h chinook-db \
        -U chinook_owner \
        -d chinook \
        -v ON_ERROR_STOP=1 \
        -Atc "
            SELECT count(*)
            FROM pg_class AS c
            JOIN pg_namespace AS n
              ON n.oid = c.relnamespace
            WHERE n.nspname = 'public'
              AND c.relkind IN ('r', 'p')
              AND c.relname IN (
                  'album',
                  'artist',
                  'customer',
                  'employee',
                  'genre',
                  'invoice',
                  'invoice_line',
                  'media_type',
                  'playlist',
                  'playlist_track',
                  'track'
              );
        "
)"

case "$table_count" in
    0)
        echo "Importing Chinook dataset..."
        psql \
            -h chinook-db \
            -U chinook_owner \
            -d chinook \
            -v ON_ERROR_STOP=1 \
            --single-transaction \
            -f /bootstrap/db/chinook/10-chinook-1.4.5.sql
        ;;
    11)
        echo "Chinook dataset already present; import skipped."
        ;;
    *)
        echo "Chinook bootstrap failed: partial dataset state detected ($table_count of 11 tables)." >&2
        exit 1
        ;;
esac

echo "Creating or updating corporate PDG projection..."
psql \
    -h chinook-db \
    -U chinook_owner \
    -d chinook \
    -v ON_ERROR_STOP=1 \
    -f /bootstrap/db/chinook/20-create-pdg-schema-view.sql

echo "Creating or updating pdg_reader..."
psql \
    -h chinook-db \
    -U chinook_owner \
    -d chinook \
    -v ON_ERROR_STOP=1 \
    -v pdg_reader_password="$PDG_READER_PASSWORD" \
    -f /bootstrap/db/chinook/30-create-pdg-reader.sql

echo "Applying corporate runtime grants..."
psql \
    -h chinook-db \
    -U chinook_owner \
    -d chinook \
    -v ON_ERROR_STOP=1 \
    -f /bootstrap/db/chinook/40-corporate-grants.sql

echo "Verifying corporate security boundary..."
/bin/sh /bootstrap/scripts/verify-corporate-security.sh

echo "Corporate Data Source bootstrap completed successfully."