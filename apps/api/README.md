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

## Caixa

A importacao de concursos consulta a API publica da Caixa. O CDN dela (Azion) responde **HTTP 403 para requisicoes vindas de fora do Brasil**, independentemente dos cabecalhos enviados. Servidores hospedados no exterior precisam de uma rota alternativa.

```json
{
  "Caixa": {
    "BaseUrl": "https://servicebus3.caixa.gov.br/portaldeloterias/api",
    "Proxy": {
      "Enabled": false,
      "Addresses": [],
      "Username": "",
      "Password": "",
      "TimeoutSeconds": 15,
      "FailureCooldownSeconds": 120
    }
  }
}
```

- `BaseUrl`: endereco base das consultas. Aponte para um relay hospedado no Brasil que repasse as chamadas para `servicebus3.caixa.gov.br`. O caminho `/{modalidade}/{numeroConcurso}` e acrescentado pela aplicacao.
- `Proxy.Addresses`: lista de proxies tentados em ordem. Aceita `http`, `https`, `socks4`, `socks4a` e `socks5`. `Address` continua aceito como atalho para um endereco unico.
- `Proxy.AddressList`: a mesma lista em uma unica string separada por virgula. E o formato usado no deploy, porque dezenas de chaves indexadas em variavel de ambiente sao impraticaveis.
- `Proxy.FailureCooldownSeconds`: tempo em que um proxy que falhou vai para o fim da fila. Ele nunca e descartado, apenas despriorizado.

Em producao as variaveis sao `Caixa__BaseUrl`, `Caixa__Proxy__Enabled` e `Caixa__Proxy__AddressList`, alimentadas pelo `.env` da raiz do repositorio (veja `.env.example`).

### Proxies publicos gratuitos

`scripts/find-caixa-proxies.sh` baixa listas publicas, testa cada endereco contra a API real da Caixa e imprime as linhas prontas para o `.env`:

```bash
bash scripts/find-caixa-proxies.sh 40 >> .env
```

So entra na lista o proxy que devolver o JSON de um concurso conhecido. Proxies anunciados como brasileiros sao testados primeiro e ficam no inicio da lista final.

Duas caracteristicas desses proxies moldam a configuracao:

- **Eles caem o tempo todo.** Em medicao feita a partir do servidor de producao, um endereco aprovado no teste tinha por volta de 25% de sucesso nas chamadas seguintes, e parte deles parava de responder em minutos. Por isso a lista deve ter varias dezenas de enderecos, e nao um so. Com failover, basta que um deles responda.
- **Eles nao conseguem adulterar os resultados.** A consulta e HTTPS, entao o proxy apenas encaminha um tunel `CONNECT` cifrado: o TLS e validado fim a fim contra o certificado da Caixa. Por isso a aplicacao recusa iniciar quando `Caixa:Proxy` esta habilitado com uma `BaseUrl` sem `https`.

A lista e lida no startup. Renove-a periodicamente rodando o script de novo e reiniciando o container.

### Tratamento de falhas

O 403 e tratado como falha definitiva (`CaixaAccessBlockedException`), nao como erro temporario: repetir a chamada nao resolve bloqueio de origem. Quando ha varias rotas, o 403 derruba apenas aquela rota e a proxima e tentada; o erro so sobe se todas forem bloqueadas. A modalidade entao e marcada como `falhou` e a importacao segue para a proxima. Erros realmente temporarios (429 e 5xx) continuam sendo repetidos ate `ContestUpdates:MaxRetryAttempts`.

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
