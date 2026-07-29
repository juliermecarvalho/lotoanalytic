#!/usr/bin/env python3
import argparse
import csv
import json
import sqlite3
from datetime import datetime
from pathlib import Path


TABLES = {
    "resultados_lotofacil": ("lotofacil", 15),
    "resultados_megasena": ("mega_sena", 6),
    "resultados_quina": ("quina", 5),
    "resultados_maismilionaria": ("maismilionaria", 6),
    "resultados_lotomania": ("lotomania", 20),
    "resultados_timemania": ("timemania", 7),
    "resultados_duplasena": ("dupla_sena", 6),
    "resultados_diadesorte": ("dia_de_sorte", 7),
    "resultados_supersete": ("super_sete", 7),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Gera staging para importar concursos do SQLite local.")
    parser.add_argument("--sqlite", required=True, type=Path, help="Caminho do banco SQLite local.")
    parser.add_argument("--out", required=True, type=Path, help="Pasta de saida dos CSVs e SQL.")
    return parser.parse_args()


def parse_date(value: object) -> str:
    if value is None:
        return ""

    text = str(value).strip()
    if not text:
        return ""

    for fmt in ("%d/%m/%Y", "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt).date().isoformat()
        except ValueError:
            continue

    return ""


def parse_bool(value: object) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"

    if value is None:
        return "false"

    text = str(value).strip().lower()
    return "true" if text in {"1", "true", "sim", "s"} else "false"


def parse_money(value: object) -> str:
    if value is None:
        return ""

    text = str(value).strip()
    if not text:
        return ""

    if "," in text and "." in text:
        text = text.replace(".", "").replace(",", ".")
    elif "," in text:
        text = text.replace(",", ".")

    return text


def sanitize_json(value: object) -> object:
    if isinstance(value, dict):
        return {key: sanitize_json(child) for key, child in value.items()}

    if isinstance(value, list):
        return [sanitize_json(child) for child in value]

    if isinstance(value, str):
        return value.replace("\x00", "")

    return value


def to_json_object(raw: object) -> dict:
    if raw is None:
        return {}

    text = str(raw).strip()
    if not text:
        return {}

    try:
        parsed = json.loads(text)
    except json.JSONDecodeError:
        return {}

    return sanitize_json(parsed) if isinstance(parsed, dict) else {}


def as_list(value: object) -> list[object]:
    return value if isinstance(value, list) else []


def number_value(value: object) -> str:
    text = str(value).strip()
    if not text:
        return ""

    try:
        return str(int(text))
    except ValueError:
        return ""


def clean_text(value: object) -> str:
    if value is None:
        return ""

    return str(value).replace("\x00", "").strip()


def first_text(data: dict, *keys: str) -> str:
    for key in keys:
        value = clean_text(data.get(key))
        if value:
            return value
    return ""


def row_value(row: sqlite3.Row, key: str) -> object:
    return row[key] if key in row.keys() else None


def collect_main_numbers(row: sqlite3.Row, result: dict, expected_count: int) -> list[object]:
    numbers = as_list(result.get("listaDezenas"))
    if numbers:
        return numbers

    values = []
    for index in range(1, expected_count + 1):
        value = row_value(row, f"dezenas{index:02d}")
        if value is not None and str(value).strip():
            values.append(value)
    return values


def write_import_sql(output_dir: Path) -> None:
    sql = """
\\set ON_ERROR_STOP on

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TEMP TABLE stg_concursos (
    modalidade_codigo text NOT NULL,
    numero integer NOT NULL,
    numero_concurso_anterior integer NULL,
    numero_concurso_proximo integer NULL,
    data_apuracao date NULL,
    data_proximo_concurso date NULL,
    local_sorteio text NULL,
    municipio_uf_sorteio text NULL,
    acumulado boolean NOT NULL,
    ultimo_concurso boolean NOT NULL,
    valor_arrecadado numeric(14,2) NULL,
    valor_estimado_proximo_concurso numeric(14,2) NULL,
    valor_acumulado_proximo_concurso numeric(14,2) NULL,
    resultado_especial text NULL,
    result_json jsonb NOT NULL
);

CREATE TEMP TABLE stg_dezenas (
    modalidade_codigo text NOT NULL,
    numero integer NOT NULL,
    tipo text NOT NULL,
    posicao integer NOT NULL,
    valor text NOT NULL,
    valor_numero integer NULL
);

CREATE TEMP TABLE stg_rateios (
    modalidade_codigo text NOT NULL,
    numero integer NOT NULL,
    faixa integer NOT NULL,
    descricao_faixa text NOT NULL,
    numero_ganhadores integer NOT NULL,
    valor_premio numeric(14,2) NOT NULL
);

CREATE TEMP TABLE stg_ganhadores_municipios (
    modalidade_codigo text NOT NULL,
    numero integer NOT NULL,
    municipio text NOT NULL,
    uf text NOT NULL,
    ganhadores integer NOT NULL
);

\\copy stg_concursos FROM '/tmp/lotoanalytics-import/concursos.csv' WITH (FORMAT csv, HEADER true, NULL '')
\\copy stg_dezenas FROM '/tmp/lotoanalytics-import/dezenas.csv' WITH (FORMAT csv, HEADER true, NULL '')
\\copy stg_rateios FROM '/tmp/lotoanalytics-import/rateios.csv' WITH (FORMAT csv, HEADER true, NULL '')
\\copy stg_ganhadores_municipios FROM '/tmp/lotoanalytics-import/ganhadores_municipios.csv' WITH (FORMAT csv, HEADER true, NULL '')

INSERT INTO concursos (
    id,
    modalidade_id,
    numero,
    numero_concurso_anterior,
    numero_concurso_proximo,
    data_apuracao,
    data_proximo_concurso,
    local_sorteio,
    municipio_uf_sorteio,
    acumulado,
    ultimo_concurso,
    valor_arrecadado,
    valor_estimado_proximo_concurso,
    valor_acumulado_proximo_concurso,
    resultado_especial,
    result_json,
    criado_em,
    atualizado_em
)
SELECT
    gen_random_uuid(),
    modalidades.id,
    stg_concursos.numero,
    stg_concursos.numero_concurso_anterior,
    stg_concursos.numero_concurso_proximo,
    stg_concursos.data_apuracao,
    stg_concursos.data_proximo_concurso,
    NULLIF(left(stg_concursos.local_sorteio, 160), ''),
    NULLIF(left(stg_concursos.municipio_uf_sorteio, 160), ''),
    stg_concursos.acumulado,
    stg_concursos.ultimo_concurso,
    stg_concursos.valor_arrecadado,
    stg_concursos.valor_estimado_proximo_concurso,
    stg_concursos.valor_acumulado_proximo_concurso,
    NULLIF(left(stg_concursos.resultado_especial, 160), ''),
    stg_concursos.result_json,
    now(),
    now()
FROM stg_concursos
JOIN modalidades ON modalidades.codigo = stg_concursos.modalidade_codigo
ON CONFLICT (modalidade_id, numero) DO UPDATE SET
    numero_concurso_anterior = EXCLUDED.numero_concurso_anterior,
    numero_concurso_proximo = EXCLUDED.numero_concurso_proximo,
    data_apuracao = EXCLUDED.data_apuracao,
    data_proximo_concurso = EXCLUDED.data_proximo_concurso,
    local_sorteio = EXCLUDED.local_sorteio,
    municipio_uf_sorteio = EXCLUDED.municipio_uf_sorteio,
    acumulado = EXCLUDED.acumulado,
    ultimo_concurso = EXCLUDED.ultimo_concurso,
    valor_arrecadado = EXCLUDED.valor_arrecadado,
    valor_estimado_proximo_concurso = EXCLUDED.valor_estimado_proximo_concurso,
    valor_acumulado_proximo_concurso = EXCLUDED.valor_acumulado_proximo_concurso,
    resultado_especial = EXCLUDED.resultado_especial,
    result_json = EXCLUDED.result_json,
    atualizado_em = now();

DELETE FROM concurso_dezenas
USING concursos
JOIN modalidades ON modalidades.id = concursos.modalidade_id
JOIN stg_concursos ON stg_concursos.modalidade_codigo = modalidades.codigo AND stg_concursos.numero = concursos.numero
WHERE concurso_dezenas.concurso_id = concursos.id;

INSERT INTO concurso_dezenas (id, concurso_id, tipo, posicao, valor, valor_numero, criado_em)
SELECT
    gen_random_uuid(),
    concursos.id,
    left(stg_dezenas.tipo, 30),
    stg_dezenas.posicao,
    left(stg_dezenas.valor, 4),
    stg_dezenas.valor_numero,
    now()
FROM stg_dezenas
JOIN modalidades ON modalidades.codigo = stg_dezenas.modalidade_codigo
JOIN concursos ON concursos.modalidade_id = modalidades.id AND concursos.numero = stg_dezenas.numero;

DELETE FROM concurso_rateios
USING concursos
JOIN modalidades ON modalidades.id = concursos.modalidade_id
JOIN stg_concursos ON stg_concursos.modalidade_codigo = modalidades.codigo AND stg_concursos.numero = concursos.numero
WHERE concurso_rateios.concurso_id = concursos.id;

INSERT INTO concurso_rateios (id, concurso_id, faixa, descricao_faixa, numero_ganhadores, valor_premio, criado_em)
SELECT
    gen_random_uuid(),
    concursos.id,
    stg_rateios.faixa,
    left(stg_rateios.descricao_faixa, 120),
    stg_rateios.numero_ganhadores,
    stg_rateios.valor_premio,
    now()
FROM stg_rateios
JOIN modalidades ON modalidades.codigo = stg_rateios.modalidade_codigo
JOIN concursos ON concursos.modalidade_id = modalidades.id AND concursos.numero = stg_rateios.numero;

DELETE FROM concurso_ganhadores_municipios
USING concursos
JOIN modalidades ON modalidades.id = concursos.modalidade_id
JOIN stg_concursos ON stg_concursos.modalidade_codigo = modalidades.codigo AND stg_concursos.numero = concursos.numero
WHERE concurso_ganhadores_municipios.concurso_id = concursos.id;

INSERT INTO concurso_ganhadores_municipios (id, concurso_id, municipio, uf, ganhadores, criado_em)
SELECT
    gen_random_uuid(),
    concursos.id,
    left(stg_ganhadores_municipios.municipio, 120),
    left(stg_ganhadores_municipios.uf, 2),
    stg_ganhadores_municipios.ganhadores,
    now()
FROM stg_ganhadores_municipios
JOIN modalidades ON modalidades.codigo = stg_ganhadores_municipios.modalidade_codigo
JOIN concursos ON concursos.modalidade_id = modalidades.id AND concursos.numero = stg_ganhadores_municipios.numero;

SELECT modalidades.codigo, count(*) AS concursos
FROM concursos
JOIN modalidades ON modalidades.id = concursos.modalidade_id
GROUP BY modalidades.codigo
ORDER BY modalidades.codigo;
"""
    (output_dir / "import.sql").write_text(sql.strip() + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    sqlite_path = args.sqlite.resolve()
    output_dir = args.out.resolve()

    if not sqlite_path.exists():
        raise FileNotFoundError(f"Banco SQLite nao encontrado: {sqlite_path}")

    output_dir.mkdir(parents=True, exist_ok=True)

    contests_path = output_dir / "concursos.csv"
    numbers_path = output_dir / "dezenas.csv"
    prizes_path = output_dir / "rateios.csv"
    cities_path = output_dir / "ganhadores_municipios.csv"

    totals = {"concursos": 0, "dezenas": 0, "rateios": 0, "ganhadores": 0}

    with sqlite3.connect(sqlite_path) as connection:
        connection.row_factory = sqlite3.Row
        with contests_path.open("w", newline="", encoding="utf-8") as contests_file, \
            numbers_path.open("w", newline="", encoding="utf-8") as numbers_file, \
            prizes_path.open("w", newline="", encoding="utf-8") as prizes_file, \
            cities_path.open("w", newline="", encoding="utf-8") as cities_file:

            contest_writer = csv.writer(contests_file, lineterminator="\n")
            number_writer = csv.writer(numbers_file, lineterminator="\n")
            prize_writer = csv.writer(prizes_file, lineterminator="\n")
            city_writer = csv.writer(cities_file, lineterminator="\n")

            contest_writer.writerow([
                "modalidade_codigo",
                "numero",
                "numero_concurso_anterior",
                "numero_concurso_proximo",
                "data_apuracao",
                "data_proximo_concurso",
                "local_sorteio",
                "municipio_uf_sorteio",
                "acumulado",
                "ultimo_concurso",
                "valor_arrecadado",
                "valor_estimado_proximo_concurso",
                "valor_acumulado_proximo_concurso",
                "resultado_especial",
                "result_json",
            ])
            number_writer.writerow(["modalidade_codigo", "numero", "tipo", "posicao", "valor", "valor_numero"])
            prize_writer.writerow(["modalidade_codigo", "numero", "faixa", "descricao_faixa", "numero_ganhadores", "valor_premio"])
            city_writer.writerow(["modalidade_codigo", "numero", "municipio", "uf", "ganhadores"])

            for table, (mode_code, expected_count) in TABLES.items():
                for row in connection.execute(f"SELECT * FROM {table} ORDER BY numeroConcurso"):
                    result = to_json_object(row_value(row, "result_json"))
                    contest_number = int(row["numeroConcurso"])
                    contest_writer.writerow([
                        mode_code,
                        contest_number,
                        result.get("numeroConcursoAnterior") or "",
                        result.get("numeroConcursoProximo") or "",
                        parse_date(result.get("dataApuracao")),
                        parse_date(result.get("dataProximoConcurso")),
                        first_text(result, "localSorteio"),
                        first_text(result, "nomeMunicipioUFSorteio"),
                        parse_bool(result.get("acumulado")),
                        parse_bool(result.get("ultimoConcurso")),
                        parse_money(result.get("valorArrecadado")),
                        parse_money(result.get("valorEstimadoProximoConcurso")),
                        parse_money(result.get("valorAcumuladoProximoConcurso")),
                        first_text(result, "nomeTimeCoracaoMesSorte", "tituloConcursoEspecial"),
                        json.dumps(result, ensure_ascii=False, separators=(",", ":")),
                    ])
                    totals["concursos"] += 1

                    number_sets = [("principal", collect_main_numbers(row, result, expected_count))]
                    number_sets.append(("ordem_sorteio", as_list(result.get("dezenasSorteadasOrdemSorteio"))))
                    number_sets.append(("segundo_sorteio", as_list(result.get("listaDezenasSegundoSorteio"))))
                    number_sets.append(("trevo", as_list(result.get("trevosSorteados"))))

                    for number_type, values in number_sets:
                        for position, value in enumerate(values, start=1):
                            text = str(value).strip()
                            text = clean_text(text)
                            if not text:
                                continue
                            number_writer.writerow([mode_code, contest_number, number_type, position, text, number_value(text)])
                            totals["dezenas"] += 1

                    for prize in as_list(result.get("listaRateioPremio")):
                        if not isinstance(prize, dict):
                            continue
                        prize_writer.writerow([
                            mode_code,
                            contest_number,
                            prize.get("faixa") or "",
                            first_text(prize, "descricaoFaixa"),
                            prize.get("numeroDeGanhadores") or 0,
                            parse_money(prize.get("valorPremio")) or "0",
                        ])
                        totals["rateios"] += 1

                    for city in as_list(result.get("listaMunicipioUFGanhadores")):
                        if not isinstance(city, dict):
                            continue
                        city_writer.writerow([
                            mode_code,
                            contest_number,
                            first_text(city, "municipio") or "Nao informado",
                            first_text(city, "uf") or "NA",
                            city.get("ganhadores") or city.get("numeroGanhadores") or 0,
                        ])
                        totals["ganhadores"] += 1

    write_import_sql(output_dir)

    print(json.dumps(totals, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
