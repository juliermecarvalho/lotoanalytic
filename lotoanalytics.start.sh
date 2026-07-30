#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$ROOT/docker-compose.yml"

WEB_URL="http://127.0.0.1:5174"
API_URL="http://localhost:5291"
KEYCLOAK_URL="http://localhost:8080/realms/lotoanalytics"
MAILPIT_URL="http://localhost:8025"
NO_BUILD=false
NO_BROWSER=false
STOP_CONFLICTS=false
ADMIN_USER_EMAILS=("juliermecarvalho@gmail.com")
ADMIN_EMAIL_OVERRIDDEN=false

usage() {
    cat <<'EOF'
Uso: ./lotoanalytics.start.sh [opcoes]

Opcoes:
  --web-url URL              URL do frontend.
  --api-url URL              URL da API.
  --keycloak-url URL         URL do realm no Keycloak.
  --mailpit-url URL          URL do Mailpit.
  --admin-user-email EMAIL   E-mail/usuario administrador (pode ser repetido).
  --no-build                 Nao reconstrói as imagens.
  --no-browser               Nao abre o navegador.
  --stop-conflicts           Encerra processos locais que ocupam as portas.
  -h, --help                 Exibe esta ajuda.
EOF
}

while (($# > 0)); do
    case "$1" in
        --web-url|--api-url|--keycloak-url|--mailpit-url|--admin-user-email)
            (($# >= 2)) || { printf 'Valor ausente para %s.\n' "$1" >&2; exit 2; }
            option="$1"
            value="$2"
            case "$option" in
                --web-url) WEB_URL="$value" ;;
                --api-url) API_URL="$value" ;;
                --keycloak-url) KEYCLOAK_URL="$value" ;;
                --mailpit-url) MAILPIT_URL="$value" ;;
                --admin-user-email)
                    if [[ "$ADMIN_EMAIL_OVERRIDDEN" == false ]]; then
                        ADMIN_USER_EMAILS=()
                        ADMIN_EMAIL_OVERRIDDEN=true
                    fi
                    ADMIN_USER_EMAILS+=("$value")
                    ;;
            esac
            shift 2
            ;;
        --no-build) NO_BUILD=true; shift ;;
        --no-browser) NO_BROWSER=true; shift ;;
        --stop-conflicts) STOP_CONFLICTS=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) printf 'Opcao desconhecida: %s\n' "$1" >&2; usage >&2; exit 2 ;;
    esac
done

compose() {
    docker compose --file "$COMPOSE_FILE" "$@"
}

get_listener_pid() {
    local port="$1"

    if command -v lsof >/dev/null 2>&1; then
        { lsof -nP -t -iTCP:"$port" -sTCP:LISTEN 2>/dev/null || true; } |
            head -n 1
        return
    fi

    if command -v ss >/dev/null 2>&1; then
        ss -H -ltnp "sport = :$port" 2>/dev/null |
            sed -nE 's/.*pid=([0-9]+).*/\1/p' |
            head -n 1
        return
    fi

    printf 'Instale lsof ou iproute2 (ss) para verificar conflitos de porta.\n' >&2
    return 1
}

assert_compose_ports_available() {
    local entries=("5432:postgres" "1025:mailpit" "8025:mailpit" "8080:keycloak" "5291:api" "5174:web")
    local running_services entry port service pid process_name
    local -a conflict_pids=()

    printf 'Verificando portas usadas pelo ambiente...\n'
    running_services="$(compose ps --services --status running 2>/dev/null || true)"

    for entry in "${entries[@]}"; do
        port="${entry%%:*}"
        service="${entry#*:}"
        if grep -Fxq "$service" <<<"$running_services"; then
            continue
        fi

        pid="$(get_listener_pid "$port")"
        [[ -n "$pid" ]] || continue
        process_name="$(ps -p "$pid" -o comm= 2>/dev/null || printf 'desconhecido')"
        printf 'Porta %s (servico %s) ocupada por %s (PID %s).\n' \
            "$port" "$service" "$process_name" "$pid"

        if [[ "$process_name" =~ ^(com\.docker|docker|vpnkit|wslrelay|wslhost) ]]; then
            printf '%s\n' 'Ha portas ocupadas pela infraestrutura do Docker sem o servico correspondente. Verifique outros projetos com docker ps.' >&2
            return 1
        fi
        conflict_pids+=("$pid")
    done

    if ((${#conflict_pids[@]} == 0)); then
        printf 'Portas livres para o Docker Compose.\n'
        return
    fi

    if [[ "$STOP_CONFLICTS" != true ]]; then
        printf '%s\n' 'Portas em conflito com processos locais. Encerre-os ou execute novamente com --stop-conflicts.' >&2
        return 1
    fi

    mapfile -t conflict_pids < <(printf '%s\n' "${conflict_pids[@]}" | sort -nu)
    for pid in "${conflict_pids[@]}"; do
        printf 'Encerrando processo PID %s para liberar as portas do ambiente...\n' "$pid"
        kill "$pid"
    done
    sleep 1
    for pid in "${conflict_pids[@]}"; do
        kill -0 "$pid" 2>/dev/null && kill -KILL "$pid" || true
    done
    printf 'Conflitos encerrados.\n'
}

wait_http_endpoint() {
    local url="$1"
    local timeout_seconds="${2:-120}"
    local deadline=$((SECONDS + timeout_seconds))

    while ((SECONDS < deadline)); do
        if curl --silent --show-error --location --max-time 3 \
            --output /dev/null --write-out '%{http_code}' "$url" 2>/dev/null |
            grep -Eq '^[234][0-9]{2}$'; then
            return 0
        fi
        sleep 2
    done
    return 1
}

kcadm() {
    docker exec lotoanalytics-keycloak /opt/keycloak/bin/kcadm.sh "$@"
}

json_rows() {
    python3 -c '
import json, sys
for item in json.load(sys.stdin):
    print("\t".join(str(item.get(field, "")) for field in sys.argv[1:]))
' "$@"
}

set_keycloak_portuguese_locale() {
    local realm user_id

    printf 'Configurando Keycloak em PT-BR...\n'
    kcadm config credentials --server http://localhost:8080 --realm master \
        --user admin --password admin >/dev/null

    for realm in master lotoanalytics; do
        kcadm update "realms/$realm" \
            -s internationalizationEnabled=true \
            -s defaultLocale=pt-BR \
            -s 'supportedLocales=["pt-BR"]' >/dev/null

        while IFS=$'\t' read -r user_id; do
            [[ -n "$user_id" ]] || continue
            kcadm update "users/$user_id" -r "$realm" \
                -s attributes.locale=pt-BR >/dev/null
        done < <(kcadm get users -r "$realm" --fields id -q max=1000 | json_rows id)
    done
    printf 'Keycloak configurado em PT-BR.\n'
}

set_keycloak_login_settings() {
    printf 'Configurando opcoes de login do Keycloak...\n'
    kcadm update realms/lotoanalytics \
        -s registrationAllowed=true \
        -s registrationEmailAsUsername=true \
        -s resetPasswordAllowed=true \
        -s verifyEmail=true >/dev/null
    printf 'Opcoes de login configuradas.\n'
}

set_keycloak_development_smtp() {
    local smtp_server
    smtp_server='{"host":"mailpit","port":"1025","from":"noreply@lotoanalytics.local","fromDisplayName":"LotoAnalytics","replyTo":"suporte@lotoanalytics.local","replyToDisplayName":"Suporte LotoAnalytics","ssl":"false","starttls":"false","auth":"false"}'

    printf 'Configurando SMTP de desenvolvimento do Keycloak...\n'
    kcadm update realms/lotoanalytics -s "smtpServer=$smtp_server" >/dev/null
    printf 'SMTP de desenvolvimento configurado.\n'
}

has_admin_identity() {
    local candidate="$1" admin_identity
    for admin_identity in "${ADMIN_USER_EMAILS[@]}"; do
        [[ "$candidate" == "$admin_identity" ]] && return 0
    done
    return 1
}

set_keycloak_application_roles() {
    local user_id username email role_names

    printf 'Configurando papeis dos usuarios no Keycloak...\n'
    while IFS=$'\t' read -r user_id username email; do
        [[ -n "$user_id" ]] || continue
        if has_admin_identity "$email" || has_admin_identity "$username"; then
            role_names="$(kcadm get "users/$user_id/role-mappings/realm/composite" \
                -r lotoanalytics --fields name | json_rows name)"
            if ! grep -Fxq administrador <<<"$role_names"; then
                kcadm add-roles -r lotoanalytics --uid "$user_id" \
                    --rolename administrador >/dev/null
            fi
        fi
    done < <(kcadm get users -r lotoanalytics --fields id,username,email \
        -q max=1000 | json_rows id username email)
    printf 'Papeis dos usuarios configurados.\n'
}

show_contest_base_status() {
    local response contest total
    if response="$(curl --fail --silent --show-error --max-time 5 \
        "$API_URL/concursos/lotofacil/ultimo")"; then
        read -r contest total < <(
            python3 -c 'import json,sys; d=json.load(sys.stdin); print(d["numeroConcurso"], d["totalConcursos"])' \
                <<<"$response"
        )
        printf 'Base de concursos carregada: concurso %s com %s sorteios.\n' "$contest" "$total"
    else
        printf '%s\n' 'Base de concursos ainda vazia: o atualizador automatico importa em background (a tela usa a base de exemplo ate concluir).'
    fi
}

open_browser() {
    local url="$1"
    if command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$url" >/dev/null 2>&1 &
    elif command -v open >/dev/null 2>&1; then
        open "$url" >/dev/null 2>&1 &
    else
        printf 'Nao foi possivel abrir o navegador automaticamente: %s\n' "$url"
    fi
}

[[ -f "$COMPOSE_FILE" ]] || { printf 'docker-compose.yml nao encontrado em %s\n' "$COMPOSE_FILE" >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { printf 'Docker nao encontrado no PATH.\n' >&2; exit 1; }
command -v curl >/dev/null 2>&1 || { printf 'curl nao encontrado no PATH.\n' >&2; exit 1; }
command -v python3 >/dev/null 2>&1 || { printf 'python3 nao encontrado no PATH.\n' >&2; exit 1; }

printf 'LotoAnalytics - subindo ambiente Docker Compose\n'
printf 'PostgreSQL: localhost:5432\nKeycloak: %s\nMailpit: %s\nAPI: %s\nWeb: %s\n\n' \
    "$KEYCLOAK_URL" "$MAILPIT_URL" "$API_URL" "$WEB_URL"
printf 'Usuario local Keycloak: dev / dev123\nAdmin Keycloak: admin / admin\n\n'

assert_compose_ports_available

up_arguments=(up -d)
[[ "$NO_BUILD" == true ]] || up_arguments+=(--build)
compose "${up_arguments[@]}"

printf '\nAguardando Keycloak responder...\n'
if wait_http_endpoint "$KEYCLOAK_URL"; then
    printf 'Keycloak pronto.\n'
    set_keycloak_portuguese_locale
    set_keycloak_login_settings
    set_keycloak_development_smtp
    set_keycloak_application_roles
else
    printf 'Keycloak ainda nao respondeu; use docker compose logs keycloak.\n'
fi

printf 'Aguardando Mailpit responder...\n'
wait_http_endpoint "$MAILPIT_URL" &&
    printf 'Mailpit pronto.\n' ||
    printf 'Mailpit ainda nao respondeu; use docker compose logs mailpit.\n'

printf 'Aguardando API responder...\n'
if wait_http_endpoint "$API_URL/health"; then
    printf 'API pronta.\n'
    if curl --fail --silent --output /dev/null --max-time 5 \
        "$API_URL/estatisticas/lotofacil/filtros"; then
        printf 'API com o build atual (endpoints de estatisticas disponiveis).\n'
        show_contest_base_status
    else
        printf '%s\n' 'API respondeu, mas sem os endpoints atuais: provavelmente um build antigo na porta 5291. Rode novamente sem --no-build e confira se nao ha um dotnet run local.'
    fi
else
    printf 'API ainda nao respondeu; use docker compose logs api.\n'
fi

printf 'Aguardando frontend responder...\n'
web_ready=false
if wait_http_endpoint "$WEB_URL"; then
    web_ready=true
    printf 'Frontend pronto.\n'
else
    printf 'Frontend ainda nao respondeu; use docker compose logs web.\n'
fi

if [[ "$NO_BROWSER" != true && "$web_ready" == true ]]; then
    open_browser "$WEB_URL/concursos/importar"
fi

printf '\nPara encerrar tudo: ./lotoanalytics.stop.sh\n'
