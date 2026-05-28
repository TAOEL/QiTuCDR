param(
    [string]$SourcePath,
    [string]$Configuration = "Debug",
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repoRoot "src\Host\bin\$Configuration\net48"
}

$diagnosticsJson = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "tools\diagnostics\Test-QiTuEnvironment.ps1") -Json
if ($LASTEXITCODE -ne 0) {
    throw "Environment diagnostics failed with exit code $LASTEXITCODE."
}

$diagnostics = $diagnosticsJson | ConvertFrom-Json
$hostDll = Join-Path $SourcePath "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $SourcePath "WebUI\index.html"
$sourceExists = Test-Path -LiteralPath $SourcePath
$hostDllExists = Test-Path -LiteralPath $hostDll
$webUiExists = Test-Path -LiteralPath $webUiIndex

$failures = New-Object "System.Collections.Generic.List[string]"
foreach ($item in @($diagnostics.FatalFailures)) {
    if (-not [string]::IsNullOrWhiteSpace($item)) {
        $failures.Add([string]$item)
    }
}

if (-not $sourceExists) {
    $failures.Add("Source path is missing: $SourcePath")
}

if (-not $hostDllExists) {
    $failures.Add("QiTuCDR.Host.dll is missing: $hostDll")
}

if (-not $webUiExists) {
    $failures.Add("WebUI index is missing in source output: $webUiIndex")
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    Configuration = $Configuration
    SourcePath = $SourcePath
    SourceExists = $sourceExists
    HostDll = $hostDll
    HostDllExists = $hostDllExists
    WebUiIndex = $webUiIndex
    WebUiIndexExists = $webUiExists
    DiagnosticsStatus = $diagnostics.Status
    WebView2RuntimePaths = @($diagnostics.WebView2RuntimePaths)
    CorelDrawTypeLibs = @($diagnostics.CorelDrawTypeLibs)
    FatalFailures = @($failures)
    Status = $status
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR install prerequisites" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "SourcePath: $SourcePath"
Write-Host "HostDll: $(if ($hostDllExists) { 'OK' } else { 'MISSING' })"
Write-Host "WebUI: $(if ($webUiExists) { 'OK' } else { 'MISSING' })"
Write-Host "Diagnostics: $($diagnostics.Status)"
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })

if ($failures.Count -gt 0) {
    Write-Host "Required actions:" -ForegroundColor Yellow
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
