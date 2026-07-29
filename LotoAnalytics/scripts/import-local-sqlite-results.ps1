[CmdletBinding()]
param(
    [string]$SqlitePath = "..\lotofacil.db",
    [string]$ContainerName = "lotoanalytics-postgres",
    [string]$Database = "lotoanalytics",
    [string]$User = "lotoanalytics",
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

function Resolve-ProjectPath {
    param([string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -ne $resolved) {
        return $resolved.Path
    }

    throw "Caminho nao encontrado: $Path"
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$sqliteFullPath = Resolve-ProjectPath -Path (Join-Path $projectRoot $SqlitePath)
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$importRoot = Join-Path $projectRoot "bd\import\local-sqlite\$timestamp"
$backupRoot = Join-Path $projectRoot "bd\backups"
$pythonScript = Join-Path $PSScriptRoot "import-local-sqlite-results.py"

New-Item -ItemType Directory -Force -Path $importRoot | Out-Null
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

Write-Host "LotoAnalytics - gerando staging do SQLite local"
Write-Host "SQLite: $sqliteFullPath"
python $pythonScript --sqlite $sqliteFullPath --out $importRoot

if (-not $SkipBackup) {
    $backupPath = Join-Path $backupRoot "pre-local-sqlite-import-$timestamp.sql"
    Write-Host "Criando backup em $backupPath"
    docker exec $ContainerName pg_dump -U $User -d $Database --clean --if-exists | Out-File -FilePath $backupPath -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw "Falha ao criar backup do PostgreSQL." }
}

Write-Host "Copiando staging para o container $ContainerName"
docker exec $ContainerName sh -c "rm -rf /tmp/lotoanalytics-import && mkdir -p /tmp/lotoanalytics-import"
if ($LASTEXITCODE -ne 0) { throw "Falha ao preparar a pasta temporaria no container." }
docker cp "$importRoot\." "${ContainerName}:/tmp/lotoanalytics-import"
if ($LASTEXITCODE -ne 0) { throw "Falha ao copiar staging para o container." }

Write-Host "Importando concursos no PostgreSQL"
docker exec $ContainerName psql -U $User -d $Database -f /tmp/lotoanalytics-import/import.sql
if ($LASTEXITCODE -ne 0) { throw "Falha ao importar concursos no PostgreSQL." }

Write-Host "Importacao concluida."
