<#
.SYNOPSIS
    Reset the TaladPOS dev database to a fresh QA dataset.

.DESCRIPTION
    Starts the docker compose "postgres" service (unless -SkipDocker), waits
    until it accepts connections, then runs api/tools/TaladPOS.TestData, which
    wipes products, promotions, members and sales, copies the product images
    from docs/image/product into web/public/images/products, and rebuilds
    20 products, promotions, members and back-dated sales.
    Staff accounts are kept, so the manager/cashier logins keep working.

.EXAMPLE
    ./scripts/reset-test-data.ps1

.EXAMPLE
    ./scripts/reset-test-data.ps1 -SkipDocker -ImageBaseUrl http://192.168.1.10:3000
#>
[CmdletBinding()]
param(
    # Use when PostgreSQL is not the docker compose service (e.g. a local install).
    [switch]$SkipDocker,

    # Origin the web app is served from; product ImageUrl values are built from it.
    [string]$ImageBaseUrl = 'http://localhost:3000'
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'docker-compose.yml'

if (-not $SkipDocker) {
    Write-Host 'Starting PostgreSQL (docker compose service "postgres")...'
    docker compose -f $composeFile up -d postgres
    if ($LASTEXITCODE -ne 0) {
        throw 'docker compose up failed. If port 5432 is taken by another container, stop that container first (docker ps).'
    }

    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        docker compose -f $composeFile exec -T postgres pg_isready -U taladpos -d taladpos *> $null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 1
    }

    if (-not $ready) {
        throw 'PostgreSQL did not accept connections within 30 seconds.'
    }
}

dotnet run --project (Join-Path $repoRoot 'api/tools/TaladPOS.TestData') -- --image-base-url $ImageBaseUrl
if ($LASTEXITCODE -ne 0) {
    throw "TaladPOS.TestData failed with exit code $LASTEXITCODE."
}
