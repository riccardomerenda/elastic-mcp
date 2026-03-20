<#
.SYNOPSIS
    ElasticMCP — Local Test Pipeline
.DESCRIPTION
    Runs build verification, unit tests, and integration tests (Docker required).
.PARAMETER UnitOnly
    Run only unit tests (no Docker needed)
.PARAMETER IntegrationOnly
    Run only integration tests (Docker required)
.PARAMETER Coverage
    Collect code coverage via coverlet
.EXAMPLE
    .\test.ps1
    .\test.ps1 -UnitOnly
    .\test.ps1 -Coverage
#>
param(
    [switch]$UnitOnly,
    [switch]$IntegrationOnly,
    [switch]$Coverage
)

$ErrorActionPreference = "Stop"
$step = 0
$pass = 0
$fail = 0

function Write-Step($title) {
    $script:step++
    Write-Host ""
    Write-Host "  Step $script:step`: $title" -ForegroundColor Cyan
    Write-Host ("  " + ("=" * 54)) -ForegroundColor Cyan
}

function Test-StepResult($exitCode, $label) {
    if ($exitCode -eq 0) {
        Write-Host "  [PASS] $label" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  [FAIL] $label" -ForegroundColor Red
        $script:fail++
    }
}

# ── Step 1: Prerequisites ────────────────────────────────────────
Write-Step "Check prerequisites"

$dotnetVersion = & dotnet --version 2>$null
if ($dotnetVersion) {
    Write-Host "  .NET SDK: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  .NET SDK not found. Install from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

if (-not $UnitOnly) {
    $ErrorActionPreference = "Continue"
    $null = & docker info 2>&1
    $dockerExit = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    if ($dockerExit -eq 0) {
        Write-Host "  Docker:   Running" -ForegroundColor Green
    } else {
        Write-Host "  Docker is not running - integration tests will be skipped." -ForegroundColor Yellow
        Write-Host "  Start Docker Desktop and re-run, or use -UnitOnly." -ForegroundColor Yellow
        $UnitOnly = $true
    }
}

# ── Step 2: Clean & Restore ──────────────────────────────────────
Write-Step "Clean and restore packages"

& dotnet clean ElasticMcp.slnx -v quiet 2>&1 | Out-Null
& dotnet restore ElasticMcp.slnx -v quiet 2>&1 | Out-Null
Test-StepResult $LASTEXITCODE "Packages restored"

# ── Step 3: Build ────────────────────────────────────────────────
Write-Step "Build solution"

& dotnet build ElasticMcp.slnx --no-restore -v quiet
Test-StepResult $LASTEXITCODE "Build succeeded"

# ── Step 4: Unit Tests ───────────────────────────────────────────
if (-not $IntegrationOnly) {
    Write-Step "Unit tests"

    if ($Coverage) {
        & dotnet test tests/ElasticMcp.Tests/ --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory ./test-results/unit `
            --verbosity normal
    } else {
        & dotnet test tests/ElasticMcp.Tests/ --no-build --verbosity normal
    }
    Test-StepResult $LASTEXITCODE "Unit tests"
}

# ── Step 5: Integration Tests ────────────────────────────────────
if (-not $UnitOnly) {
    Write-Step "Integration tests (Testcontainers + Elasticsearch 9.x)"
    Write-Host "  This may take 1-2 minutes on first run (Docker image pull)" -ForegroundColor Yellow

    if ($Coverage) {
        & dotnet test tests/ElasticMcp.IntegrationTests/ --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory ./test-results/integration `
            --verbosity normal
    } else {
        & dotnet test tests/ElasticMcp.IntegrationTests/ --no-build --verbosity normal
    }
    Test-StepResult $LASTEXITCODE "Integration tests"
}

# ── Summary ──────────────────────────────────────────────────────
Write-Host ""
Write-Host ("  " + ("=" * 54)) -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host ("  " + ("=" * 54)) -ForegroundColor Cyan
Write-Host "  Steps passed: $pass" -ForegroundColor Green

if ($fail -gt 0) {
    Write-Host "  Steps failed: $fail" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Pipeline FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "  All checks passed!" -ForegroundColor Green
}
