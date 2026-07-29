$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $root "apps\api\src\LotoAnalytics.Api\LotoAnalytics.Api.csproj"

if (-not (Test-Path $project)) {
    throw "Missing API project: apps\api\src\LotoAnalytics.Api\LotoAnalytics.Api.csproj"
}

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()

$url = "http://127.0.0.1:$port"
$env:ASPNETCORE_URLS = $url
$env:ASPNETCORE_ENVIRONMENT = "Development"

$process = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", $project, "--no-launch-profile") -NoNewWindow -PassThru

try {
    $deadline = (Get-Date).AddSeconds(60)
    do {
        try {
            $health = Invoke-RestMethod -Uri "$url/health" -TimeoutSec 2
            if ($health.status -eq "ok") {
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    if ((Get-Date) -ge $deadline) {
        throw "API did not respond on $url/health before timeout."
    }

    $openApi = Invoke-RestMethod -Uri "$url/openapi/v1.json" -TimeoutSec 5
    if ($openApi.openapi -notlike "3.*") {
        throw "Unexpected OpenAPI version: $($openApi.openapi)"
    }

    if (-not $openApi.paths.PSObject.Properties.Name.Contains("/health")) {
        throw "OpenAPI document does not include /health."
    }

    $docs = Invoke-WebRequest -Uri "$url/docs" -TimeoutSec 5
    if ($docs.StatusCode -ne 200 -or $docs.Content -notmatch "Scalar") {
        throw "Scalar docs did not return the expected HTML."
    }

    Write-Output "api docs smoke tests passed"
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
