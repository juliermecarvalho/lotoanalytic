#!/usr/bin/env bash
# Descobre proxies publicos brasileiros que conseguem consultar a API de loterias da Caixa.
#
# Proxies gratuitos ficam fora do ar o tempo todo, entao a unica validacao que vale e a
# chamada real: o proxy so entra na lista se devolver o JSON de um concurso conhecido.
#
# Uso:
#   bash scripts/find-caixa-proxies.sh [quantidade_desejada]
set -euo pipefail

WANTED="${1:-10}"
TEST_URL="${TEST_URL:-https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/3500}"
TIMEOUT="${TIMEOUT:-12}"
PARALLEL="${PARALLEL:-120}"
# As listas marcadas como brasileiras rendem poucas dezenas de enderecos. As listas globais sao
# grandes e o proprio teste ja filtra o que interessa: passar pelo bloqueio geografico da Caixa.
INCLUDE_GLOBAL="${INCLUDE_GLOBAL:-1}"
WORK_DIR="$(mktemp -d)"

cleanup() {
    rm -rf -- "$WORK_DIR"
}
trap cleanup EXIT

SOURCES=(
    "https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/countries/BR/data.txt"
    "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&country=BR&timeout=10000"
    "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&country=BR&timeout=10000"
)

if [[ "$INCLUDE_GLOBAL" == "1" ]]; then
    SOURCES+=(
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt"
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt"
        "https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/protocols/http/data.txt"
        "https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/protocols/socks5/data.txt"
    )
fi

printf 'Baixando listas de proxies...\n' >&2

# As listas misturam formatos: com esquema, sem esquema e com pais anexado apos o endereco.
normalize() {
    grep -oE '(socks5://|socks4://|http://)?([0-9]{1,3}\.){3}[0-9]{1,3}:[0-9]{2,5}' \
        | sed -E 's#^(([0-9]{1,3}\.){3}[0-9]{1,3}:[0-9]+)$#http://\1#' \
        | sort -u
}

for source in "${SOURCES[@]:0:3}"; do
    curl -fsSL --max-time 30 "$source" 2>/dev/null || true
done | normalize >"$WORK_DIR/brazilian.txt"

for source in "${SOURCES[@]:3}"; do
    curl -fsSL --max-time 30 "$source" 2>/dev/null || true
done | normalize >"$WORK_DIR/global.txt"

# Proxies anunciados como brasileiros vem primeiro: tendem a durar mais contra o bloqueio da Caixa.
cat "$WORK_DIR/brazilian.txt" >"$WORK_DIR/candidates.txt"
grep -Fxv -f "$WORK_DIR/brazilian.txt" "$WORK_DIR/global.txt" >>"$WORK_DIR/candidates.txt" 2>/dev/null || true

CANDIDATE_COUNT="$(wc -l <"$WORK_DIR/candidates.txt" | tr -d ' ')"

if [[ "$CANDIDATE_COUNT" -eq 0 ]]; then
    printf 'Nenhum proxy candidato foi obtido. Verifique a conectividade com as listas publicas.\n' >&2
    exit 1
fi

printf 'Testando %s candidatos contra a API da Caixa (%s em paralelo)...\n' \
    "$CANDIDATE_COUNT" "$PARALLEL" >&2

test_proxy() {
    local proxy="$1"
    local body

    # Exige JSON com o campo "numero" para descartar portais de login e paginas de bloqueio.
    body="$(curl -s --proxy "$proxy" --max-time "$TIMEOUT" \
        -H 'accept: application/json, text/plain, */*' \
        -H 'referer: https://loterias.caixa.gov.br/' \
        -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36' \
        "$TEST_URL" 2>/dev/null)" || return 1

    case "$body" in
        *'"numero"'*) printf '%s\n' "$proxy" ;;
        *) return 1 ;;
    esac
}
export -f test_proxy
export TEST_URL TIMEOUT

xargs -P "$PARALLEL" -I {} bash -c 'test_proxy "$@"' _ {} \
    <"$WORK_DIR/candidates.txt" \
    >"$WORK_DIR/unsorted.txt" 2>/dev/null || true

# Reordena os aprovados mantendo os brasileiros na frente da lista final.
grep -Fx -f "$WORK_DIR/brazilian.txt" "$WORK_DIR/unsorted.txt" >"$WORK_DIR/working.txt" 2>/dev/null || true
grep -Fxv -f "$WORK_DIR/brazilian.txt" "$WORK_DIR/unsorted.txt" >>"$WORK_DIR/working.txt" 2>/dev/null || true

WORKING_COUNT="$(wc -l <"$WORK_DIR/working.txt" | tr -d ' ')"

if [[ "$WORKING_COUNT" -eq 0 ]]; then
    printf '\nNenhum proxy publico conseguiu acessar a API da Caixa.\n' >&2
    printf 'Considere um relay proprio hospedado no Brasil.\n' >&2
    exit 2
fi

head -n "$WANTED" "$WORK_DIR/working.txt" >"$WORK_DIR/selected.txt"
SELECTED_COUNT="$(wc -l <"$WORK_DIR/selected.txt" | tr -d ' ')"

printf '\n%s de %s proxies funcionaram. Usando os %s primeiros.\n\n' \
    "$WORKING_COUNT" "$CANDIDATE_COUNT" "$SELECTED_COUNT" >&2

printf '# Gerado por scripts/find-caixa-proxies.sh\n'
printf '# %s proxies validados contra a API da Caixa.\n' "$SELECTED_COUNT"
printf 'CAIXA_PROXY_ENABLED=true\n'
printf 'CAIXA_PROXY_ADDRESSES=%s\n' "$(paste -sd, "$WORK_DIR/selected.txt")"
