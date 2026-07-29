# Usuarios Locais

Os usuarios runtime ficam persistidos no banco `lotoanalytics_keycloak`, dentro de `bd/postgres-data`.

Usuarios conhecidos no ambiente local:

- Admin do realm `master`: `admin` / `admin`
- Usuario da aplicacao cadastrado: `juliermecarvalho@gmail.com`

Historicamente o README cita `dev` / `dev123`, mas esse usuario pode nao existir se o realm ja foi criado antes do import atual ou se os dados persistidos foram alterados pelo console.

Para listar usuarios:

```powershell
docker exec lotoanalytics-keycloak /opt/keycloak/bin/kcadm.sh get users -r lotoanalytics --fields username,email -q max=1000
```

Para ver usuarios no console:

```text
Realm atual: lotoanalytics
Administracao > Usuarios
```
