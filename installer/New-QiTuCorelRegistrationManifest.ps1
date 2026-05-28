param(
    [string]$OutputPath,
    [string[]]$TargetCorelVersions = @("23", "24", "25", "26", "27"),
    [string]$Status = "DRAFT",
    [string]$EnableCorelVersionIdentifier,
    [string]$ProductLabel,
    [string]$RegistrationKind = "AddIn",
    [string]$RegistryPath,
    [string]$ConfirmationSource,
    [string]$ConfirmedBy,
    [string]$ConfirmedAt
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "artifacts\registration\qitucdr-coreldraw-registration-manifest.template.json"
}

$normalizedStatus = $Status.Trim().ToUpperInvariant()
if ($normalizedStatus -ne "DRAFT" -and $normalizedStatus -ne "CONFIRMED") {
    throw "Status must be DRAFT or CONFIRMED."
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

$versions = @(
    $TargetCorelVersions |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { [string]$_ } |
        Select-Object -Unique
)

if ($versions.Count -eq 0) {
    throw "At least one target CorelDRAW version identifier is required."
}

$enabledVersion = ""
if (-not [string]::IsNullOrWhiteSpace($EnableCorelVersionIdentifier)) {
    $enabledVersion = $EnableCorelVersionIdentifier.Trim()

    if ($normalizedStatus -ne "CONFIRMED") {
        throw "Use -Status CONFIRMED when enabling a CorelDRAW registration target."
    }

    if (-not (Test-CorelVersionIdentifier $enabledVersion)) {
        throw "EnableCorelVersionIdentifier must be one of 23, 24, 25, 26, 27."
    }

    if ($versions -notcontains $enabledVersion) {
        throw "EnableCorelVersionIdentifier must be included in TargetCorelVersions."
    }

    if ([string]::IsNullOrWhiteSpace($ProductLabel)) {
        throw "ProductLabel is required when enabling a target."
    }

    if ($RegistrationKind -ne "AddIn" -and $RegistrationKind -ne "Docker") {
        throw "RegistrationKind must be AddIn or Docker."
    }

    if (-not (Test-SafeCorelRegistryPath $RegistryPath)) {
        throw "RegistryPath must be under HKCU:\Software\Corel\ or HKLM:\Software\Corel\."
    }

    if ([string]::IsNullOrWhiteSpace($ConfirmationSource)) {
        throw "ConfirmationSource is required when enabling a target."
    }

    if ([string]::IsNullOrWhiteSpace($ConfirmedBy)) {
        throw "ConfirmedBy is required when enabling a target."
    }

    if (-not (Test-IsoDateTime $ConfirmedAt)) {
        throw "ConfirmedAt is required and must be a valid date/time when enabling a target."
    }
}

$targets = foreach ($version in $versions) {
    $enabled = ($version -eq $enabledVersion)

    [ordered]@{
        Enabled = $enabled
        CorelVersionIdentifier = $version
        ProductLabel = $(if ($enabled) { $ProductLabel } else { "" })
        RegistrationKind = $(if ($enabled) { $RegistrationKind } else { "AddIn" })
        RegistryPath = $(if ($enabled) { $RegistryPath } else { "" })
        ConfirmationSource = $(if ($enabled) { $ConfirmationSource } else { "" })
        ConfirmedBy = $(if ($enabled) { $ConfirmedBy } else { "" })
        ConfirmedAt = $(if ($enabled) { $ConfirmedAt } else { "" })
    }
}

$manifest = [ordered]@{
    SchemaVersion = "1.0"
    Product = "QiTuCDR"
    Status = $normalizedStatus
    CreatedAt = (Get-Date).ToString("o")
    Notes = "Set Enabled to true only after the official CorelDRAW AddIn or Docker registration path is confirmed."
    Targets = @($targets)
}

$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

Write-Host "QiTuCDR CorelDRAW registration manifest template created." -ForegroundColor Green
Write-Host "OutputPath: $OutputPath"
Write-Host "Status: $normalizedStatus"
Write-Host "TargetCount: $($versions.Count)"
if (-not [string]::IsNullOrWhiteSpace($enabledVersion)) {
    Write-Host "EnabledTarget: $enabledVersion"
}
