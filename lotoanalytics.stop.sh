#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$ROOT/docker-compose.yml"
REMOVE_VOLUMES=false

usage() {
    cat <<'EOF'
Uso: ./lotoanalytics.stop.sh [opcoes]

Opcoes:
  --remove-volumes  Remove tambem os volumes nomeados do Docker Compose.
  -h, --help        Exibe esta ajuda.
EOF
}

while (($# > 0)); do
    case "$1" in
        --remove-volumes) REMOVE_VOLUMES=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) printf 'Opcao desconhecida: %s\n' "$1" >&2; usage >&2; exit 2 ;;
    esac
done

[[ -f "$COMPOSE_FILE" ]] || { printf 'docker-compose.yml nao encontrado em %s\n' "$COMPOSE_FILE" >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { printf 'Docker nao encontrado no PATH.\n' >&2; exit 1; }

down_arguments=(down)
[[ "$REMOVE_VOLUMES" == true ]] && down_arguments+=(--volumes)

printf 'LotoAnalytics - parando ambiente Docker Compose\n'
docker compose --file "$COMPOSE_FILE" "${down_arguments[@]}"
printf 'Ambiente parado.\n'
printf 'Dados PostgreSQL locais preservados em: %s\n' "$ROOT/bd/postgres-data"
