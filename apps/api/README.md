# API - LotoAnalytics

API .NET 10 do LotoAnalytics.

## Arquitetura

- Vertical Slice sem mediator.
- Controllers com `[ApiController]`.
- DTOs de entrada com DataAnnotations.
- Documentacao interativa com Scalar em `/docs` no ambiente de desenvolvimento.
- Contrato OpenAPI em `/openapi/v1.json` no ambiente de desenvolvimento.
- Autenticacao JWT via Keycloak, assumindo realm existente.

## Estrutura

- `src/Common`: componentes compartilhados.
- `src/Infrastructure`: banco, servicos externos, Keycloak, migrations e jobs.
- `src/Features`: funcionalidades por dominio.
- `tests`: testes unitarios, integracao e arquitetura.

## Endpoints Iniciais

- `GET /health`: smoke de disponibilidade.
- `GET /modalidades`: lista modalidades seedadas no PostgreSQL.
- `GET /usuarios/me`: retorna o usuario autenticado por JWT/Keycloak.
- `GET /openapi/v1.json`: contrato OpenAPI em desenvolvimento.
- `GET /docs`: UI Scalar em desenvolvimento.

## Banco

Configure `ConnectionStrings:DefaultConnection` para habilitar PostgreSQL. Quando configurada, a API aplica migrations pendentes no startup.

## Keycloak

Configure um realm existente no Keycloak e ajuste a secao `Keycloak`:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/lotoanalytics",
    "Audience": "lotoanalytics-api",
    "RequireHttpsMetadata": false,
    "UsernameClaim": "preferred_username"
  }
}
```

O backend valida JWT Bearer, usa `preferred_username` como nome do usuario e converte `realm_access.roles` para roles do ASP.NET Core.

## Comandos

```powershell
dotnet build ..\..\LotoAnalytics.slnx -nologo
pwsh -NoProfile -File ..\..\scripts\tests\api_smoke.test.ps1
pwsh -NoProfile -File ..\..\scripts\tests\api_docs_smoke.test.ps1
```

## Testes De Integracao

Os testes de integracao usam Testcontainers e exigem Docker ativo.

```powershell
dotnet test --project apps\api\tests\Integration\LotoAnalytics.Api.IntegrationTests.csproj --no-restore
```
