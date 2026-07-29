# Plano de Execucao - LotoAnalytics

## Visao Geral

O **LotoAnalytics** sera um sistema web para analise, geracao, conferencia e organizacao de jogos da Lotofacil, com arquitetura preparada para evoluir para outras modalidades de loteria.

O projeto deve sair do formato atual de script/prototipo e evoluir para um monorepo dentro da pasta `LotoAnalytics/`, com:

- Backend em .NET 10.
- Frontend em Vite + React + TypeScript.
- Banco PostgreSQL.
- Autenticacao via Keycloak.
- API em Controllers com `[ApiController]` e DataAnnotations.
- Documentacao de API com Scalar.
- Rotas frontend com TanStack Router.
- E2E com Playwright.
- Testes automatizados.
- CI separado por contexto de alteracao.
- Dockerfile por aplicacao.
- Documentacao tecnica e de produto no proprio repositorio.

> Posicionamento: sistema de analise estatistica para montar, filtrar, organizar e conferir jogos de loteria com mais criterio.

> Aviso obrigatorio: loteria envolve sorte. O LotoAnalytics nao garante premio, resultado ou aumento real de probabilidade de ganho.

## Parametros Arquiteturais Fixados

Esses parametros guiam o scaffold, a organizacao e as convencoes do sistema:

| Parametro | Valor | Decisao |
| --- | --- | --- |
| `ARCH` | `vertical-slice` | Organizar backend por feature, sem mediator. |
| `API_STYLE` | `controllers` | Usar Controllers com `[ApiController]` e DataAnnotations. |
| `API_DOCS` | `scalar` | Usar Scalar como UI moderna e interativa para documentacao da API. |
| `AUTH` | `keycloak` | Usar Keycloak OIDC self-hosted, assumindo realm existente. |
| `ROUTER` | `tanstack` | Usar TanStack Router para rotas type-safe no frontend. |
| `E2E` | `playwright` | Usar Playwright, com dev server iniciado automaticamente pelos testes. |
| `DOC_LANG` | `pt-br` | Documentacao em Portugues do Brasil. |
| `DB_LANG` | `pt-br` | Nomes de tabelas e colunas do banco em Portugues do Brasil. |
| `CODE_LANG` | `en` | Codigo e identificadores em ingles. |

Regras derivadas:

- A pasta do projeto deve ser derivada do nome do produto.
- O namespace raiz .NET deve ser derivado do nome em PascalCase: `LotoAnalytics`.
- O nome do banco deve ser derivado por slug PostgreSQL seguro: lowercase, hifens convertidos para underscores e caracteres invalidos removidos. Para este projeto: `lotoanalytics`.
- A funcao de scaffold deve usar a mesma regra de `slugify_db` prevista em `scripts/lib/common.sh`.
- Se a pasta alvo existir e nao estiver vazia, o scaffold deve abortar e sugerir `FORCE=1`.
- Documentos, README, ADRs, comentarios explicativos e mensagens de usuario devem ficar em PT-BR.
- Codigo, classes, metodos, variaveis, endpoints internos, DTOs e identificadores devem ficar em ingles.
- Cada metodo criado no backend deve ter comentario breve em PT-BR descrevendo sua responsabilidade.
- Comentarios devem explicar intencao do metodo, nao repetir linha a linha o que o codigo faz.

## Estrutura Alvo Do Projeto

```text
LotoAnalytics/
├── apps/
│   ├── landing/                # Landing page estatica atual
│   │   ├── index.html
│   │   ├── styles.css
│   │   ├── app.js
│   │   ├── assets/
│   │   ├── tests/
│   │   ├── PRODUCT.md
│   │   └── DESIGN.md
│   ├── api/
│   │   ├── src/
│   │   │   ├── Common/
│   │   │   ├── Infrastructure/
│   │   │   └── Features/
│   │   ├── tests/
│   │   ├── Dockerfile
│   │   └── README.md
│   └── web/
│       ├── src/
│       │   ├── components/
│       │   │   └── ui/
│       │   ├── features/
│       │   ├── lib/
│       │   ├── router.tsx
│       │   └── main.tsx
│       ├── tests/
│       ├── e2e/
│       ├── Dockerfile
│       ├── DESIGN_RULES.md
│       └── README.md
├── LotoAnalytics.slnx
├── .github/
│   └── workflows/
│       └── ci.yml
├── Directory.Build.props
├── Directory.Packages.props
├── Makefile
├── README.md
├── TDD.md
├── PRODUCT_OVERVIEW.md
├── AGENTS.md
├── CLAUDE.md
├── .gitignore
├── .editorconfig
└── docs/
    ├── MODELAGEM_POSTGRESQL.md
    └── PLANO_EXECUCAO.md
```

Observacao: a estrutura de backend sera **Vertical Slice sem mediator**. A alternativa em camadas (`Domain`, `Application`, `Infrastructure`, `Api`) nao deve ser usada neste projeto sem uma nova decisao formal.

## Arquitetura Do Monorepo

### `apps/landing`

Landing page estatica atual, mantida separada do futuro frontend autenticado.

Responsabilidades:

- Apresentar o LotoAnalytics comercialmente.
- Explicar proposta de valor, planos e aviso responsavel.
- Demonstrar um gerador simples.
- Servir como material de validacao e captacao enquanto o sistema web e construido.

Regras:

- Nao misturar codigo da landing com `apps/web`.
- Assets e testes da landing ficam dentro de `apps/landing`.
- Quando o produto web estiver pronto, a landing pode continuar como site publico ou ser migrada para uma rota publica do frontend, mediante nova decisao.

### `apps/api`

Backend .NET 10 responsavel por:

- API REST.
- Controllers com `[ApiController]`.
- Validacao de entrada com DataAnnotations.
- Documentacao interativa com Scalar.
- Autenticacao e autorizacao via Keycloak/JWT.
- Integracao com PostgreSQL.
- Importacao dos concursos oficiais.
- Normalizacao do `result_json`.
- Calculo de estatisticas.
- Geracao de jogos.
- Conferencia de jogos.
- Area administrativa.

Estrutura recomendada:

```text
apps/api/
├── src/
│   ├── Common/
│   │   ├── Auth/
│   │   ├── Errors/
│   │   ├── Results/
│   │   └── Time/
│   ├── Infrastructure/
│   │   ├── Database/
│   │   ├── ExternalServices/
│   │   ├── Keycloak/
│   │   ├── Migrations/
│   │   └── Jobs/
│   └── Features/
│       ├── Modalidades/
│       ├── Concursos/
│       ├── Estatisticas/
│       ├── Gerador/
│       ├── Conferidor/
│       ├── Usuarios/
│       ├── Planos/
│       └── Admin/
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Architecture/
├── Dockerfile
└── README.md
```

Testes do backend:

- xUnit v3.
- Shouldly.
- NSubstitute.
- Testcontainers para PostgreSQL e, quando necessario, Keycloak.

Regras:

- Nullable habilitado.
- Implicit usings habilitado.
- `LangVersion` 14.
- Warnings como erros.
- Central Package Management em `Directory.Packages.props`.
- Controllers devem usar `[ApiController]`.
- DTOs de entrada devem usar DataAnnotations para validacoes basicas.
- Nao usar MediatR ou mediator equivalente.
- Classes, metodos, propriedades, DTOs e namespaces devem estar em ingles.
- Cada metodo deve ter comentario breve em PT-BR descrevendo sua responsabilidade.
- Nomes do banco, migrations e colunas devem estar em PT-BR.
- Keycloak deve assumir um realm existente, sem provisionar realm automaticamente no primeiro MVP.

### `apps/web`

Frontend Vite + React + TypeScript responsavel por:

- Dashboard estatistico.
- Tela de concursos.
- Gerador de jogos.
- Conferidor.
- Historico do usuario.
- Favoritos.
- Configuracoes de filtros.
- Area administrativa.
- Integracao com Keycloak.
- Rotas type-safe com TanStack Router.

Estrutura recomendada:

```text
apps/web/
├── src/
│   ├── components/
│   │   └── ui/
│   ├── features/
│   │   ├── auth/
│   │   ├── concursos/
│   │   ├── estatisticas/
│   │   ├── gerador/
│   │   ├── conferidor/
│   │   ├── usuario/
│   │   └── admin/
│   ├── lib/
│   │   ├── api-client/
│   │   ├── auth/
│   │   └── formatters/
│   ├── routeTree.gen.ts
│   ├── router.tsx
│   └── main.tsx
├── tests/
├── e2e/
├── Dockerfile
├── DESIGN_RULES.md
└── README.md
```

Testes do frontend:

- Vitest.
- Testing Library.
- MSW para mocks da API.
- Playwright para smoke E2E.

Regras:

- TypeScript sem `any` salvo excecao documentada.
- Cliente tipado para a API.
- TanStack Router como roteador unico.
- Playwright deve subir o dev server automaticamente.
- Componentes base em `components/ui`, usando shadcn.
- Features isoladas por dominio.
- `DESIGN_RULES.md` deve documentar tokens, camadas visuais, formularios, tabelas, modais e padroes de estados.

## Funcionalidades Do Sistema

### 1. Atualizacao Automatica Dos Concursos

- Buscar resultados oficiais.
- Salvar historico no PostgreSQL.
- Salvar `result_json` bruto em `jsonb`.
- Normalizar dezenas, rateios e ganhadores.
- Calcular pares, impares, soma, repetidos do concurso anterior e estatisticas especificas da Lotofacil.
- Retomar importacao a partir do ultimo concurso salvo.
- Registrar execucoes e falhas de importacao.

### 2. Dashboard Estatistico

- Frequencia das dezenas.
- Dezenas mais sorteadas.
- Dezenas atrasadas.
- Distribuicao de pares e impares.
- Soma dos concursos.
- Repeticao em relacao ao concurso anterior.
- Sequencias.
- Linhas e colunas.
- Moldura e miolo.
- Filtros por janelas: geral, ultimos 10, 25, 50 e 100 concursos.

### 3. Gerador De Jogos

- Gerar jogos de 15 ou 16 dezenas.
- Aplicar filtros:
  - quantidade de pares e impares;
  - faixa de soma;
  - dezenas obrigatorias;
  - dezenas excluidas;
  - repetidas do ultimo concurso;
  - primos;
  - moldura e miolo;
  - linhas e colunas;
  - limite de sequencias.
- Evitar jogos duplicados.
- Salvar historico.
- Exportar CSV.
- Exportar PDF.
- Exportar arquivo para automacao quando aplicavel.

### 4. Conferidor De Jogos

- Colar jogos manualmente.
- Importar CSV.
- Comparar com resultado oficial.
- Mostrar jogos com 11, 12, 13, 14 e 15 acertos.
- Destacar dezenas acertadas.
- Gerar resumo do concurso.
- Exportar relatorio PDF.
- Gerar texto para WhatsApp/Telegram.

### 5. Integracao Com Keycloak

- Login.
- Logout.
- Refresh token.
- Validacao JWT no backend.
- Protecao de rotas no frontend.
- Papeis:
  - `usuario_gratis`
  - `usuario_premium`
  - `administrador`
- Sincronizacao do usuario autenticado na tabela `usuarios`.

### 6. Area Do Usuario

- Perfil.
- Historico de jogos gerados.
- Historico de conferencias.
- Jogos favoritos.
- Configuracoes favoritas de filtros.
- Plano atual.
- Limites por plano.

### 7. Administracao

- Gerenciar usuarios.
- Gerenciar planos.
- Ver estatisticas de uso.
- Ver concursos importados.
- Monitorar falhas de importacao.
- Publicar analises.
- Controlar assinaturas.
- Configurar limites por plano.

## Modelagem PostgreSQL

A modelagem deve seguir a proposta documentada em `docs/MODELAGEM_POSTGRESQL.md`, baseada na analise dos arquivos `result_json`.

Idioma do banco:

- Tabelas e colunas devem ficar em PT-BR.
- O banco fisico deve usar slug PostgreSQL seguro: `lotoanalytics`.
- Identificadores SQL devem preferir `snake_case`.
- Evitar nomes acentuados no SQL, mesmo mantendo PT-BR.

Tabelas iniciais:

- `modalidades`
- `concursos`
- `concurso_dezenas`
- `concurso_rateios`
- `concurso_ganhadores_municipios`
- `concurso_estatisticas`
- `importacoes_concursos`
- `importacao_concurso_erros`
- `usuarios`
- `planos`
- `geracoes_jogos`
- `jogos_gerados`
- `conferencias`
- `jogos_conferidos`

Decisoes importantes:

- Usar `jsonb` para preservar o `result_json` completo.
- Normalizar dezenas em tabela propria.
- Nao criar uma tabela por loteria.
- Separar dezenas por `tipo`: `principal`, `segundo_sorteio`, `ordem_sorteio`, `trevo`.
- Usar campos calculados em `concurso_estatisticas` para consultas rapidas do dashboard.

## CI E Qualidade

### `.github/workflows/ci.yml`

CI deve ser filtrado por paths:

- Mudancas em `apps/api/**` rodam build/testes da API.
- Mudancas em `apps/web/**` rodam typecheck/testes/build do frontend.
- Mudancas em arquivos globais rodam ambos.
- Um job agregado deve falhar se qualquer contexto obrigatorio falhar.

### Backend

Checks minimos:

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- testes de integracao com Testcontainers quando houver alteracao de persistencia.

### Frontend

Checks minimos:

- install com lockfile.
- typecheck.
- unit tests.
- build.
- smoke E2E quando rotas principais forem alteradas.
- Playwright deve iniciar o dev server automaticamente.

## Arquivos De Governanca

### `Directory.Build.props`

Deve centralizar:

- nullable.
- implicit usings.
- LangVersion 14.
- warnings-as-errors.
- analyzers padrao do projeto.

### `Directory.Packages.props`

Deve centralizar versoes dos pacotes .NET.

### `TDD.md`

Deve documentar:

- fluxo red-green-refactor.
- quando criar unit test.
- quando criar integration test.
- como usar Testcontainers.
- padroes de teste por feature.

### `AGENTS.md`

Contexto para agentes de codigo:

- arquitetura escolhida.
- comandos de build/test.
- limites de edicao.
- convencoes do projeto.
- onde ficam API, web, docs e testes.
- parametros arquiteturais fixados: `ARCH`, `API_STYLE`, `API_DOCS`, `AUTH`, `ROUTER`, `E2E`, `DOC_LANG`, `DB_LANG` e `CODE_LANG`.

### `CLAUDE.md`

Deve apontar para `AGENTS.md`, evitando duplicacao de regras.

### `PRODUCT_OVERVIEW.md`

Resumo em linguagem de negocio:

- publico-alvo.
- problema.
- proposta de valor.
- funcionalidades principais.
- riscos de comunicacao responsavel.

## Roadmap De Execucao

### Fase 1 - Reorganizacao Do Projeto

- [ ] Validar nome do projeto e derivar pasta, namespace raiz e nome do banco.
- [ ] Abortar scaffold se a pasta alvo existir e nao estiver vazia, sugerindo `FORCE=1`.
- [x] Separar landing page atual em `LotoAnalytics/apps/landing`.
- [x] Criar estrutura `LotoAnalytics/apps/api`.
- [x] Criar estrutura `LotoAnalytics/apps/web`.
- [x] Criar `LotoAnalytics/docs`.
- [x] Mover plano para `LotoAnalytics/docs/PLANO_EXECUCAO.md`.
- [x] Mover modelagem para `LotoAnalytics/docs/MODELAGEM_POSTGRESQL.md`.
- [x] Criar `README.md` global do monorepo.
- [x] Criar `PRODUCT_OVERVIEW.md`.
- [x] Criar `TDD.md`.
- [x] Criar `AGENTS.md`.
- [x] Criar `CLAUDE.md` apontando para `AGENTS.md`.
- [x] Criar `scripts/lib/common.sh` com `slugify_db`.

### Fase 2 - Fundacao .NET

- [x] Criar solution `LotoAnalytics.slnx`.
- [x] Criar projeto da API em .NET 10.
- [x] Adicionar projeto da API na solution.
- [x] Configurar `Directory.Build.props`.
- [x] Configurar `Directory.Packages.props`.
- [x] Criar estrutura Vertical Slice sem mediator em `apps/api/src`.
- [x] Criar Controllers com `[ApiController]`.
- [x] Configurar DataAnnotations nos DTOs de entrada.
- [x] Configurar Scalar para documentacao da API.
- [x] Configurar validacao JWT com Keycloak assumindo realm existente.
- [x] Criar projeto de testes em `apps/api/tests`.
- [x] Configurar xUnit v3, Shouldly, NSubstitute e Testcontainers.
- [x] Criar README da API.
- [ ] Documentar politica de comentarios: cada metodo deve ter breve comentario em PT-BR.

### Fase 3 - Fundacao Web

- [x] Criar app Vite + React + TypeScript em `apps/web`.
- [ ] Adicionar `.esproj` para carregamento no Visual Studio.
- [ ] Criar estrutura `components/ui`, `features`, `lib` e `router.tsx`.
- [ ] Configurar shadcn.
- [x] Configurar TanStack Router.
- [x] Configurar Vitest, Testing Library e MSW.
- [x] Configurar Playwright com dev server automatico.
- [x] Criar `DESIGN_RULES.md`.
- [x] Criar README do frontend.

### Fase 4 - Banco E Importacao

- [ ] Configurar PostgreSQL local.
- [x] Definir Docker Compose para ambiente local.
- [x] Implementar migrations iniciais.
- [x] Criar seed de modalidades.
- [x] Usar tabelas e colunas em PT-BR, com identificadores `snake_case` sem acentos.
- [x] Migrar importacao dos concursos para a API.
- [x] Persistir `result_json` em `jsonb`.
- [x] Normalizar dezenas, rateios e ganhadores.
- [ ] Criar logs de importacao.
- [x] Criar testes de integracao com PostgreSQL.

### Fase 5 - Estatisticas E Dashboard

- [x] Implementar calculo de estatisticas da Lotofacil.
- [x] Criar endpoints de estatisticas.
- [x] Criar dashboard web.
- [ ] Criar telas de frequencia e atraso historico.
- [x] Criar telas de paridade, soma, repetidos e grade.
- [x] Criar testes de dominio para estatisticas.
- [x] Criar testes de UI para dashboard.

### Fase 6 - Gerador

- [x] Implementar regras do gerador no backend.
- [x] Criar endpoint de geracao.
- [x] Salvar historico de geracoes.
- [x] Criar tela do gerador.
- [ ] Implementar filtros avancados.
- [x] Exportar CSV.
- [ ] Exportar PDF.
- [x] Testar duplicidade e limites por plano.

### Fase 7 - Conferidor

- [x] Criar endpoint de conferencia.
- [x] Permitir entrada manual.
- [ ] Permitir importacao CSV.
- [x] Calcular acertos.
- [x] Destacar 11, 12, 13, 14 e 15 pontos.
- [ ] Criar resumo para WhatsApp/Telegram.
- [ ] Exportar PDF.
- [x] Salvar historico de conferencias.

### Fase 8 - Autenticacao E Planos

- [ ] Configurar Keycloak.
- [x] Integrar frontend com Keycloak.
- [x] Validar JWT no backend.
- [x] Criar tabela `usuarios`.
- [x] Sincronizar usuario autenticado.
- [x] Criar tabela `planos`.
- [x] Criar tela de perfil do usuario e plano atual.
- [ ] Aplicar limites do plano gratis e premium.
- [ ] Criar protecao por papel de acesso.

### Fase 9 - Administracao

- [ ] Criar area administrativa.
- [ ] Listar usuarios.
- [ ] Gerenciar planos.
- [ ] Ver estatisticas de uso.
- [ ] Monitorar importacoes.
- [ ] Publicar analises.
- [ ] Controlar assinaturas.

### Fase 10 - CI, Docker E Publicacao

- [x] Criar Dockerfile da API.
- [x] Criar Dockerfile do frontend.
- [ ] Criar workflow CI path-filtered.
- [ ] Criar job agregado de qualidade.
- [ ] Documentar comandos no Makefile.
- [x] Validar build local completo.
- [ ] Preparar ambiente de publicacao.

## Regras De Acesso Inicial

| Recurso | Gratis | Premium | Administrador |
| --- | --- | --- | --- |
| Dashboard basico | Sim | Sim | Sim |
| Dashboard completo | Nao | Sim | Sim |
| Gerar poucos jogos | Sim | Sim | Sim |
| Geracao com filtros completos | Nao | Sim | Sim |
| Exportar CSV | Nao | Sim | Sim |
| Exportar PDF | Nao | Sim | Sim |
| Conferidor limitado | Sim | Sim | Sim |
| Historico completo | Nao | Sim | Sim |
| Administracao | Nao | Nao | Sim |

## Decisoes Do Projeto

| Data | Decisao | Motivo |
| --- | --- | --- |
| 2026-07-12 | Nome do projeto: LotoAnalytics | Nome permite evoluir para loterias e posiciona o produto como analise de dados. |
| 2026-07-12 | Comecar por Lotofacil | Ja existe base, regra de geracao e contexto do produto. |
| 2026-07-12 | Usar monorepo dentro de `LotoAnalytics/` | Mantem API, web, docs, CI e governanca no mesmo produto. |
| 2026-07-12 | Backend em .NET 10 | Alinha o sistema a uma API robusta, testavel e pronta para PostgreSQL/Keycloak. |
| 2026-07-12 | Frontend em Vite + React + TypeScript | Permite construir dashboard e ferramentas interativas com cliente tipado. |
| 2026-07-12 | PostgreSQL com `jsonb` | Preserva o JSON bruto da Caixa e permite normalizacao gradual. |
| 2026-07-12 | Keycloak para autenticacao | Evita senha propria e centraliza usuarios, sessoes e papeis. |
| 2026-07-12 | Vertical Slice sem mediator | Organiza por feature sem adicionar camada de despacho indireta. |
| 2026-07-12 | Controllers + DataAnnotations | Mantem a API explicita, simples de depurar e alinhada ao ASP.NET Core. |
| 2026-07-12 | Scalar para documentacao da API | Entrega UI moderna e interativa para explorar endpoints. |
| 2026-07-12 | TanStack Router | Garante rotas type-safe no frontend React. |
| 2026-07-12 | Playwright para E2E | Permite smoke tests com dev server automatico. |
| 2026-07-12 | Docs e banco em PT-BR; codigo em ingles | Mantem produto e dados legiveis para o dominio, sem fugir de convencoes tecnicas do codigo. |
| 2026-07-12 | Comunicacao responsavel | O sistema vende organizacao e analise, nao promessa de premio. |

## Proximas Acoes

- [ ] Criar `LotoAnalytics/docs`.
- [ ] Mover este plano para `LotoAnalytics/docs/PLANO_EXECUCAO.md`.
- [ ] Mover a modelagem para `LotoAnalytics/docs/MODELAGEM_POSTGRESQL.md`.
- [ ] Registrar os parametros fixos em `AGENTS.md`.
- [x] Criar solution `LotoAnalytics.slnx`.
- [ ] Criar esqueleto de `apps/api`.
- [ ] Criar esqueleto de `apps/web`.
- [x] Criar `scripts/lib/common.sh` com `slugify_db`.
- [ ] Criar arquivos globais: `Directory.Build.props`, `Directory.Packages.props`, `TDD.md`, `AGENTS.md`, `CLAUDE.md`, `.editorconfig` e Makefile.



