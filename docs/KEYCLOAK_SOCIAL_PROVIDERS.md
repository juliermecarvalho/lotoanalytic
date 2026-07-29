# Provedores Sociais Do Keycloak

O realm `lotoanalytics` ja declara provedores sociais para:

- Google
- Facebook
- LinkedIn
- Microsoft

## Variaveis De Ambiente

Configure as credenciais antes de subir o Keycloak em ambiente real:

```powershell
$env:GOOGLE_CLIENT_ID="..."
$env:GOOGLE_CLIENT_SECRET="..."
$env:FACEBOOK_CLIENT_ID="..."
$env:FACEBOOK_CLIENT_SECRET="..."
$env:LINKEDIN_CLIENT_ID="..."
$env:LINKEDIN_CLIENT_SECRET="..."
$env:MICROSOFT_CLIENT_ID="..."
$env:MICROSOFT_CLIENT_SECRET="..."
```

Sem essas credenciais reais, os botoes podem aparecer na tela, mas o login no provedor externo nao sera concluido.

## URLs De Callback

Cadastre estas URLs nos consoles dos provedores:

```text
http://localhost:8080/realms/lotoanalytics/broker/google/endpoint
http://localhost:8080/realms/lotoanalytics/broker/facebook/endpoint
http://localhost:8080/realms/lotoanalytics/broker/linkedin/endpoint
http://localhost:8080/realms/lotoanalytics/broker/microsoft/endpoint
```

Em producao, troque `http://localhost:8080` pelo dominio publico do Keycloak.
