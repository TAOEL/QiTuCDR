param(
    [string]$OutputDirectory,
    [switch]$StartIfMissing,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\validation"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonPath = Join-Path $OutputDirectory "qitucdr-coreldraw-com-smoke-$timestamp.json"
$markdownPath = Join-Path $OutputDirectory "qitucdr-coreldraw-com-smoke-$timestamp.md"
$failures = New-Object "System.Collections.Generic.List[string]"
$app = $null
$createdBySmoke = $false
$name = "UNKNOWN"
$version = "UNKNOWN"
$visible = "UNKNOWN"
$processes = @()

try {
    try {
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject("CorelDRAW.Application")
    }
    catch {
        if ($StartIfMissing) {
            $app = New-Object -ComObject "CorelDRAW.Application"
            $createdBySmoke = $true
        }
        else {
            Add-Failure $failures "CorelDRAW.Application COM object is not active. Re-run with -StartIfMissing only when starting CorelDRAW is intended."
        }
    }

    if ($app -ne $null) {
        try { $name = [string]$app.Name } catch { Add-Failure $failures "Could not read CorelDRAW application Name: $($_.Exception.Message)" }
        try { $version = [string]$app.Version } catch { Add-Failure $failures "Could not read CorelDRAW application Version: $($_.Exception.Message)" }
        try { $visible = [string]$app.Visible } catch { Add-Failure $failures "Could not read CorelDRAW application Visible state: $($_.Exception.Message)" }
        try { $app.Visible = $true } catch { Add-Failure $failures "Could not set CorelDRAW application Visible state: $($_.Exception.Message)" }
    }

    $processes = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq "CorelDRW" } | ForEach-Object {
        [pscustomobject]@{
            Id = $_.Id
            ProcessName = $_.ProcessName
            Path = $_.Path
        }
    })
}
catch {
    Add-Failure $failures "CorelDRAW COM smoke failed: $($_.Exception.Message)"
}
finally {
    if ($app -ne $null -and [Runtime.InteropServices.Marshal]::IsComObject($app)) {
        [Runtime.InteropServices.Marshal]::ReleaseComObject($app) | Out-Null
        $app = $null
    }
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    Status = $status
    Name = $name
    Version = $version
    Visible = $visible
    CreatedBySmoke = [bool]$createdBySmoke
    Processes = @($processes)
    FatalFailures = @($failures)
}

$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = New-Object "System.Collections.Generic.List[string]"
$lines.Add("# QiTuCDR CorelDRAW COM Smoke") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("Status: $status") | Out-Null
$lines.Add("Name: $name") | Out-Null
$lines.Add("Version: $version") | Out-Null
$lines.Add("Visible: $visible") | Out-Null
$lines.Add("CreatedBySmoke: $createdBySmoke") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("## Processes") | Out-Null
if ($processes.Count -eq 0) {
    $lines.Add("- None") | Out-Null
}
else {
    foreach ($process in $processes) {
        $lines.Add("- $($process.Id) / $($process.ProcessName) / $($process.Path)") | Out-Null
    }
}
$lines.Add("") | Out-Null
$lines.Add("## Failures") | Out-Null
if ($failures.Count -eq 0) {
    $lines.Add("- None") | Out-Null
}
else {
    foreach ($failure in $failures) {
        $lines.Add("- $failure") | Out-Null
    }
}

$lines | Set-Content -LiteralPath $markdownPath -Encoding UTF8

if ($Json) {
    $result | ConvertTo-Json -Depth 6
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW COM smoke" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "Name: $name"
Write-Host "Version: $version"
Write-Host "Visible: $visible"
Write-Host "Json: $jsonPath"
Write-Host "Markdown: $markdownPath"
if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
