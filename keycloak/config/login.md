# Login E Cadastro

Realm: `lotoanalytics`

Configuracoes esperadas:

- `registrationAllowed=true`
- `registrationEmailAsUsername=true`
- `resetPasswordAllowed=true`
- `verifyEmail=true`
- `internationalizationEnabled=true`
- `defaultLocale=pt-BR`
- `supportedLocales=[pt-BR]`

Essas opcoes sao aplicadas no bootstrap em `keycloak/import/lotoanalytics-realm.json` e reaplicadas no start por `Set-KeycloakLoginSettings` e `Set-KeycloakPortugueseLocale`.

O link `Esqueci minha senha` depende de `resetPasswordAllowed=true` e do SMTP configurado.
