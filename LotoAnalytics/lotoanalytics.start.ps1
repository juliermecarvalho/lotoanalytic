[CmdletBinding()]
param(
    [string]$WebUrl = "http://127.0.0.1:5174",
    [string]$ApiUrl = "http://localhost:5291",
    [string]$KeycloakUrl = "http://localhost:8080/realms/lotoanalytics",
    [string]$MailpitUrl = "http://localhost:8025",
    [string[]]$AdminUserEmails = @("juliermecarvalho@gmail.com"),
    [switch]$NoBuild,
    [switch]$NoBrowser,
    [switch]$StopConflicts
)

$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$ComposeFile = Join-Path $Root "docker-compose.yml"

# Portas publicadas pelo docker-compose.yml e o servico dono de cada uma.
$ComposePortMap = @(
    [pscustomobject]@{ Port = 5432; Service = "postgres" },
    [pscustomobject]@{ Port = 1025; Service = "mailpit" },
    [pscustomobject]@{ Port = 8025; Service = "mailpit" },
    [pscustomobject]@{ Port = 8080; Service = "keycloak" },
    [pscustomobject]@{ Port = 5291; Service = "api" },
    [pscustomobject]@{ Port = 5174; Service = "web" }
)

# Processos de infraestrutura do Docker que nunca devem ser encerrados pelo script.
$DockerProcessNamePattern = "^(com\.docker|docker|vpnkit|wslrelay|wslhost)"

function Test-CommandAvailable {
    param([string]$Name)

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-LotoAnalyticsCompose {
    param([string[]]$Arguments)

    & docker @("compose", "--file", $ComposeFile) @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose falhou com codigo $LASTEXITCODE."
    }
}

function Get-PortListenerProcess {
    param([int]$Port)

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $connection) {
        return $null
    }

    return Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
}

function Get-RunningComposeServices {
    $services = & docker @("compose", "--file", $ComposeFile, "ps", "--services", "--status", "running") 2>$null
    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    return @($services | Where-Object { $_ })
}

function Assert-ComposePortsAvailable {
    Write-Host "Verificando portas usadas pelo ambiente..."

    $runningServices = Get-RunningComposeServices
    $conflicts = @()

    foreach ($entry in $ComposePortMap) {
        if ($runningServices -contains $entry.Service) {
            # A porta pertence ao proprio container; o compose reaproveita ou recria.
            continue
        }

        $process = Get-PortListenerProcess -Port $entry.Port
        if ($null -eq $process) {
            continue
        }

        $conflicts += [pscustomobject]@{ Port = $entry.Port; Service = $entry.Service; Process = $process }
    }

    if ($conflicts.Count -eq 0) {
        Write-Host "Portas livres para o Docker Compose." -ForegroundColor Green
        return
    }

    foreach ($conflict in $conflicts) {
        Write-Host ("Porta {0} (servico {1}) ocupada por {2} (PID {3})." -f $conflict.Port, $conflict.Service, $conflict.Process.ProcessName, $conflict.Process.Id) -ForegroundColor Yellow
    }

    $dockerConflicts = @($conflicts | Where-Object { $_.Process.ProcessName -match $DockerProcessNamePattern })
    if ($dockerConflicts.Count -gt 0) {
        throw "Ha portas ocupadas pela infraestrutura do Docker sem o servico correspondente em execucao. Verifique containers de outros projetos com docker ps."
    }

    if (-not $StopConflicts) {
        throw "Portas em conflito com processos locais (ex.: vite dev ou dotnet run). Encerre-os ou execute novamente com -StopConflicts."
    }

    $conflictProcesses = @($conflicts | ForEach-Object { $_.Process } | Sort-Object Id -Unique)
    foreach ($process in $conflictProcesses) {
        Write-Host ("Encerrando {0} (PID {1}) para liberar as portas do ambiente..." -f $process.ProcessName, $process.Id)
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
    }

    Start-Sleep -Seconds 1
    Write-Host "Conflitos encerrados." -ForegroundColor Green
}

function Test-ApiEndpointsCurrent {
    # O endpoint de estatisticas de filtros existe apenas em builds atuais e sempre responde 200.
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$ApiUrl/estatisticas/lotofacil/filtros" -TimeoutSec 5
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Show-ContestBaseStatus {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$ApiUrl/concursos/lotofacil/ultimo" -TimeoutSec 5
        $latest = $response.Content | ConvertFrom-Json
        Write-Host ("Base de concursos carregada: concurso {0} com {1} sorteios." -f $latest.numeroConcurso, $latest.totalConcursos) -ForegroundColor Green
    }
    catch {
        Write-Host "Base de concursos ainda vazia: o atualizador automatico importa em background (a tela usa a base de exemplo ate concluir)." -ForegroundColor Yellow
    }
}

function Wait-HttpEndpoint {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return $true
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    return $false
}

function Invoke-KeycloakAdminCli {
    param([string[]]$Arguments)

    & docker @("exec", "lotoanalytics-keycloak", "/opt/keycloak/bin/kcadm.sh") @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "kcadm.sh falhou com codigo $LASTEXITCODE."
    }
}

function Set-KeycloakRealmPortugueseLocale {
    param([string]$Realm)

    Invoke-KeycloakAdminCli -Arguments @(
        "update",
        "realms/$Realm",
        "-s",
        "internationalizationEnabled=true",
        "-s",
        "defaultLocale=pt-BR",
        "-s",
        "supportedLocales=[`"pt-BR`"]"
    ) | Out-Null
}

function Set-KeycloakUsersPortugueseLocale {
    param([string]$Realm)

    $usersJson = Invoke-KeycloakAdminCli -Arguments @(
        "get",
        "users",
        "-r",
        $Realm,
        "--fields",
        "id,username",
        "-q",
        "max=1000"
    )
    $users = $usersJson | Out-String | ConvertFrom-Json

    foreach ($user in @($users)) {
        if ($null -eq $user.id) {
            continue
        }

        Invoke-KeycloakAdminCli -Arguments @(
            "update",
            "users/$($user.id)",
            "-r",
            $Realm,
            "-s",
            "attributes.locale=pt-BR"
        ) | Out-Null
    }
}

function Set-KeycloakDevelopmentSmtp {
    Write-Host "Configurando SMTP de desenvolvimento do Keycloak..."

    $smtpServerJson = @{
        host = "mailpit"
        port = "1025"
        from = "noreply@lotoanalytics.local"
        fromDisplayName = "LotoAnalytics"
        replyTo = "suporte@lotoanalytics.local"
        replyToDisplayName = "Suporte LotoAnalytics"
        ssl = "false"
        starttls = "false"
        auth = "false"
    } | ConvertTo-Json -Compress

    Invoke-KeycloakAdminCli -Arguments @(
        "update",
        "realms/lotoanalytics",
        "-s",
        "smtpServer=$smtpServerJson"
    ) | Out-Null

    Write-Host "SMTP de desenvolvimento configurado." -ForegroundColor Green
}

function Set-KeycloakLoginSettings {
    Write-Host "Configurando opcoes de login do Keycloak..."

    Invoke-KeycloakAdminCli -Arguments @(
        "update",
        "realms/lotoanalytics",
        "-s",
        "registrationAllowed=true",
        "-s",
        "registrationEmailAsUsername=true",
        "-s",
        "resetPasswordAllowed=true",
        "-s",
        "verifyEmail=true"
    ) | Out-Null

    Write-Host "Opcoes de login configuradas." -ForegroundColor Green
}

function Get-KeycloakRealmUsers {
    param([string]$Realm)

    $usersJson = Invoke-KeycloakAdminCli -Arguments @(
        "get",
        "users",
        "-r",
        $Realm,
        "--fields",
        "id,username,email",
        "-q",
        "max=1000"
    )

    return @($usersJson | Out-String | ConvertFrom-Json)
}

function Get-KeycloakUserRealmRoleNames {
    param(
        [string]$Realm,
        [string]$UserId
    )

    $rolesJson = Invoke-KeycloakAdminCli -Arguments @(
        "get",
        "users/$UserId/role-mappings/realm/composite",
        "-r",
        $Realm,
        "--fields",
        "name"
    )
    $roles = @($rolesJson | Out-String | ConvertFrom-Json)

    return @($roles | ForEach-Object { $_.name })
}

function Add-KeycloakUserRealmRole {
    param(
        [string]$Realm,
        [string]$UserId,
        [string]$RoleName
    )

    $roleNames = Get-KeycloakUserRealmRoleNames -Realm $Realm -UserId $UserId
    if ($roleNames -contains $RoleName) {
        return
    }

    Invoke-KeycloakAdminCli -Arguments @(
        "add-roles",
        "-r",
        $Realm,
        "--uid",
        $UserId,
        "--rolename",
        $RoleName
    ) | Out-Null
}

function Add-KeycloakDefaultRealmRole {
    param(
        [string]$Realm,
        [string]$RoleName
    )

    $defaultRolesJson = Invoke-KeycloakAdminCli -Arguments @(
        "get",
        "roles/default-roles-$Realm/composites",
        "-r",
        $Realm,
        "--fields",
        "name"
    )
    $defaultRoles = @($defaultRolesJson | Out-String | ConvertFrom-Json | ForEach-Object { $_.name })
    if ($defaultRoles -contains $RoleName) {
        return
    }

    Invoke-KeycloakAdminCli -Arguments @(
        "add-roles",
        "-r",
        $Realm,
        "--rname",
        "default-roles-$Realm",
        "--rolename",
        $RoleName
    ) | Out-Null
}

function Set-KeycloakApplicationRoles {
    Write-Host "Configurando papeis dos usuarios no Keycloak..."

    $users = Get-KeycloakRealmUsers -Realm "lotoanalytics"
    foreach ($user in $users) {
        if ($null -eq $user.id) {
            continue
        }

        $email = [string]$user.email
        $username = [string]$user.username
        if (($AdminUserEmails -contains $email) -or ($AdminUserEmails -contains $username)) {
            Add-KeycloakUserRealmRole -Realm "lotoanalytics" -UserId $user.id -RoleName "administrador"
        }
    }

    Write-Host "Papeis dos usuarios configurados." -ForegroundColor Green
}

function Set-KeycloakPortugueseLocale {
    Write-Host "Configurando Keycloak em PT-BR..."

    Invoke-KeycloakAdminCli -Arguments @(
        "config",
        "credentials",
        "--server",
        "http://localhost:8080",
        "--realm",
        "master",
        "--user",
        "admin",
        "--password",
        "admin"
    ) | Out-Null

    foreach ($realm in @("master", "lotoanalytics")) {
        Set-KeycloakRealmPortugueseLocale -Realm $realm
        Set-KeycloakUsersPortugueseLocale -Realm $realm
    }

    Write-Host "Keycloak configurado em PT-BR." -ForegroundColor Green
}

if (-not (Test-Path $ComposeFile)) {
    throw "docker-compose.yml nao encontrado em $ComposeFile"
}

if (-not (Test-CommandAvailable "docker")) {
    throw "Docker nao encontrado no PATH."
}

Write-Host "LotoAnalytics - subindo ambiente Docker Compose" -ForegroundColor Cyan
Write-Host "PostgreSQL: localhost:5432"
Write-Host "Keycloak: $KeycloakUrl"
Write-Host "Mailpit: $MailpitUrl"
Write-Host "API: $ApiUrl"
Write-Host "Web: $WebUrl"
Write-Host ""
Write-Host "Usuario local Keycloak: dev / dev123"
Write-Host "Admin Keycloak: admin / admin"
Write-Host ""

Assert-ComposePortsAvailable

$upArguments = @("up", "-d")
if (-not $NoBuild) {
    $upArguments += "--build"
}

Invoke-LotoAnalyticsCompose -Arguments $upArguments

Write-Host ""
Write-Host "Aguardando Keycloak responder..."
$keycloakReady = Wait-HttpEndpoint -Url $KeycloakUrl
Write-Host ($(if ($keycloakReady) { "Keycloak pronto." } else { "Keycloak ainda nao respondeu; use docker compose logs keycloak." })) -ForegroundColor ($(if ($keycloakReady) { "Green" } else { "Yellow" }))
if ($keycloakReady) {
    Set-KeycloakPortugueseLocale
    Set-KeycloakLoginSettings
    Set-KeycloakDevelopmentSmtp
    Set-KeycloakApplicationRoles
}

Write-Host "Aguardando Mailpit responder..."
$mailpitReady = Wait-HttpEndpoint -Url $MailpitUrl
Write-Host ($(if ($mailpitReady) { "Mailpit pronto." } else { "Mailpit ainda nao respondeu; use docker compose logs mailpit." })) -ForegroundColor ($(if ($mailpitReady) { "Green" } else { "Yellow" }))

Write-Host "Aguardando API responder..."
$apiReady = Wait-HttpEndpoint -Url "$ApiUrl/health"
Write-Host ($(if ($apiReady) { "API pronta." } else { "API ainda nao respondeu; use docker compose logs api." })) -ForegroundColor ($(if ($apiReady) { "Green" } else { "Yellow" }))
if ($apiReady) {
    if (Test-ApiEndpointsCurrent) {
        Write-Host "API com o build atual (endpoints de estatisticas disponiveis)." -ForegroundColor Green
        Show-ContestBaseStatus
    }
    else {
        Write-Host "API respondeu, mas sem os endpoints atuais: provavelmente um build antigo na porta 5291. Rode novamente sem -NoBuild e confira se nao ha um dotnet run local." -ForegroundColor Yellow
    }
}

Write-Host "Aguardando frontend responder..."
$webReady = Wait-HttpEndpoint -Url $WebUrl
Write-Host ($(if ($webReady) { "Frontend pronto." } else { "Frontend ainda nao respondeu; use docker compose logs web." })) -ForegroundColor ($(if ($webReady) { "Green" } else { "Yellow" }))

if (-not $NoBrowser -and $webReady) {
    Start-Process "$WebUrl/concursos/importar"
}

Write-Host ""
Write-Host "Para encerrar tudo: .\lotoanalytics.stop.ps1" -ForegroundColor Cyan
