param(
    [string]$ManifestPath,
    [switch]$RequireConfirmed,
    [switch]$AllowNonCorelRegistryPath,
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

function Test-SafeCorelRegistryPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    return $Path -match "^HK(CU|LM):\\Software\\(WOW6432Node\\)?Corel\\"
}

function Test-CorelVersionIdentifier {
    param([string]$Version)

    return $Version -match "^(23|24|25|26|27)$"
}

function Test-IsoDateTime {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $parsed = [DateTimeOffset]::MinValue
    return [DateTimeOffset]::TryParse($Value, [ref]$parsed)
}

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    throw "ManifestPath is required."
}

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "ManifestPath does not exist: $ManifestPath"
}

$failures = New-Object "System.Collections.Generic.List[string]"
$manifest = $null

try {
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Add-Failure $failures "Manifest is not valid JSON: $($_.Exception.Message)"
}

$enabledTargets = @()

if ($manifest -ne $null) {
    if ($manifest.Product -ne "QiTuCDR") {
        Add-Failure $failures "Product must be QiTuCDR."
    }

    if ($manifest.SchemaVersion -ne "1.0") {
        Add-Failure $failures "SchemaVersion must be 1.0."
    }

    $status = ([string]$manifest.Status).Trim().ToUpperInvariant()
    if ($status -ne "DRAFT" -and $status -ne "CONFIRMED") {
        Add-Failure $failures "Status must be DRAFT or CONFIRMED."
    }

    if ($RequireConfirmed -and $status -ne "CONFIRMED") {
        Add-Failure $failures "Status must be CONFIRMED when RequireConfirmed is set."
    }

    $targets = @($manifest.Targets)
    if ($targets.Count -eq 0) {
        Add-Failure $failures "Targets must contain at least one item."
    }

    $seenVersions = @{}
    $seenRegistryPaths = @{}

    foreach ($target in $targets) {
        $isEnabled = [bool]$target.Enabled
        if ($isEnabled) {
            $enabledTargets += $target
        }

        $versionIdentifier = [string]$target.CorelVersionIdentifier
        $registryPath = [string]$target.RegistryPath

        if ([string]::IsNullOrWhiteSpace($versionIdentifier)) {
            Add-Failure $failures "Enabled or draft target is missing CorelVersionIdentifier."
        }
        elseif (-not (Test-CorelVersionIdentifier $versionIdentifier)) {
            Add-Failure $failures "CorelVersionIdentifier must be one of 23, 24, 25, 26, 27: $versionIdentifier"
        }
        elseif ($seenVersions.ContainsKey($versionIdentifier)) {
            Add-Failure $failures "Duplicate CorelVersionIdentifier: $versionIdentifier"
        }
        else {
            $seenVersions[$versionIdentifier] = $true
        }

        if ($isEnabled) {
            if ([string]::IsNullOrWhiteSpace([string]$target.ProductLabel)) {
                Add-Failure $failures "Enabled target is missing ProductLabel."
            }

            $registrationKind = [string]$target.RegistrationKind
            if ($registrationKind -ne "AddIn" -and $registrationKind -ne "Docker") {
                Add-Failure $failures "Enabled target RegistrationKind must be AddIn or Docker."
            }

            if ([string]::IsNullOrWhiteSpace($registryPath)) {
                Add-Failure $failures "Enabled target is missing RegistryPath."
            }
            elseif (-not $AllowNonCorelRegistryPath -and -not (Test-SafeCorelRegistryPath $registryPath)) {
                Add-Failure $failures "Enabled target RegistryPath is outside HKCU/HKLM Software Corel: $($target.RegistryPath)"
            }
            elseif ($seenRegistryPaths.ContainsKey($registryPath.ToLowerInvariant())) {
                Add-Failure $failures "Duplicate enabled RegistryPath: $registryPath"
            }
            else {
                $seenRegistryPaths[$registryPath.ToLowerInvariant()] = $true
            }

            if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmationSource)) {
                Add-Failure $failures "Enabled target is missing ConfirmationSource."
            }

            if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmedBy)) {
                Add-Failure $failures "Enabled target is missing ConfirmedBy."
            }

            if (-not (Test-IsoDateTime ([string]$target.ConfirmedAt))) {
                Add-Failure $failures "Enabled target ConfirmedAt must be a valid date/time."
            }
        }
    }

    if ($RequireConfirmed -and $enabledTargets.Count -eq 0) {
        Add-Failure $failures "At least one target must be enabled when RequireConfirmed is set."
    }
}

$statusText = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    ManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    RequireConfirmed = [bool]$RequireConfirmed
    EnabledTargetCount = @($enabledTargets).Count
    FatalFailures = @($failures)
    Status = $statusText
}

if ($Json) {
    $result | ConvertTo-Json -Depth 5
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW registration manifest verification" -ForegroundColor Cyan
Write-Host "ManifestPath: $ManifestPath"
Write-Host "EnabledTargetCount: $($enabledTargets.Count)"
Write-Host "Status: $statusText" -ForegroundColor $(if ($statusText -eq "OK") { "Green" } else { "Yellow" })

if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
