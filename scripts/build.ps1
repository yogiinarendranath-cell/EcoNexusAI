<#
.SYNOPSIS
    Build and test EcoNexus AI from a clean state.

.DESCRIPTION
    1. Kills any stale EcoNexus.Api process that would lock build output.
    2. Cleans and restores NuGet packages.
    3. Builds the solution with warnings treated as errors (enforced by Directory.Build.props).
    4. Runs all tests.
    Exits with non-zero status on any failure so it can be used in CI.

.EXAMPLE
    .\scripts\build.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " EcoNexus AI — Build & Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Kill stale API process (common cause of file-lock build failures)
Write-Host "`n[1/4] Stopping stale EcoNexus.Api processes..." -ForegroundColor Yellow
$stale = Get-Process -Name "EcoNexus.Api" -ErrorAction SilentlyContinue
if ($stale) {
    $stale | Stop-Process -Force
    Write-Host "      Stopped $($stale.Count) process(es)." -ForegroundColor Yellow
} else {
    Write-Host "      None running." -ForegroundColor DarkGray
}

# 2. Clean and restore
Write-Host "`n[2/4] Cleaning and restoring..." -ForegroundColor Yellow
dotnet clean | Out-Null
dotnet restore
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed." -ForegroundColor Red; exit 1 }

# 3. Build
Write-Host "`n[3/4] Building..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }

# 4. Test
Write-Host "`n[4/4] Running tests..." -ForegroundColor Yellow
dotnet test --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed." -ForegroundColor Red; exit 1 }

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " BUILD & TEST SUCCEEDED" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
exit 0
