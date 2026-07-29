#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

source "$ROOT_DIR/scripts/lib/common.sh"

assert_slugify_db() {
    local input="$1"
    local expected="$2"
    local actual

    actual="$(slugify_db "$input")"

    if [[ "$actual" != "$expected" ]]; then
        printf 'slugify_db(%q): expected %q, got %q\n' "$input" "$expected" "$actual" >&2
        return 1
    fi
}

assert_slugify_db "LotoAnalytics" "lotoanalytics"
assert_slugify_db "Mega-Sena Analytics" "mega_sena_analytics"
assert_slugify_db "Loto@Analytics 2026!" "lotoanalytics_2026"
assert_slugify_db "  Dia de Sorte  " "dia_de_sorte"

printf 'scripts/lib/common.sh tests passed\n'
