param(
    [string]$CorelProgramsDirectory = "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64",
    [string]$AddonName = "QiTuCDR",
    [switch]$Enable,
    [switch]$Disable,
    [switch]$HardDisable,
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

function Assert-ChildPath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not $pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside addon directory: $Path"
    }
}

if ($Enable -and $Disable) {
    throw "Use either -Enable or -Disable, not both."
}

if (-not $Enable -and -not $Disable) {
    $Disable = $true
}

$programsFull = [System.IO.Path]::GetFullPath($CorelProgramsDirectory)
$addonDirectory = Join-Path $programsFull "Addons\$AddonName"
$enabledMarker = Join-Path $addonDirectory "CorelDrw.addon"
$disabledMarker = Join-Path $addonDirectory "CorelDrw.addon.disabled"
$hostDll = Join-Path $addonDirectory "QiTuCDR.Host.dll"
$hardDisabledHostDll = Join-Path $addonDirectory "QiTuCDR.Host.dll.disabled"
$manifestPath = Join-Path $addonDirectory "qitucdr-addon-install-manifest.json"
$failures = New-Object "System.Collections.Generic.List[string]"
$warnings = New-Object "System.Collections.Generic.List[string]"

if (-not (Test-Path -LiteralPath $addonDirectory)) {
    Add-Failure $failures "Addon directory was not found: $addonDirectory"
}

$action = if ($Enable) { "Enable" } else { "Disable" }

if ($failures.Count -eq 0) {
    Assert-ChildPath $addonDirectory $enabledMarker
    Assert-ChildPath $addonDirectory $disabledMarker
    Assert-ChildPath $addonDirectory $hostDll
    Assert-ChildPath $addonDirectory $hardDisabledHostDll

    if ($Enable) {
        if (Test-Path -LiteralPath $enabledMarker) {
            # Already enabled.
        }
        elseif (Test-Path -LiteralPath $disabledMarker) {
            Rename-Item -LiteralPath $disabledMarker -NewName "CorelDrw.addon"
        }
        else {
            New-Item -ItemType File -Force -Path $enabledMarker | Out-Null
        }
    }
    else {
        if (Test-Path -LiteralPath $disabledMarker) {
            # Already disabled.
        }
        elseif (Test-Path -LiteralPath $enabledMarker) {
            Rename-Item -LiteralPath $enabledMarker -NewName "CorelDrw.addon.disabled"
        }
        else {
            New-Item -ItemType File -Force -Path $disabledMarker | Out-Null
        }

        if ($HardDisable -and (Test-Path -LiteralPath $hostDll)) {
            $loadedBy = @()
            $programsPrefix = $programsFull.TrimEnd('\') + '\'
            foreach ($process in @(Get-Process CorelDRW -ErrorAction SilentlyContinue | Where-Object { $_.Path -and ([System.IO.Path]::GetFullPath($_.Path).StartsWith($programsPrefix, [System.StringComparison]::OrdinalIgnoreCase)) })) {
                try {
                    $loaded = @($process.Modules | Where-Object { $_.FileName -and ([System.IO.Path]::GetFullPath($_.FileName).Equals(([System.IO.Path]::GetFullPath($hostDll)), [System.StringComparison]::OrdinalIgnoreCase)) })
                    if ($loaded.Count -gt 0) {
                        $loadedBy += $process.Id
                    }
                }
                catch {
                    $warnings.Add("Could not enumerate modules for PID $($process.Id): $($_.Exception.Message)") | Out-Null
                }
            }

            if ($loadedBy.Count -gt 0) {
                Add-Failure $failures "HardDisable refused because QiTuCDR.Host.dll is still loaded by CorelDRAW PID(s): $($loadedBy -join ', '). Close target CorelDRAW first."
            }
            elseif (Test-Path -LiteralPath $hardDisabledHostDll) {
                $warnings.Add("Hard-disabled Host DLL already exists: $hardDisabledHostDll") | Out-Null
            }
            else {
                Rename-Item -LiteralPath $hostDll -NewName "QiTuCDR.Host.dll.disabled"
            }
        }
    }

    if ($failures.Count -eq 0 -and (Test-Path -LiteralPath $manifestPath)) {
        try {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
            $manifest | Add-Member -NotePropertyName Marker -NotePropertyValue $(if ($Enable) { "CorelDrw.addon" } else { "CorelDrw.addon.disabled" }) -Force
            $manifest | Add-Member -NotePropertyName AutoLoadEnabled -NotePropertyValue ([bool]$Enable) -Force
            $manifest | Add-Member -NotePropertyName LastAutoLoadChangeAt -NotePropertyValue (Get-Date).ToString("o") -Force
            $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
        }
        catch {
            $warnings.Add("Could not update addon manifest: $($_.Exception.Message)") | Out-Null
        }
    }
}

$enabled = Test-Path -LiteralPath $enabledMarker
$disabled = Test-Path -LiteralPath $disabledMarker
$hostDllExists = Test-Path -LiteralPath $hostDll
$hostDllHardDisabled = Test-Path -LiteralPath $hardDisabledHostDll
$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    Status = $status
    Action = $action
    CorelProgramsDirectory = $programsFull
    AddonDirectory = $addonDirectory
    AutoLoadEnabled = $enabled
    AutoLoadDisabled = $disabled
    HostDllExists = $hostDllExists
    HostDllHardDisabled = $hostDllHardDisabled
    Warnings = @($warnings)
    FatalFailures = @($failures)
}

if ($Json) {
    $result | ConvertTo-Json -Depth 5
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW addon autoload" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "Action: $action"
Write-Host "CorelProgramsDirectory: $programsFull"
Write-Host "AddonDirectory: $addonDirectory"
Write-Host "AutoLoadEnabled: $enabled"
Write-Host "AutoLoadDisabled: $disabled"
Write-Host "HostDllExists: $hostDllExists"
Write-Host "HostDllHardDisabled: $hostDllHardDisabled"
if ($warnings.Count -gt 0) {
    Write-Host "Warnings:" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  - $warning" -ForegroundColor Yellow
    }
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
