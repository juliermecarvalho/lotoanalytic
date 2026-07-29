# Configuracao Keycloak - LotoAnalytics

Esta pasta documenta a configuracao declarativa esperada para o Keycloak local.

## Fontes De Verdade

- Persistencia runtime: `bd/postgres-data`, via banco `lotoanalytics_keycloak`.
- Bootstrap de realm novo: `keycloak/import/lotoanalytics-realm.json`.
- Tema visual: `keycloak/themes/lotoanalytics`.
- Reaplicacao idempotente local: `lotoanalytics.start.ps1`.

Nao persistimos `/opt/keycloak/data` inteiro. O container Keycloak deve continuar descartavel; as configuracoes runtime ficam no PostgreSQL.

## Arquivos

- `login.md`: opcoes da tela de login e cadastro.
- `roles.md`: papeis de realm e regra local por usuario.
- `smtp.md`: SMTP local com Mailpit.
- `social-providers.md`: provedores sociais configurados.
- `local-users.md`: usuarios locais conhecidos no ambiente de desenvolvimento.
