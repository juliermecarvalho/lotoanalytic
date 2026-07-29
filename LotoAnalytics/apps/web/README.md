# Web - LotoAnalytics

Frontend autenticado do LotoAnalytics em Vite, React e TypeScript.

## Estrutura

- `src/components/ui`: componentes base.
- `src/features`: funcionalidades por dominio.
- `src/lib`: cliente de API, autenticacao e formatadores.
- `tests`: testes unitarios e de componente com MSW para mocks da API.
- `e2e`: smoke tests Playwright.

## Telas Implementadas

- Dashboard estatistico da Lotofacil.
- Estatisticas detalhadas com paridade, soma, repetidas, linhas, colunas, moldura e miolo.
- Gerador de jogos com filtros iniciais e limite por plano aplicado pela API.
- Conferidor de jogos.
- Historicos de geracoes e conferencias com detalhe dos jogos.
- Exportacao CSV de geracoes premium via API.
- Perfil do usuario autenticado e plano atual.
- Modalidades ativas cadastradas no backend.
- Importacao manual de concursos oficiais por modalidade e numero.

## Comandos

```powershell
npm install
npm test
npm run e2e
npm run build
npm run dev
```

Configure a URL da API na barra superior da aplicacao. O token JWT e obtido pelo login OIDC com Keycloak.

## E2E

Os smoke tests usam Playwright e iniciam o Vite automaticamente em `http://127.0.0.1:5174`.

```powershell
npm run e2e
```

Na primeira execucao em uma maquina nova, instale o Chromium do Playwright:

```powershell
npx playwright install chromium
```

## Keycloak

O login usa OIDC Authorization Code + PKCE via Keycloak. Configure:

```env
VITE_KEYCLOAK_AUTHORITY=http://localhost:8080/realms/lotoanalytics
VITE_KEYCLOAK_CLIENT_ID=lotoanalytics-web
```

No Keycloak, o client web deve permitir `http://localhost:5174/auth/callback` como redirect URI durante desenvolvimento.
