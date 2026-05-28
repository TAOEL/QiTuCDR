param(
    [string]$ManifestPath,
    [string]$InstallRoot,
    [string]$AssemblyPath,
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

function Get-DefaultInstallRoot {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return Join-Path $localAppData "QiTuCDR"
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

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Get-DefaultInstallRoot
}

if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $AssemblyPath = Join-Path ([System.IO.Path]::GetFullPath($InstallRoot)) "App\QiTuCDR.Host.dll"
}

$failures = New-Object "System.Collections.Generic.List[string]"
$manifest = $null
$writes = @()

try {
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Add-Failure $failures "Manifest is not valid JSON: $($_.Exception.Message)"
}

if ($manifest -ne $null) {
    if ($manifest.Product -ne "QiTuCDR") {
        Add-Failure $failures "Product must be QiTuCDR."
    }

    if ($manifest.SchemaVersion -ne "1.0") {
        Add-Failure $failures "SchemaVersion must be 1.0."
    }

    $status = ([string]$manifest.Status).Trim().ToUpperInvariant()
    if ($status -ne "CONFIRMED") {
        Add-Failure $failures "Status must be CONFIRMED before registry writes are previewed."
    }

    $targets = @($manifest.Targets)
    if ($targets.Count -eq 0) {
        Add-Failure $failures "Targets must contain at least one item."
    }

    $seenVersions = @{}
    $seenRegistryPaths = @{}

    foreach ($target in $targets) {
        $versionIdentifier = [string]$target.CorelVersionIdentifier
        if ([string]::IsNullOrWhiteSpace($versionIdentifier)) {
            Add-Failure $failures "Target is missing CorelVersionIdentifier."
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

        if (-not [bool]$target.Enabled) {
            continue
        }

        $productLabel = [string]$target.ProductLabel
        $registrationKind = [string]$target.RegistrationKind
        $registryPath = [string]$target.RegistryPath
        $confirmationSource = [string]$target.ConfirmationSource
        $confirmedBy = [string]$target.ConfirmedBy
        $confirmedAt = [string]$target.ConfirmedAt

        if ([string]::IsNullOrWhiteSpace($productLabel)) {
            Add-Failure $failures "Enabled target is missing ProductLabel."
        }

        if ($registrationKind -ne "AddIn" -and $registrationKind -ne "Docker") {
            Add-Failure $failures "Enabled target RegistrationKind must be AddIn or Docker."
        }

        if (-not (Test-SafeCorelRegistryPath $registryPath)) {
            Add-Failure $failures "Enabled target RegistryPath is outside HKCU/HKLM Software Corel: $registryPath"
        }
        elseif ($seenRegistryPaths.ContainsKey($registryPath.ToLowerInvariant())) {
            Add-Failure $failures "Duplicate enabled RegistryPath: $registryPath"
        }
        else {
            $seenRegistryPaths[$registryPath.ToLowerInvariant()] = $true
        }

        if ([string]::IsNullOrWhiteSpace($confirmationSource)) {
            Add-Failure $failures "Enabled target is missing ConfirmationSource."
        }

        if ([string]::IsNullOrWhiteSpace($confirmedBy)) {
            Add-Failure $failures "Enabled target is missing ConfirmedBy."
        }

        if (-not (Test-IsoDateTime $confirmedAt)) {
            Add-Failure $failures "Enabled target ConfirmedAt must be a valid date/time."
        }

        $writes += [pscustomobject]@{
            CorelVersionIdentifier = $versionIdentifier
            ProductLabel = $productLabel
            RegistrationKind = $registrationKind
            RegistryPath = $registryPath
            RegistryKeyExists = [bool](Test-Path -Path $registryPath)
            AssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
            Values = [ordered]@{
                Name = "QiTuCDR"
                AssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
                InstalledAt = "<install time>"
                CorelVersionIdentifier = $versionIdentifier
                RegistrationKind = $registrationKind
            }
            ConfirmationSource = $confirmationSource
            ConfirmedBy = $confirmedBy
            ConfirmedAt = $confirmedAt
        }
    }

    if (@($writes).Count -eq 0) {
        Add-Failure $failures "Manifest has no enabled targets to preview."
    }
}

$statusText = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    ManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    InstallRoot = [System.IO.Path]::GetFullPath($InstallRoot)
    AssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
    WouldWriteCount = @($writes).Count
    WouldWrite = @($writes)
    FatalFailures = @($failures)
    Status = $statusText
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW registration preview" -ForegroundColor Cyan
Write-Host "ManifestPath: $ManifestPath"
Write-Host "AssemblyPath: $([System.IO.Path]::GetFullPath($AssemblyPath))"
Write-Host "WouldWriteCount: $($writes.Count)"
Write-Host "Status: $statusText" -ForegroundColor $(if ($statusText -eq "OK") { "Green" } else { "Yellow" })

foreach ($write in $writes) {
    Write-Host ""
    Write-Host ("Target: CorelDRAW {0} / {1}" -f $write.CorelVersionIdentifier, $write.RegistrationKind) -ForegroundColor Cyan
    Write-Host ("RegistryPath: {0}" -f $write.RegistryPath)
    Write-Host ("RegistryKeyExists: {0}" -f $write.RegistryKeyExists)
    Write-Host ("Name: {0}" -f $write.Values.Name)
    Write-Host ("AssemblyPath: {0}" -f $write.Values.AssemblyPath)
    Write-Host ("CorelVersionIdentifier: {0}" -f $write.Values.CorelVersionIdentifier)
    Write-Host ("RegistrationKind: {0}" -f $write.Values.RegistrationKind)
    Write-Host ("ConfirmationSource: {0}" -f $write.ConfirmationSource)
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
