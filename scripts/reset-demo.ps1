$ErrorActionPreference = "Stop"

$repoRoot =
    (Resolve-Path (
        Join-Path $PSScriptRoot ".."
    )).Path

Push-Location $repoRoot

try {
    Write-Host "PDG v0.1 destructive demo reset"
    Write-Host "Stopping Docker Compose and deleting demo volumes..."

    & docker compose down -v --remove-orphans

    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose reset failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "PDG demo environment and demo volumes were removed successfully."
}
finally {
    Pop-Location
}