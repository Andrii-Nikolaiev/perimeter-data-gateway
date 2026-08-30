param(
    [int]$WarmupCount = 10,
    [int]$MeasuredCount = 100
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($WarmupCount -lt 10) {
    throw "WarmupCount must be at least 10 according to TWP Section 18."
}

if ($MeasuredCount -lt 100) {
    throw "MeasuredCount must be at least 100 according to TWP Section 18."
}

$repositoryRoot =
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Set-Location $repositoryRoot

$envFile =
    Join-Path $repositoryRoot ".env"

if (-not (Test-Path $envFile)) {
    throw ".env was not found in the repository root."
}

function Get-DotEnvValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $prefix = "$Name="

    $line =
        Get-Content $envFile |
        Where-Object {
            $_.StartsWith(
                $prefix,
                [StringComparison]::Ordinal)
        } |
        Select-Object -First 1

    if ($null -eq $line) {
        throw "$Name was not found in .env."
    }

    $value =
        $line.Substring($prefix.Length).Trim()

    if (
        $value.Length -ge 2 -and
        (
            (
                $value.StartsWith('"') -and
                $value.EndsWith('"')
            ) -or
            (
                $value.StartsWith("'") -and
                $value.EndsWith("'")
            )
        )
    ) {
        $value =
            $value.Substring(
                1,
                $value.Length - 2)
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Name has no value in .env."
    }

    return $value
}

function ConvertTo-Base64Url {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]]$Bytes
    )

    return (
        [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
    )
}

function New-DemoJwt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SigningKey
    )

    $now =
        [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

    $headerJson =
        @{
            alg = "HS256"
            typ = "JWT"
        } |
        ConvertTo-Json -Compress

    $payloadJson =
        @{
            sub   = "user_43"
            act   = @{
                sub = "sales_copilot_v1"
            }
            scope = "sales.read"
            iss   = "https://pdg.local/test-issuer"
            aud   = "pdg-api"
            nbf   = $now
            exp   = $now + 1800
        } |
        ConvertTo-Json -Compress -Depth 4

    $header =
        ConvertTo-Base64Url (
            [Text.Encoding]::UTF8.GetBytes(
                $headerJson)
        )

    $payload =
        ConvertTo-Base64Url (
            [Text.Encoding]::UTF8.GetBytes(
                $payloadJson)
        )

    $unsignedToken =
        "$header.$payload"

    $keyBytes =
        [Text.Encoding]::UTF8.GetBytes(
            $SigningKey)

    $hmac =
        [Security.Cryptography.HMACSHA256]::new(
            $keyBytes)

    try {
        $signatureBytes =
            $hmac.ComputeHash(
                [Text.Encoding]::ASCII.GetBytes(
                    $unsignedToken))

        $signature =
            ConvertTo-Base64Url $signatureBytes
    }
    finally {
        $hmac.Dispose()
    }

    return "$unsignedToken.$signature"
}

function Get-Median {
    param(
        [Parameter(Mandatory = $true)]
        [double[]]$Values
    )

    $sorted =
        @($Values | Sort-Object)

    $count =
        $sorted.Count

    if ($count -eq 0) {
        throw "Cannot calculate median of an empty sample."
    }

    if (($count % 2) -eq 1) {
        return [double]$sorted[
            [int][Math]::Floor($count / 2)
        ]
    }

    $upper =
        [int]($count / 2)

    $lower =
        $upper - 1

    return (
        ([double]$sorted[$lower] +
         [double]$sorted[$upper]) / 2.0
    )
}

function Get-Percentile {
    param(
        [Parameter(Mandatory = $true)]
        [double[]]$Values,

        [Parameter(Mandatory = $true)]
        [double]$Percentile
    )

    if (
        $Percentile -le 0.0 -or
        $Percentile -gt 1.0
    ) {
        throw "Percentile must be > 0 and <= 1."
    }

    $sorted =
        @($Values | Sort-Object)

    if ($sorted.Count -eq 0) {
        throw "Cannot calculate percentile of an empty sample."
    }

    $index =
        [int](
            [Math]::Ceiling(
                $Percentile *
                $sorted.Count) - 1
        )

    if ($index -lt 0) {
        $index = 0
    }

    return [double]$sorted[$index]
}

function Invoke-PdgRequest {
    param(
        [Parameter(Mandatory = $true)]
        [System.Net.Http.HttpClient]$Client,

        [Parameter(Mandatory = $true)]
        [string]$Token,

        [Parameter(Mandatory = $true)]
        [string]$Uri
    )

    $request =
        [System.Net.Http.HttpRequestMessage]::new(
            [System.Net.Http.HttpMethod]::Get,
            $Uri)

    $request.Headers.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new(
            "Bearer",
            $Token)

    try {
        $response =
            $Client.SendAsync($request).GetAwaiter().GetResult()

        try {
            $body =
                $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()

            if (-not $response.IsSuccessStatusCode) {
                throw (
                    "PDG request failed with HTTP " +
                    [int]$response.StatusCode
                )
            }

            return $body.Length
        }
        finally {
            $response.Dispose()
        }
    }
    finally {
        $request.Dispose()
    }
}

Add-Type -AssemblyName System.Net.Http

$pdgReaderPassword =
    Get-DotEnvValue "PDG_READER_PASSWORD"

$jwtSigningKey =
    Get-DotEnvValue "JWT_SIGNING_KEY"

$apiUri =
    "http://127.0.0.1:8080/api/resources/SalesSummary"

$readyUri =
    "http://127.0.0.1:8080/health/ready"

Write-Host "Checking PDG readiness..."

$readinessClient =
    [System.Net.Http.HttpClient]::new()

try {
    $readyResponse =
        $readinessClient.GetAsync($readyUri).GetAwaiter().GetResult()

    try {
        if (-not $readyResponse.IsSuccessStatusCode) {
            throw (
                "PDG readiness check failed with HTTP " +
                [int]$readyResponse.StatusCode
            )
        }
    }
    finally {
        $readyResponse.Dispose()
    }
}
finally {
    $readinessClient.Dispose()
}

Write-Host "Collecting environment information..."

$cpu =
    (
        Get-CimInstance Win32_Processor |
        Select-Object -ExpandProperty Name
    ) -join "; "

$totalMemoryBytes =
    (
        Get-CimInstance Win32_ComputerSystem
    ).TotalPhysicalMemory

$totalMemoryGb =
    [Math]::Round(
        $totalMemoryBytes / 1GB,
        2)

$os =
    (
        Get-CimInstance Win32_OperatingSystem
    ).Caption

$dockerVersion =
    (
        & docker version `
            --format '{{.Server.Version}}' 2>&1 |
        Out-String
    ).Trim()

if ($LASTEXITCODE -ne 0) {
    throw "Unable to determine Docker version."
}

$dotnetVersion =
    (
        & dotnet --version 2>&1 |
        Out-String
    ).Trim()

if ($LASTEXITCODE -ne 0) {
    throw "Unable to determine .NET SDK version."
}

$datasetFile =
    Join-Path `
        $repositoryRoot `
        "db/chinook/10-chinook-1.4.5.sql"

$datasetSha256 =
    (
        Get-FileHash `
            -Algorithm SHA256 `
            $datasetFile
    ).Hash.ToLowerInvariant()

$previousPgPassword =
    $env:PGPASSWORD

$env:PGPASSWORD =
    $pdgReaderPassword

try {
    $postgresVersion =
        (
            & docker compose exec `
                -T `
                -e PGPASSWORD `
                chinook-db `
                psql `
                -U pdg_reader `
                -d chinook `
                -Atc `
                "SHOW server_version;" 2>&1 |
            Out-String
        ).Trim()

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to determine PostgreSQL version."
    }

    $datasetRowsText =
        (
            & docker compose exec `
                -T `
                -e PGPASSWORD `
                chinook-db `
                psql `
                -U pdg_reader `
                -d chinook `
                -Atc `
                "SELECT count(*) FROM pdg.sales_summary;" 2>&1 |
            Out-String
        ).Trim()

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read pdg.sales_summary as pdg_reader."
    }

    $datasetRows =
        [int]$datasetRowsText

    Write-Host "Running direct database baseline..."

    $directQuery = @'
SELECT
    "CustomerId",
    "Country",
    "InvoiceDate",
    "Total"
FROM pdg.sales_summary
ORDER BY "InvoiceDate", "CustomerId"
LIMIT 500;
'@

    $sqlLines =
        [System.Collections.Generic.List[string]]::new()

    $sqlLines.Add('\timing on')
    $sqlLines.Add('\o /dev/null')

    $totalIterations =
        $WarmupCount + $MeasuredCount

    for (
        $i = 0;
        $i -lt $totalIterations;
        $i++
    ) {
        $sqlLines.Add($directQuery)
    }

    $sqlLines.Add('\o')

    $sqlScript =
        ($sqlLines -join [Environment]::NewLine) +
        [Environment]::NewLine

    $baselineOutput =
        (
            $sqlScript |
            & docker compose exec `
                -T `
                -e PGPASSWORD `
                -e LC_ALL=C `
                chinook-db `
                psql `
                -X `
                -q `
                -v ON_ERROR_STOP=1 `
                -U pdg_reader `
                -d chinook 2>&1 |
            Out-String
        )

    if ($LASTEXITCODE -ne 0) {
        throw (
            "Direct database performance run failed.`n" +
            $baselineOutput
        )
    }

    $timingMatches =
        [regex]::Matches(
            $baselineOutput,
            'Time:\s+([0-9]+(?:\.[0-9]+)?)\s+ms')

    if (
        $timingMatches.Count -lt
        $totalIterations
    ) {
        throw (
            "Expected at least $totalIterations " +
            "database timings, but received " +
            $timingMatches.Count + "."
        )
    }

    $allBaselineTimings =
        [System.Collections.Generic.List[double]]::new()

    foreach ($match in $timingMatches) {
        $allBaselineTimings.Add(
            [double]::Parse(
                $match.Groups[1].Value,
                [Globalization.CultureInfo]::InvariantCulture))
    }

    $baselineMeasured =
        [double[]](
            $allBaselineTimings |
            Select-Object `
                -Skip $WarmupCount `
                -First $MeasuredCount
        )
}
finally {
    if ($null -eq $previousPgPassword) {
        Remove-Item `
            Env:PGPASSWORD `
            -ErrorAction SilentlyContinue
    }
    else {
        $env:PGPASSWORD =
            $previousPgPassword
    }
}

Write-Host "Running PDG HTTP comparison..."

$token =
    New-DemoJwt `
        -SigningKey $jwtSigningKey

$httpClient =
    [System.Net.Http.HttpClient]::new()

$httpClient.Timeout =
    [TimeSpan]::FromSeconds(30)

try {
    for (
        $i = 0;
        $i -lt $WarmupCount;
        $i++
    ) {
        [void](
            Invoke-PdgRequest `
                -Client $httpClient `
                -Token $token `
                -Uri $apiUri
        )
    }

    $pdgMeasured =
        [System.Collections.Generic.List[double]]::new()

    for (
        $i = 0;
        $i -lt $MeasuredCount;
        $i++
    ) {
        $stopwatch =
            [Diagnostics.Stopwatch]::StartNew()

        [void](
            Invoke-PdgRequest `
                -Client $httpClient `
                -Token $token `
                -Uri $apiUri
        )

        $stopwatch.Stop()

        $pdgMeasured.Add(
            $stopwatch.Elapsed.TotalMilliseconds)
    }
}
finally {
    $httpClient.Dispose()
}

$baselineValues =
    [double[]]$baselineMeasured

$pdgValues =
    [double[]]$pdgMeasured.ToArray()

$baselineMedian =
    Get-Median $baselineValues

$baselineP95 =
    Get-Percentile `
        -Values $baselineValues `
        -Percentile 0.95

$pdgMedian =
    Get-Median $pdgValues

$pdgP95 =
    Get-Percentile `
        -Values $pdgValues `
        -Percentile 0.95

$medianOverheadMs =
    $pdgMedian - $baselineMedian

$p95OverheadMs =
    $pdgP95 - $baselineP95

if ($baselineMedian -gt 0) {
    $medianOverheadPercent =
        (($pdgMedian / $baselineMedian) - 1.0) *
        100.0
}
else {
    $medianOverheadPercent =
        [double]::NaN
}

$invariant =
    [Globalization.CultureInfo]::InvariantCulture

function Format-Milliseconds {
    param([double]$Value)

    return $Value.ToString(
        "F3",
        $invariant)
}

function Format-Percent {
    param([double]$Value)

    if ([double]::IsNaN($Value)) {
        return "n/a"
    }

    return (
        $Value.ToString(
            "F2",
            $invariant) + "%"
    )
}

$reportPath =
    Join-Path `
        $repositoryRoot `
        "docs/performance-report-v0.1.md"

$measuredAtUtc =
    [DateTimeOffset]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")

$report = @"
# Perimeter Data Gateway v0.1 - Performance Report

## Measurement status

Performance measurement performed according to TWP Section 18.

The TWP defines no SLA threshold. These measurements are observational and are not a pass/fail SLA acceptance gate.

## Environment

- Measurement time (UTC): $measuredAtUtc
- CPU: $cpu
- RAM: $totalMemoryGb GB
- Operating system: $os
- Docker Server: $dockerVersion
- PostgreSQL: $postgresVersion
- .NET SDK: $dotnetVersion

## Dataset

- Dataset: Chinook 1.4.5
- Source file: ``db/chinook/10-chinook-1.4.5.sql``
- SHA256: ``$datasetSha256``
- ``pdg.sales_summary`` rows: $datasetRows

## Method

- Logical operation: read ``SalesSummary`` with GlobalAnalyst demo Subject ``user_43``.
- Actor: ``sales_copilot_v1``.
- Capability: ``sales.read``.
- Result limit: 500 rows.
- Concurrency: 1.
- Warm-up iterations: $WarmupCount.
- Measured iterations: $MeasuredCount.
- Baseline: direct fixed query to ``pdg.sales_summary`` under ``pdg_reader``.
- Comparison: equivalent authenticated HTTP request through PDG.
- Direct database timing is collected in one persistent ``psql`` session using ``\timing``.
- PDG timing is end-to-end client elapsed time including authentication, authorization, Platform Store access, Corporate Data Source read, mandatory audit persistence, serialization, and local HTTP transport.

## Results

### Direct database baseline

- Samples: $MeasuredCount
- Median: $(Format-Milliseconds $baselineMedian) ms
- p95: $(Format-Milliseconds $baselineP95) ms

### PDG request

- Samples: $MeasuredCount
- Median: $(Format-Milliseconds $pdgMedian) ms
- p95: $(Format-Milliseconds $pdgP95) ms

### Observed PDG overhead

- Median absolute overhead: $(Format-Milliseconds $medianOverheadMs) ms
- p95 absolute overhead: $(Format-Milliseconds $p95OverheadMs) ms
- Median relative overhead: $(Format-Percent $medianOverheadPercent)

The observed overhead is the difference between the direct database baseline and the complete PDG request path. It must not be interpreted as database-only overhead.

## Conclusion

The required performance measurements were completed with the fixed demo dataset, at least 10 warm-up requests, at least 100 sequential measured requests, and concurrency equal to 1.

No SLA threshold is asserted by PDG v0.1 TWP.
"@

Set-Content `
    -Path $reportPath `
    -Value $report `
    -Encoding UTF8

Write-Host ""
Write-Host "Performance measurement completed."
Write-Host (
    "Baseline median: " +
    (Format-Milliseconds $baselineMedian) +
    " ms"
)
Write-Host (
    "Baseline p95:    " +
    (Format-Milliseconds $baselineP95) +
    " ms"
)
Write-Host (
    "PDG median:      " +
    (Format-Milliseconds $pdgMedian) +
    " ms"
)
Write-Host (
    "PDG p95:         " +
    (Format-Milliseconds $pdgP95) +
    " ms"
)
Write-Host (
    "Median overhead: " +
    (Format-Milliseconds $medianOverheadMs) +
    " ms"
)
Write-Host (
    "Report: " +
    $reportPath
)