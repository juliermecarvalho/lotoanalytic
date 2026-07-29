#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

require_path() {
    local relative_path="$1"

    if [[ ! -e "$ROOT_DIR/$relative_path" ]]; then
        printf 'Missing required path: %s\n' "$relative_path" >&2
        return 1
    fi
}

require_path "apps/landing"
require_path "apps/api/src/Common"
require_path "apps/api/src/Infrastructure"
require_path "apps/api/src/Features"
require_path "apps/api/src/LotoAnalytics.Api/LotoAnalytics.Api.csproj"
require_path "apps/api/src/LotoAnalytics.Api/Controllers/HealthController.cs"
require_path "apps/api/tests/Unit"
require_path "apps/api/tests/Integration"
require_path "apps/api/tests/Integration/LotoAnalytics.Api.IntegrationTests.csproj"
require_path "apps/api/tests/Architecture"
require_path "apps/api/README.md"
require_path "apps/api/Dockerfile"
require_path "apps/web/src/components/ui"
require_path "apps/web/src/features"
require_path "apps/web/src/lib"
require_path "apps/web/tests"
require_path "apps/web/e2e"
require_path "apps/web/DESIGN_RULES.md"
require_path "apps/web/README.md"
require_path "apps/web/Dockerfile"
require_path "docs/PLANO_EXECUCAO.md"
require_path "docs/MODELAGEM_POSTGRESQL.md"
require_path "README.md"
require_path "PRODUCT_OVERVIEW.md"
require_path "TDD.md"
require_path "AGENTS.md"
require_path "CLAUDE.md"
require_path ".editorconfig"
require_path ".gitignore"
require_path "Makefile"
require_path "Directory.Build.props"
require_path "Directory.Packages.props"
require_path "NuGet.config"
require_path "global.json"
require_path "LotoAnalytics.slnx"
require_path "scripts/lib/common.sh"

printf 'monorepo structure tests passed\n'

