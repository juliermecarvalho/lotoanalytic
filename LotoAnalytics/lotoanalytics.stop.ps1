[CmdletBinding()]
param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$ComposeFile = Join-Path $Root "docker-compose.yml"

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

if (-not (Test-Path $ComposeFile)) {
    throw "docker-compose.yml nao encontrado em $ComposeFile"
}

if (-not (Test-CommandAvailable "docker")) {
    throw "Docker nao encontrado no PATH."
}

$downArguments = @("down")
if ($RemoveVolumes) {
    $downArguments += "--volumes"
}

Write-Host "LotoAnalytics - parando ambiente Docker Compose" -ForegroundColor Cyan
Invoke-LotoAnalyticsCompose -Arguments $downArguments
Write-Host "Ambiente parado." -ForegroundColor Green
Write-Host "Dados PostgreSQL locais preservados em: $(Join-Path $Root 'bd\\postgres-data')" -ForegroundColor Cyan
