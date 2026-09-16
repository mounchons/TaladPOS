<#
.SYNOPSIS
    Export the TaladPOS dev database to db/init/01-taladpos.sql (the teaching dataset).

.DESCRIPTION
    docker-compose.yml mounts db/init into the postgres container, so a fresh
    clone gets exactly this data on its first `docker compose up`. Run this
    after changing the data students should start from, then commit the file.

    pg_dump writes the file inside the container and it is copied out with
    `docker compose cp`, so PowerShell never re-encodes the Thai text.
    The \restrict / \unrestrict lines that pg_dump 16.10+ adds are removed:
    their token changes on every dump and older psql versions reject them.

.EXAMPLE
    ./scripts/dump-db.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $repoRoot 'db/init/01-taladpos.sql'
$containerFile = '/tmp/taladpos-dump.sql'

Push-Location $repoRoot
try {
    docker compose exec -T postgres pg_isready -U taladpos -d taladpos *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'PostgreSQL is not running. Start it first: docker compose up -d postgres'
    }

    docker compose exec -T postgres pg_dump -U taladpos -d taladpos --clean --if-exists --no-owner --no-privileges -f $containerFile
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }

    docker compose cp "postgres:$containerFile" $target
    if ($LASTEXITCODE -ne 0) { throw "docker compose cp failed with exit code $LASTEXITCODE." }

    docker compose exec -T postgres rm -f $containerFile

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $lines = [System.IO.File]::ReadAllLines($target, $utf8NoBom) |
        Where-Object { $_ -notmatch '^\\(un)?restrict ' }
    [System.IO.File]::WriteAllText($target, (($lines -join "`n") + "`n"), $utf8NoBom)
}
finally {
    Pop-Location
}

Write-Host "Wrote $target"
Write-Host 'Commit it so the next clone starts from this data.'
