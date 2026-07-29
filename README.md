# LotoAnalytics

Sistema para analise, geracao, conferencia e organizacao de jogos da Lotofacil.

## Estrutura atual

```text
LotoAnalytics/
├── apps/
│   ├── landing/     # Landing page estatica atual
│   ├── api/         # API .NET 10
│   └── web/         # App React autenticado
├── docs/            # Plano e modelagem do sistema
├── scripts/         # Automacoes do monorepo
└── Makefile         # Comandos padrao de qualidade
```

## Subprojetos

- `apps/landing`: landing page publica/prototipo comercial.
- `apps/api`: API .NET 10 em vertical slice, Controllers e Scalar.
- `apps/web`: app Vite + React + TypeScript autenticado.

## Documentacao

- `docs/PLANO_EXECUCAO.md`: plano de execucao do monorepo e do sistema.
- `docs/MODELAGEM_POSTGRESQL.md`: proposta de modelagem PostgreSQL baseada nos `result_json`.
- `docs/KEYCLOAK_SOCIAL_PROVIDERS.md`: credenciais e callbacks para Google, Facebook, LinkedIn e Microsoft.
- `keycloak/config/README.md`: configuracao declarativa esperada para Keycloak local.

## Comandos

```bash
make test
make test-scripts
make test-api
make build-api
```

## Rodar A Aplicacao Local

O ambiente local usa Docker Compose para subir PostgreSQL, Keycloak, Mailpit, API e frontend.

No PowerShell, execute a partir da pasta `LotoAnalytics`:

```powershell
.\lotoanalytics.start.ps1
```

Servicos publicados:

- Frontend: `http://127.0.0.1:5174`
- API: `http://localhost:5291`
- API docs: `http://localhost:5291/docs`
- Keycloak: `http://localhost:8080`
- Mailpit: `http://localhost:8025`
- PostgreSQL: `localhost:5432`

Credenciais locais:

- Keycloak admin: `admin` / `admin`
- Usuario seed da aplicacao em realm novo: `dev` / `dev123`
- PostgreSQL: banco `lotoanalytics`, usuario `lotoanalytics`, senha `lotoanalytics_dev`

Emails de confirmacao do Keycloak sao capturados localmente pelo Mailpit. Ao cadastrar um usuario, abra `http://localhost:8025` para visualizar a mensagem e clicar no link de verificacao.

Dados locais do PostgreSQL ficam em `bd/postgres-data`. Essa pasta e um bind mount do container e preserva os bancos mesmo se containers e volumes Docker forem apagados. Backups manuais ficam em `bd/backups`.

Configuracoes declarativas do Keycloak ficam em `keycloak/config`. Dados runtime do Keycloak ficam no PostgreSQL, nao em uma pasta separada de runtime do container.

Papeis locais do Keycloak:

- Novos usuarios recebem `usuario_gratis`.
- `dev@lotoanalytics.local` recebe `usuario_premium`.
- `juliermecarvalho@gmail.com` recebe `administrador` e `usuario_premium`.

Parametros uteis:

```powershell
.\lotoanalytics.start.ps1 -NoBrowser
.\lotoanalytics.start.ps1 -NoBuild
```

Para parar o ambiente:

```powershell
.\lotoanalytics.stop.ps1
```

Para parar e apagar o volume do PostgreSQL:

```powershell
.\lotoanalytics.stop.ps1 -RemoveVolumes
```

## Importar Concursos Do SQLite Local

Para importar os concursos do banco `..\lotofacil.db` para o PostgreSQL local:

```powershell
.\scripts\import-local-sqlite-results.ps1
```

O script cria um backup em `bd/backups`, gera staging temporario em `bd/import`, copia os arquivos para o container `lotoanalytics-postgres` e faz upsert dos concursos sem duplicar registros.

## Atualizacao Automatica De Concursos

A API dispara uma atualizacao de concursos ao inicializar, sem bloquear a subida da aplicacao, e agenda nova execucao diaria as 01:00 no fuso de Sao Paulo.

Configuracoes:

```json
"ContestUpdates": {
  "Enabled": true,
  "RunOnStartup": true,
  "DailyRunAt": "01:00:00",
  "TimeZoneId": "America/Sao_Paulo",
  "DelayMilliseconds": 200,
  "ErrorDelayMilliseconds": 300000,
  "MaxRetryAttempts": 3
}
```

`DelayMilliseconds` equivale ao `--pausa` do Python. `ErrorDelayMilliseconds` equivale ao `--pausa-erro-api`; quando a Caixa retorna HTTP 403, 429, 500, timeout ou falha temporaria, o sistema aguarda esse tempo e tenta o mesmo concurso novamente. No Docker Compose essas configuracoes ficam em variaveis `ContestUpdates__...` do servico `api`.
