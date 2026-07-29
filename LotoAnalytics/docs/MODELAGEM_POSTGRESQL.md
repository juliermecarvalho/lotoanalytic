# Modelagem PostgreSQL - LotoAnalytics

## Base analisada

Esta proposta foi criada a partir dos arquivos em `ultimos_resultados/`, exportados do campo `result_json` das modalidades:

- `lotofacil.json`
- `mega-sena.json`
- `quina.json`
- `maismilionaria.json`
- `lotomania.json`
- `timemania.json`
- `duplasena.json`
- `diadesorte.json`
- `supersete.json`

Os JSONs possuem uma estrutura principal comum, com diferencas por modalidade:

- `listaDezenas`: dezenas principais do concurso.
- `listaDezenasSegundoSorteio`: usada na Dupla Sena.
- `trevosSorteados`: usada na +Milionaria.
- `nomeTimeCoracaoMesSorte`: usado como mes da sorte no Dia de Sorte e time do coracao na Timemania.
- `listaRateioPremio`: faixas de premiacao, com quantidade de faixas variando por modalidade.
- `listaMunicipioUFGanhadores`: cidades dos ganhadores quando existem ganhadores na faixa principal.
- `dezenasSorteadasOrdemSorteio`: ordem real de sorteio, que pode ser diferente da lista ordenada.

## Principio Da Modelagem

A modelagem recomendada deve ser hibrida:

- Normalizar os dados usados pelo sistema: concursos, dezenas, rateios, ganhadores, estatisticas e importacoes.
- Manter o JSON bruto em `jsonb` para auditoria, reprocessamento e campos futuros da Caixa.
- Usar uma estrutura generica por modalidade em vez de criar uma tabela de resultado para cada loteria.

Essa abordagem permite comecar pela Lotofacil e evoluir para outras modalidades sem duplicar schema.

## Tabelas Principais

### `modalidades`

Cadastro das loterias suportadas.

```sql
CREATE TABLE modalidades (
    id BIGSERIAL PRIMARY KEY,
    codigo VARCHAR(40) NOT NULL UNIQUE,
    nome VARCHAR(80) NOT NULL,
    tipo_jogo_caixa VARCHAR(80) NOT NULL UNIQUE,
    numero_jogo_caixa INTEGER,
    quantidade_dezenas_principal INTEGER NOT NULL,
    quantidade_dezenas_segundo_sorteio INTEGER,
    possui_trevos BOOLEAN NOT NULL DEFAULT FALSE,
    possui_time_coracao BOOLEAN NOT NULL DEFAULT FALSE,
    possui_mes_sorte BOOLEAN NOT NULL DEFAULT FALSE,
    ativa BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Exemplos de `codigo`:

- `lotofacil`
- `mega_sena`
- `quina`
- `maismilionaria`
- `lotomania`
- `timemania`
- `dupla_sena`
- `dia_de_sorte`
- `super_sete`

### `concursos`

Representa o concurso oficial de uma modalidade.

```sql
CREATE TABLE concursos (
    id BIGSERIAL PRIMARY KEY,
    modalidade_id BIGINT NOT NULL REFERENCES modalidades(id),
    numero INTEGER NOT NULL,
    numero_concurso_anterior INTEGER,
    numero_concurso_proximo INTEGER,
    numero_concurso_final_0_5 INTEGER,
    data_apuracao DATE,
    data_proximo_concurso DATE,
    local_sorteio VARCHAR(160),
    municipio_uf_sorteio VARCHAR(160),
    acumulado BOOLEAN NOT NULL DEFAULT FALSE,
    ultimo_concurso BOOLEAN NOT NULL DEFAULT FALSE,
    indicador_concurso_especial INTEGER,
    titulo_concurso_especial VARCHAR(160),
    tipo_publicacao INTEGER,
    observacao TEXT,
    valor_arrecadado NUMERIC(14, 2),
    valor_estimado_proximo_concurso NUMERIC(14, 2),
    valor_acumulado_proximo_concurso NUMERIC(14, 2),
    valor_acumulado_concurso_especial NUMERIC(14, 2),
    valor_acumulado_concurso_0_5 NUMERIC(14, 2),
    valor_saldo_reserva_garantidora NUMERIC(14, 2),
    valor_total_premio_faixa_um NUMERIC(14, 2),
    resultado_especial VARCHAR(160),
    result_json JSONB NOT NULL,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (modalidade_id, numero)
);
```

Campo `resultado_especial` pode armazenar inicialmente:

- Mes da sorte do Dia de Sorte.
- Time do coracao da Timemania.

Se esses recursos crescerem, podem virar tabelas proprias depois.

### `concurso_dezenas`

Armazena as dezenas sorteadas de forma normalizada.

```sql
CREATE TABLE concurso_dezenas (
    id BIGSERIAL PRIMARY KEY,
    concurso_id BIGINT NOT NULL REFERENCES concursos(id) ON DELETE CASCADE,
    tipo VARCHAR(30) NOT NULL,
    posicao INTEGER NOT NULL,
    valor VARCHAR(4) NOT NULL,
    valor_numero INTEGER,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (concurso_id, tipo, posicao)
);
```

Valores sugeridos para `tipo`:

- `principal`: `listaDezenas`.
- `segundo_sorteio`: `listaDezenasSegundoSorteio`.
- `ordem_sorteio`: `dezenasSorteadasOrdemSorteio`.
- `trevo`: `trevosSorteados`.

Observacoes:

- `valor` deve ser texto para preservar dezenas como `00`, `01`, `09`.
- `valor_numero` ajuda em estatisticas numericas.
- Super Sete usa numeros por coluna; manter `posicao` e `valor` preserva o significado.

### `concurso_rateios`

Armazena as faixas de premio do concurso.

```sql
CREATE TABLE concurso_rateios (
    id BIGSERIAL PRIMARY KEY,
    concurso_id BIGINT NOT NULL REFERENCES concursos(id) ON DELETE CASCADE,
    faixa INTEGER NOT NULL,
    descricao_faixa VARCHAR(120) NOT NULL,
    numero_ganhadores INTEGER NOT NULL DEFAULT 0,
    valor_premio NUMERIC(14, 2) NOT NULL DEFAULT 0,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (concurso_id, faixa)
);
```

Essa tabela cobre Lotofacil, Mega-Sena, Quina, +Milionaria, Lotomania, Timemania, Dupla Sena, Dia de Sorte e Super Sete, mesmo com quantidades diferentes de faixas.

### `concurso_ganhadores_municipios`

Armazena cidades/UF de ganhadores retornadas pela API.

```sql
CREATE TABLE concurso_ganhadores_municipios (
    id BIGSERIAL PRIMARY KEY,
    concurso_id BIGINT NOT NULL REFERENCES concursos(id) ON DELETE CASCADE,
    posicao INTEGER,
    municipio VARCHAR(120),
    uf CHAR(2),
    ganhadores INTEGER NOT NULL DEFAULT 0,
    nome_fantasia_ul VARCHAR(160),
    serie VARCHAR(40),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Nos JSONs analisados, essa lista aparece vazia em algumas modalidades e preenchida em concursos com ganhadores principais.

## Estatisticas Calculadas

### `concurso_estatisticas`

Tabela para estatisticas derivadas usadas pelo dashboard e pelo gerador.

```sql
CREATE TABLE concurso_estatisticas (
    concurso_id BIGINT PRIMARY KEY REFERENCES concursos(id) ON DELETE CASCADE,
    quantidade_pares INTEGER NOT NULL DEFAULT 0,
    quantidade_impares INTEGER NOT NULL DEFAULT 0,
    soma_dezenas INTEGER NOT NULL DEFAULT 0,
    quantidade_repetidas_anterior INTEGER NOT NULL DEFAULT 0,
    dezenas_repetidas_anterior TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    quantidade_primos INTEGER NOT NULL DEFAULT 0,
    quantidade_moldura INTEGER NOT NULL DEFAULT 0,
    quantidade_miolo INTEGER NOT NULL DEFAULT 0,
    maior_sequencia INTEGER NOT NULL DEFAULT 0,
    distribuicao_linhas INTEGER[],
    distribuicao_colunas INTEGER[],
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Essa tabela deve ser preenchida para Lotofacil inicialmente. Para modalidades que nao usam a grade 5x5, campos como linhas, colunas, moldura e miolo podem ficar nulos.

## Importacao E Auditoria

### `importacoes_concursos`

Controle de execucoes da rotina de importacao.

```sql
CREATE TABLE importacoes_concursos (
    id BIGSERIAL PRIMARY KEY,
    modalidade_id BIGINT REFERENCES modalidades(id),
    inicio INTEGER,
    fim INTEGER,
    ultimo_concurso_processado INTEGER,
    quantidade_salvos INTEGER NOT NULL DEFAULT 0,
    quantidade_erros INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL,
    mensagem TEXT,
    iniciado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    finalizado_em TIMESTAMPTZ
);
```

### `importacao_concurso_erros`

Registro detalhado de falhas por concurso.

```sql
CREATE TABLE importacao_concurso_erros (
    id BIGSERIAL PRIMARY KEY,
    importacao_id BIGINT REFERENCES importacoes_concursos(id) ON DELETE CASCADE,
    modalidade_id BIGINT REFERENCES modalidades(id),
    numero_concurso INTEGER,
    codigo_erro VARCHAR(80),
    mensagem TEXT,
    payload JSONB,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

## Usuarios, Planos E Keycloak

### `usuarios`

Usuario da aplicacao sincronizado a partir do Keycloak.

```sql
CREATE TABLE usuarios (
    id BIGSERIAL PRIMARY KEY,
    keycloak_subject UUID NOT NULL UNIQUE,
    email VARCHAR(180),
    nome VARCHAR(180),
    plano_codigo VARCHAR(40) NOT NULL DEFAULT 'gratis',
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    ultimo_login_em TIMESTAMPTZ,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### `planos`

```sql
CREATE TABLE planos (
    id BIGSERIAL PRIMARY KEY,
    codigo VARCHAR(40) NOT NULL UNIQUE,
    nome VARCHAR(80) NOT NULL,
    descricao TEXT,
    limite_jogos_por_geracao INTEGER,
    permite_filtros_avancados BOOLEAN NOT NULL DEFAULT FALSE,
    permite_exportar_csv BOOLEAN NOT NULL DEFAULT FALSE,
    permite_exportar_pdf BOOLEAN NOT NULL DEFAULT FALSE,
    permite_historico_completo BOOLEAN NOT NULL DEFAULT FALSE,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

## Gerador De Jogos

### `geracoes_jogos`

```sql
CREATE TABLE geracoes_jogos (
    id BIGSERIAL PRIMARY KEY,
    usuario_id BIGINT REFERENCES usuarios(id),
    modalidade_id BIGINT NOT NULL REFERENCES modalidades(id),
    concurso_base_id BIGINT REFERENCES concursos(id),
    quantidade_jogos INTEGER NOT NULL,
    quantidade_dezenas INTEGER NOT NULL,
    filtros JSONB NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'concluida',
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### `jogos_gerados`

```sql
CREATE TABLE jogos_gerados (
    id BIGSERIAL PRIMARY KEY,
    geracao_id BIGINT NOT NULL REFERENCES geracoes_jogos(id) ON DELETE CASCADE,
    posicao INTEGER NOT NULL,
    dezenas TEXT[] NOT NULL,
    quantidade_pares INTEGER,
    quantidade_impares INTEGER,
    soma_dezenas INTEGER,
    quantidade_primos INTEGER,
    quantidade_moldura INTEGER,
    quantidade_miolo INTEGER,
    maior_sequencia INTEGER,
    favorito BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (geracao_id, posicao)
);
```

## Conferidor

### `conferencias`

```sql
CREATE TABLE conferencias (
    id BIGSERIAL PRIMARY KEY,
    usuario_id BIGINT REFERENCES usuarios(id),
    modalidade_id BIGINT NOT NULL REFERENCES modalidades(id),
    concurso_id BIGINT NOT NULL REFERENCES concursos(id),
    origem VARCHAR(30) NOT NULL,
    quantidade_jogos INTEGER NOT NULL DEFAULT 0,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### `jogos_conferidos`

```sql
CREATE TABLE jogos_conferidos (
    id BIGSERIAL PRIMARY KEY,
    conferencia_id BIGINT NOT NULL REFERENCES conferencias(id) ON DELETE CASCADE,
    posicao INTEGER NOT NULL,
    dezenas TEXT[] NOT NULL,
    acertos INTEGER NOT NULL,
    dezenas_acertadas TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    dezenas_nao_sorteadas TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (conferencia_id, posicao)
);
```

## Indices Recomendados

```sql
CREATE INDEX idx_concursos_modalidade_numero
    ON concursos (modalidade_id, numero DESC);

CREATE INDEX idx_concursos_data_apuracao
    ON concursos (data_apuracao DESC);

CREATE INDEX idx_concurso_dezenas_busca
    ON concurso_dezenas (tipo, valor);

CREATE INDEX idx_concurso_dezenas_concurso_tipo
    ON concurso_dezenas (concurso_id, tipo, posicao);

CREATE INDEX idx_concurso_rateios_concurso
    ON concurso_rateios (concurso_id, faixa);

CREATE INDEX idx_geracoes_usuario
    ON geracoes_jogos (usuario_id, criado_em DESC);

CREATE INDEX idx_conferencias_usuario
    ON conferencias (usuario_id, criado_em DESC);

CREATE INDEX idx_concursos_result_json_gin
    ON concursos USING GIN (result_json);
```

## Observacoes Por Modalidade

### Lotofacil

- `listaDezenas` tem 15 dezenas.
- Boa candidata para estatisticas completas: pares, impares, soma, primos, moldura, miolo, linhas, colunas e sequencias.
- `listaRateioPremio` possui faixas de 15 a 11 acertos.

### Mega-Sena

- `listaDezenas` tem 6 dezenas.
- Estatisticas numericas simples funcionam bem: frequencia, atraso, soma, pares/impares.
- Nao usar moldura/miolo da Lotofacil.

### Quina

- `listaDezenas` tem 5 dezenas.
- Estrutura parecida com Mega-Sena, com quatro faixas de rateio no JSON analisado.

### +Milionaria

- `listaDezenas` tem 6 dezenas.
- `trevosSorteados` tem 2 valores.
- `dezenasSorteadasOrdemSorteio` no JSON analisado inclui dezenas e trevos na mesma lista. Por isso, manter `tipo = 'trevo'` em `concurso_dezenas` e preservar o `result_json`.

### Lotomania

- `listaDezenas` tem 20 dezenas.
- Pode ter dezena `00`, entao o campo textual `valor` e obrigatorio.
- Estatisticas numericas devem tratar `00` como zero.

### Timemania

- `listaDezenas` tem 7 dezenas.
- `nomeTimeCoracaoMesSorte` representa o time do coracao.
- Pode ser armazenado inicialmente em `concursos.resultado_especial`.

### Dupla Sena

- `listaDezenas` tem 6 dezenas do primeiro sorteio.
- `listaDezenasSegundoSorteio` tem 6 dezenas do segundo sorteio.
- `dezenasSorteadasOrdemSorteio` no JSON analisado tem 12 valores.
- A tabela `concurso_dezenas` deve separar `principal`, `segundo_sorteio` e `ordem_sorteio`.

### Dia De Sorte

- `listaDezenas` tem 7 dezenas.
- `nomeTimeCoracaoMesSorte` representa o mes da sorte.
- Pode ser armazenado inicialmente em `concursos.resultado_especial`.

### Super Sete

- `listaDezenas` tem 7 valores, um por coluna.
- Como os valores podem ter apenas um digito, manter `posicao` e `valor` e nao assumir duas casas.

## Recomendacao Final

Para o LotoAnalytics, a primeira implementacao PostgreSQL deveria criar:

1. `modalidades`
2. `concursos`
3. `concurso_dezenas`
4. `concurso_rateios`
5. `concurso_ganhadores_municipios`
6. `concurso_estatisticas`
7. `importacoes_concursos`
8. `usuarios`
9. `planos`
10. `geracoes_jogos`
11. `jogos_gerados`
12. `conferencias`
13. `jogos_conferidos`

As demais tabelas podem entrar depois, conforme assinatura, administracao e comercializacao forem implementadas.
