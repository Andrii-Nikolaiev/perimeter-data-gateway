#!/bin/sh
set -eu

: "${PLATFORM_OWNER_PASSWORD:?PLATFORM_OWNER_PASSWORD is required}"
: "${PLATFORM_APP_PASSWORD:?PLATFORM_APP_PASSWORD is required}"
: "${CHINOOK_OWNER_PASSWORD:?CHINOOK_OWNER_PASSWORD is required}"
: "${PDG_READER_PASSWORD:?PDG_READER_PASSWORD is required}"

echo "Starting PDG bootstrap..."

echo "Bootstrapping Platform Store..."
/bin/sh /bootstrap/scripts/bootstrap-platform.sh

echo "Bootstrapping Corporate Data Source..."
/bin/sh /bootstrap/scripts/bootstrap-chinook.sh

echo "PDG bootstrap completed successfully."