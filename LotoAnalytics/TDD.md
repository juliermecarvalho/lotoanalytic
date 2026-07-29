# TDD - LotoAnalytics

O projeto usa ciclos pequenos de red-green.

## Quando Criar Unit Test

- Regras puras de dominio, como estatisticas, filtros de geracao e conferencia.
- Funcoes publicas de automacao, como `slugify_db`.

## Quando Criar Integration Test

- Persistencia PostgreSQL.
- Endpoints HTTP da API.
- Autenticacao e autorizacao.

## Padrao

Cada teste deve observar um seam publico. Evite testar metodos privados, detalhes de implementacao ou consultas diretas quando houver uma API publica para verificar o comportamento.
