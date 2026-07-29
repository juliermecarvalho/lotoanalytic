#!/usr/bin/env bash
set -euo pipefail

# Converte nomes de produto para identificadores seguros de banco PostgreSQL.
slugify_db() {
    local value="$1"

    value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
    value="$(printf '%s' "$value" | sed -E 's/[[:space:]-]+/_/g')"
    value="$(printf '%s' "$value" | sed -E 's/[^a-z0-9_]+//g')"
    value="$(printf '%s' "$value" | sed -E 's/_+/_/g; s/^_+//; s/_+$//')"

    printf '%s\n' "$value"
}
