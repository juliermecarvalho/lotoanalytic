#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
BACKUP_DIR="$ROOT/bd/backups"
DUMP_FILE="$BACKUP_DIR/loto.dump"
TEMP_DUMP="$BACKUP_DIR/.loto.dump.tmp"

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-lotoanalytics-postgres}"
POSTGRES_DATABASE="${POSTGRES_DATABASE:-lotoanalytics}"
POSTGRES_USER="${POSTGRES_USER:-lotoanalytics}"

DOKPLOY_HOST="${DOKPLOY_HOST:-116.202.106.2}"
DOKPLOY_USER="${DOKPLOY_USER:-root}"
DOKPLOY_PATH="${DOKPLOY_PATH:-/root/loto.dump}"
SSH_KEY="${SSH_KEY:-$HOME/hetzner-lotoanalytics_RKgaN7K2jklwaEQwnrcPs_private_id_ed25519.txt}"
REMOTE_POSTGRES_CONTAINER="${REMOTE_POSTGRES_CONTAINER:-lotoanalytics-stack-kbqasm-postgres-1}"
REMOTE_API_CONTAINER="${REMOTE_API_CONTAINER:-lotoanalytics-stack-kbqasm-api-1}"
REMOTE_POSTGRES_DATABASE="${REMOTE_POSTGRES_DATABASE:-lotoanalytics}"
REMOTE_POSTGRES_USER="${REMOTE_POSTGRES_USER:-lotoanalytics}"

cleanup() {
    rm -f -- "$TEMP_DUMP"
}
trap cleanup EXIT

command -v docker >/dev/null 2>&1 || {
    printf 'Erro: Docker nao encontrado no WSL.\n' >&2
    exit 1
}

command -v scp >/dev/null 2>&1 || {
    printf 'Erro: scp nao encontrado no WSL.\n' >&2
    exit 1
}

[[ -f "$SSH_KEY" ]] || {
    printf 'Erro: chave SSH nao encontrada em %s.\n' "$SSH_KEY" >&2
    exit 1
}

if [[ "$(stat -c '%a' "$SSH_KEY")" != "600" ]]; then
    chmod 600 -- "$SSH_KEY"
fi

if ! docker inspect --format '{{.State.Running}}' "$POSTGRES_CONTAINER" 2>/dev/null |
    grep -Fxq true; then
    printf 'Erro: o container %s nao esta em execucao.\n' "$POSTGRES_CONTAINER" >&2
    printf 'Inicie o ambiente antes de executar este script.\n' >&2
    exit 1
fi

mkdir -p -- "$BACKUP_DIR"
rm -f -- "$TEMP_DUMP"

printf 'Gerando backup do banco %s...\n' "$POSTGRES_DATABASE"
docker exec "$POSTGRES_CONTAINER" \
    pg_dump --username="$POSTGRES_USER" --dbname="$POSTGRES_DATABASE" \
    --format=custom --no-owner --no-privileges >"$TEMP_DUMP"

[[ -s "$TEMP_DUMP" ]] || {
    printf 'Erro: o dump gerado esta vazio.\n' >&2
    exit 1
}

mv -f -- "$TEMP_DUMP" "$DUMP_FILE"
printf 'Backup salvo em %s.\n' "$DUMP_FILE"

printf 'Enviando backup para %s@%s:%s...\n' \
    "$DOKPLOY_USER" "$DOKPLOY_HOST" "$DOKPLOY_PATH"
scp -i "$SSH_KEY" \
    -o BatchMode=yes \
    -o IdentitiesOnly=yes \
    "$DUMP_FILE" "$DOKPLOY_USER@$DOKPLOY_HOST:$DOKPLOY_PATH"

printf 'Backup enviado. Iniciando restauracao no servidor...\n'
ssh -i "$SSH_KEY" \
    -o BatchMode=yes \
    -o IdentitiesOnly=yes \
    "$DOKPLOY_USER@$DOKPLOY_HOST" \
    bash -s -- \
    "$DOKPLOY_PATH" \
    "$REMOTE_POSTGRES_CONTAINER" \
    "$REMOTE_API_CONTAINER" \
    "$REMOTE_POSTGRES_USER" \
    "$REMOTE_POSTGRES_DATABASE" <<'REMOTE_SCRIPT'
set -euo pipefail

dump_file="$1"
postgres_container="$2"
api_container="$3"
postgres_user="$4"
postgres_database="$5"
container_dump="/tmp/loto.dump"
api_stopped=false

restart_api() {
    if [[ "$api_stopped" == true ]]; then
        printf 'Religando a API...\n'
        docker start "$api_container" >/dev/null
    fi
}
trap restart_api EXIT

[[ -s "$dump_file" ]] || {
    printf 'Erro: dump remoto nao encontrado ou vazio em %s.\n' "$dump_file" >&2
    exit 1
}

printf 'Copiando o dump para o container PostgreSQL...\n'
docker cp "$dump_file" "$postgres_container:$container_dump"

printf 'Parando a API durante a restauracao...\n'
docker stop "$api_container" >/dev/null
api_stopped=true

printf 'Restaurando o banco %s...\n' "$postgres_database"
docker exec "$postgres_container" \
    pg_restore \
    --username="$postgres_user" \
    --dbname="$postgres_database" \
    --clean \
    --if-exists \
    --no-owner \
    "$container_dump"

printf 'Restauracao concluida.\n'
REMOTE_SCRIPT

printf 'Backup enviado e banco de producao atualizado com sucesso.\n'
