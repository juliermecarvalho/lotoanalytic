# AGENTS.md

## Arquitetura

- `ARCH`: vertical-slice.
- `API_STYLE`: controllers.
- `API_DOCS`: scalar.
- `AUTH`: keycloak.
- `ROUTER`: tanstack.
- `E2E`: playwright.
- `DOC_LANG`: pt-br.
- `DB_LANG`: pt-br.
- `CODE_LANG`: en.

## Estrutura

- `apps/landing`: landing page estatica.
- `apps/api`: API .NET 10.
- `apps/web`: frontend Vite + React + TypeScript.
- `docs`: plano e modelagem.
- `scripts`: automacoes locais.

## Convencoes

- Documentos, README, ADRs, comentarios explicativos e mensagens para usuario em PT-BR.
- Codigo, classes, metodos, variaveis, endpoints internos, DTOs e identificadores em ingles.
- Tabelas e colunas do banco em PT-BR, `snake_case`, sem acentos.
- Backend por feature em vertical slice, sem MediatR.
- Controllers com `[ApiController]` e DTOs de entrada com DataAnnotations.
- Cada metodo criado no backend deve ter comentario breve em PT-BR descrevendo sua responsabilidade.

## Comandos

```bash
make test
make test-scripts
```
