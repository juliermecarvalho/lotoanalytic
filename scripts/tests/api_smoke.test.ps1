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
    $deadline = (Get-Date).AddSeconds(45)
    do {
        try {
            $response = Invoke-RestMethod -Uri "$url/health" -TimeoutSec 2
            if ($response.status -ne "ok" -or $response.product -ne "LotoAnalytics") {
                throw "Unexpected health response: $($response | ConvertTo-Json -Compress)"
            }

            Write-Output "api smoke tests passed"
            exit 0
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "API did not respond on $url/health before timeout."
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
