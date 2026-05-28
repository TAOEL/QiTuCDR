param(
    [string]$CorelProgramsDirectory = "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64",
    [string]$AddonName = "QiTuCDR",
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

$programsFull = [System.IO.Path]::GetFullPath($CorelProgramsDirectory)
$addonDirectory = Join-Path $programsFull "Addons\$AddonName"
$addonDirectoryPrefix = [System.IO.Path]::GetFullPath($addonDirectory).TrimEnd('\') + '\'
$failures = New-Object "System.Collections.Generic.List[string]"
$loadedModules = @()
$processes = @(Get-Process CorelDRW -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ([System.IO.Path]::GetFullPath($_.Path).StartsWith($programsFull, [System.StringComparison]::OrdinalIgnoreCase))
})

if ($processes.Count -eq 0) {
    Add-Failure $failures "No CorelDRW process is running from CorelProgramsDirectory: $programsFull"
}

if (-not (Test-Path -LiteralPath $addonDirectory)) {
    Add-Failure $failures "Addon directory does not exist: $addonDirectory"
}

foreach ($process in $processes) {
    try {
        $loadedModules += @($process.Modules | Where-Object {
            $_.FileName -and ([System.IO.Path]::GetFullPath($_.FileName).StartsWith($addonDirectoryPrefix, [System.StringComparison]::OrdinalIgnoreCase))
        } | ForEach-Object {
            [pscustomobject]@{
                ProcessId = $process.Id
                ModuleName = $_.ModuleName
                FileName = $_.FileName
            }
        })
    }
    catch {
        Add-Failure $failures "Could not inspect process modules for PID $($process.Id): $($_.Exception.Message)"
    }
}

$requiredModules = @(
    "QiTuCDR.Host.dll",
    "QiTuCDR.Core.dll",
    "QiTuCDR.Bridge.dll",
    "QiTuCDR.Infrastructure.dll",
    "QiTuCDR.Shared.dll"
)

foreach ($required in $requiredModules) {
    if (-not ($loadedModules | Where-Object { $_.ModuleName -eq $required } | Select-Object -First 1)) {
        Add-Failure $failures "Required addon module is not loaded in CorelDRAW process: $required"
    }
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    CorelProgramsDirectory = $programsFull
    AddonDirectory = $addonDirectory
    ProcessCount = $processes.Count
    Processes = @($processes | Select-Object Id, ProcessName, Path, StartTime)
    LoadedModules = @($loadedModules)
    FatalFailures = @($failures)
    Status = $status
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW addon load check" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "CorelProgramsDirectory: $programsFull"
Write-Host "AddonDirectory: $addonDirectory"
Write-Host "ProcessCount: $($processes.Count)"
Write-Host "LoadedAddonModuleCount: $($loadedModules.Count)"
foreach ($module in $loadedModules) {
    Write-Host "  - PID $($module.ProcessId): $($module.ModuleName)"
}

if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
