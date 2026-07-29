# SMTP Local

O ambiente local usa Mailpit para capturar emails do Keycloak.

Servico:

- SMTP: `mailpit:1025`
- UI local: `http://localhost:8025`

Configuracao esperada no realm `lotoanalytics`:

- `host=mailpit`
- `port=1025`
- `from=noreply@lotoanalytics.local`
- `fromDisplayName=LotoAnalytics`
- `replyTo=suporte@lotoanalytics.local`
- `replyToDisplayName=Suporte LotoAnalytics`
- `ssl=false`
- `starttls=false`
- `auth=false`

Essa configuracao e reaplicada por `Set-KeycloakDevelopmentSmtp` no `lotoanalytics.start.ps1`.
