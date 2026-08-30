$ErrorActionPreference = "Stop"

$repoRoot =
    (Resolve-Path (
        Join-Path $PSScriptRoot ".."
    )).Path

$acceptanceProject =
    Join-Path $repoRoot `
        "tests/Perimeter.Gateway.AcceptanceTests/Perimeter.Gateway.AcceptanceTests.csproj"

Push-Location $repoRoot

try {
    Write-Host "=== PDG v0.1 acceptance build ==="

    & dotnet build `
        "Perimeter.Gateway.sln" `
        -c Release

    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "=== PDG v0.1 acceptance suite ==="

    & dotnet run `
        --project $acceptanceProject `
        -c Release `
        --no-build `
        -- `
        -reporter verbose

    if ($LASTEXITCODE -ne 0) {
        throw "Acceptance suite failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "PDG v0.1 acceptance suite completed successfully."
}
finally {
    Pop-Location
}