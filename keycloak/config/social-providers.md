# Provedores Sociais

Realm: `lotoanalytics`

Provedores configurados:

- Google: alias `google`
- Facebook: alias `facebook`
- LinkedIn: alias `linkedin`
- Microsoft: alias `microsoft`

No ambiente local, os provedores ficam registrados com placeholders de credenciais no import do realm. As credenciais reais devem ser configuradas por variaveis de ambiente ou pelo console do Keycloak.

Politica atual:

- `trustEmail=false`

Com `trustEmail=false`, emails vindos de provedores sociais nao sao marcados como confiaveis automaticamente pelo Keycloak.

Callbacks locais:

- `http://localhost:8080/realms/lotoanalytics/broker/google/endpoint`
- `http://localhost:8080/realms/lotoanalytics/broker/facebook/endpoint`
- `http://localhost:8080/realms/lotoanalytics/broker/linkedin/endpoint`
- `http://localhost:8080/realms/lotoanalytics/broker/microsoft/endpoint`
