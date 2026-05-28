param(
    [string]$OutputDirectory,
    [string]$CorelDrawVersion,
    [string]$CorelVersionIdentifier,
    [string]$RegistrationManifestPath,
    [string]$SettingsPath,
    [string]$DockHostMode,
    [switch]$AllowOfficialCorelDockerAdapter,
    [switch]$DisableOfficialCorelDockerAdapter,
    [string]$ActiveDockPanelHostKind,
    [string]$ActiveDockerAdapterType,
    [string]$IsDockerAdapterAttached,
    [string]$WebViewCreateCount,
    [switch]$SkipRegistrationPlan
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\validation"
}

if ($AllowOfficialCorelDockerAdapter -and $DisableOfficialCorelDockerAdapter) {
    throw "Use only one of -AllowOfficialCorelDockerAdapter or -DisableOfficialCorelDockerAdapter."
}

function Get-VersionText {
    $versionPath = Join-Path $repoRoot "VERSION"
    if (Test-Path -LiteralPath $versionPath) {
        return (Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8).Trim()
    }

    return "UNKNOWN"
}

function Get-WindowsVersionText {
    try {
        $os = Get-CimInstance Win32_OperatingSystem
        return "$($os.Caption) $($os.Version) build $($os.BuildNumber)"
    }
    catch {
        return "UNKNOWN"
    }
}

function Get-WebView2RuntimeVersion {
    $roots = @(
        "HKCU:\Software\Microsoft\EdgeUpdate\Clients",
        "HKLM:\Software\Microsoft\EdgeUpdate\Clients",
        "HKLM:\Software\WOW6432Node\Microsoft\EdgeUpdate\Clients"
    )

    foreach ($root in $roots) {
        if (-not (Test-Path -Path $root)) {
            continue
        }

        $runtime = Get-ChildItem -Path $root -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $item = Get-ItemProperty -Path $_.PSPath -ErrorAction Stop
                if ($item.name -like "*WebView2*") {
                    return [string]$item.pv
                }
            }
            catch {
            }
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

        if (-not [string]::IsNullOrWhiteSpace($runtime)) {
            return $runtime
        }
    }

    return "UNKNOWN"
}

function Get-DefaultSettingsPath {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return Join-Path $localAppData "QiTuCDR\Config\settings.json"
}

function New-DefaultConfigSnapshot {
    return [pscustomobject]@{
        DockHostMode = "Debug"
        AllowOfficialCorelDockerAdapter = $false
    }
}

function Get-PropertyValue {
    param(
        [object]$Source,
        [string[]]$Names,
        [object]$DefaultValue
    )

    foreach ($name in $Names) {
        $property = $Source.PSObject.Properties[$name]
        if ($property) {
            return $property.Value
        }
    }

    return $DefaultValue
}

function Read-ConfigSnapshot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return New-DefaultConfigSnapshot
    }

    try {
        $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $source = $raw | ConvertFrom-Json

        return [pscustomobject]@{
            DockHostMode = [string](Get-PropertyValue $source @("DockHostMode", "dockHostMode") "Debug")
            AllowOfficialCorelDockerAdapter = [bool](Get-PropertyValue $source @("AllowOfficialCorelDockerAdapter", "allowOfficialCorelDockerAdapter") $false)
        }
    }
    catch {
        Write-Warning "Could not read settings snapshot. Safe defaults will be used. Path: $Path"
        return New-DefaultConfigSnapshot
    }
}

function Normalize-DockHostMode {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "Debug"
    }

    $normalized = $Value.Trim()
    if ($normalized -ne "Debug" -and $normalized -ne "CorelDocker") {
        throw "DockHostMode must be Debug or CorelDocker."
    }

    return $normalized
}

function Set-TableValue {
    param(
        [string]$Text,
        [string]$Field,
        [string]$Value
    )

    $escapedField = [regex]::Escape($Field)
    $safeValue = ""
    if ($null -ne $Value) {
        $safeValue = [string]$Value
    }

    $safeValue = $safeValue -replace "\|", "/"
    $pattern = "^(?<prefix>\|\s*$escapedField\s*\|\s*)[^|\r\n]*(?<suffix>\|.*)$"
    $lines = $Text -split "`r?`n"
    $updated = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) {
            $lines[$i] = "$($Matches["prefix"])$safeValue $($Matches["suffix"])"
            $updated = $true
            break
        }
    }

    if (-not $updated) {
        throw "Template table field was not found: $Field"
    }

    return ($lines -join [Environment]::NewLine)
}

function U {
    param([int[]]$CodePoints)

    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

function Get-LatestFile {
    param(
        [string]$Directory,
        [string]$Filter
    )

    if (-not (Test-Path -LiteralPath $Directory)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $Directory -Filter $Filter -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
    $SettingsPath = Get-DefaultSettingsPath
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$version = Get-VersionText
$windowsVersion = Get-WindowsVersionText
$webView2Version = Get-WebView2RuntimeVersion
$machineName = $env:COMPUTERNAME
$userName = $env:USERNAME
$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$configSnapshot = Read-ConfigSnapshot $SettingsPath
$effectiveDockHostMode = Normalize-DockHostMode $configSnapshot.DockHostMode
$effectiveAllowOfficialCorelDockerAdapter = [bool]$configSnapshot.AllowOfficialCorelDockerAdapter

if (-not [string]::IsNullOrWhiteSpace($DockHostMode)) {
    $effectiveDockHostMode = Normalize-DockHostMode $DockHostMode
}

if ($AllowOfficialCorelDockerAdapter) {
    $effectiveAllowOfficialCorelDockerAdapter = $true
}

if ($DisableOfficialCorelDockerAdapter) {
    $effectiveAllowOfficialCorelDockerAdapter = $false
}

if ([string]::IsNullOrWhiteSpace($ActiveDockPanelHostKind)) {
    $ActiveDockPanelHostKind = "Pending real host snapshot"
}

if ([string]::IsNullOrWhiteSpace($ActiveDockerAdapterType)) {
    $ActiveDockerAdapterType = "Pending real host snapshot"
}

if ([string]::IsNullOrWhiteSpace($IsDockerAdapterAttached)) {
    $IsDockerAdapterAttached = "Pending real host snapshot"
}

if ([string]::IsNullOrWhiteSpace($WebViewCreateCount)) {
    $WebViewCreateCount = "Pending real host snapshot"
}

$registrationPlanJson = ""
$registrationPlanMarkdown = ""

if (-not $SkipRegistrationPlan) {
    $registrationPlanDirectory = Join-Path $OutputDirectory "registration-plan-$timestamp"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "installer\Get-QiTuCorelRegistrationPlan.ps1") -OutputDirectory $registrationPlanDirectory | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Get-QiTuCorelRegistrationPlan.ps1 failed with exit code $LASTEXITCODE."
    }

    $latestJson = Get-LatestFile $registrationPlanDirectory "qitucdr-coreldraw-registration-plan-*.json"
    $latestMarkdown = Get-LatestFile $registrationPlanDirectory "qitucdr-coreldraw-registration-plan-*.md"
    if ($latestJson) {
        $registrationPlanJson = $latestJson.FullName
    }

    if ($latestMarkdown) {
        $registrationPlanMarkdown = $latestMarkdown.FullName
    }
}

$validationTemplatePath = Join-Path $repoRoot "docs\REAL_HOST_VALIDATION_TEMPLATE.md"
$registrationTemplatePath = Join-Path $repoRoot "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md"

$validationText = Get-Content -LiteralPath $validationTemplatePath -Raw -Encoding UTF8
$registrationText = Get-Content -LiteralPath $registrationTemplatePath -Raw -Encoding UTF8

$validationPath = Join-Path $OutputDirectory "qitucdr-real-host-validation-$timestamp.md"
$registrationPath = Join-Path $OutputDirectory "qitucdr-registration-confirmation-$timestamp.md"

$validationText = Set-TableValue $validationText (U @(0x6D4B, 0x8BD5, 0x65E5, 0x671F)) $generatedAt
$validationText = Set-TableValue $validationText (U @(0x6D4B, 0x8BD5, 0x4EBA, 0x5458)) $userName
$validationText = Set-TableValue $validationText (U @(0x43, 0x6F, 0x72, 0x65, 0x6C, 0x44, 0x52, 0x41, 0x57, 0x20, 0x7248, 0x672C)) $CorelDrawVersion
$validationText = Set-TableValue $validationText (U @(0x57, 0x69, 0x6E, 0x64, 0x6F, 0x77, 0x73, 0x20, 0x7248, 0x672C)) $windowsVersion
$validationText = Set-TableValue $validationText (U @(0x57, 0x65, 0x62, 0x56, 0x69, 0x65, 0x77, 0x32, 0x20, 0x52, 0x75, 0x6E, 0x74, 0x69, 0x6D, 0x65, 0x20, 0x7248, 0x672C)) $webView2Version
$validationText = Set-TableValue $validationText (U @(0x51, 0x69, 0x54, 0x75, 0x43, 0x44, 0x52, 0x20, 0x6784, 0x5EFA, 0x7248, 0x672C, 0x2F, 0x63D0, 0x4EA4, 0x6807, 0x8BC6)) $version
$validationText = Set-TableValue $validationText (U @(0x44, 0x6F, 0x63, 0x6B, 0x48, 0x6F, 0x73, 0x74, 0x4D, 0x6F, 0x64, 0x65)) $effectiveDockHostMode
$validationText = Set-TableValue $validationText (U @(0x41, 0x6C, 0x6C, 0x6F, 0x77, 0x4F, 0x66, 0x66, 0x69, 0x63, 0x69, 0x61, 0x6C, 0x43, 0x6F, 0x72, 0x65, 0x6C, 0x44, 0x6F, 0x63, 0x6B, 0x65, 0x72, 0x41, 0x64, 0x61, 0x70, 0x74, 0x65, 0x72)) ([string]$effectiveAllowOfficialCorelDockerAdapter)
$validationText = Set-TableValue $validationText (U @(0x41, 0x63, 0x74, 0x69, 0x76, 0x65, 0x44, 0x6F, 0x63, 0x6B, 0x50, 0x61, 0x6E, 0x65, 0x6C, 0x48, 0x6F, 0x73, 0x74, 0x4B, 0x69, 0x6E, 0x64)) $ActiveDockPanelHostKind
$validationText = Set-TableValue $validationText (U @(0x41, 0x63, 0x74, 0x69, 0x76, 0x65, 0x44, 0x6F, 0x63, 0x6B, 0x65, 0x72, 0x41, 0x64, 0x61, 0x70, 0x74, 0x65, 0x72, 0x54, 0x79, 0x70, 0x65)) $ActiveDockerAdapterType
$validationText = Set-TableValue $validationText (U @(0x49, 0x73, 0x44, 0x6F, 0x63, 0x6B, 0x65, 0x72, 0x41, 0x64, 0x61, 0x70, 0x74, 0x65, 0x72, 0x41, 0x74, 0x74, 0x61, 0x63, 0x68, 0x65, 0x64)) $IsDockerAdapterAttached
$validationText = Set-TableValue $validationText (U @(0x57, 0x65, 0x62, 0x56, 0x69, 0x65, 0x77, 0x43, 0x72, 0x65, 0x61, 0x74, 0x65, 0x43, 0x6F, 0x75, 0x6E, 0x74)) $WebViewCreateCount
$validationText = Set-TableValue $validationText (U @(0x6CE8, 0x518C, 0x20, 0x6D, 0x61, 0x6E, 0x69, 0x66, 0x65, 0x73, 0x74, 0x20, 0x8DEF, 0x5F84)) $RegistrationManifestPath
$validationText = Set-TableValue $validationText (U @(0x6CE8, 0x518C, 0x786E, 0x8BA4, 0x8BB0, 0x5F55, 0x8DEF, 0x5F84)) $registrationPath

$registrationText = Set-TableValue $registrationText (U @(0x786E, 0x8BA4, 0x65E5, 0x671F)) $generatedAt
$registrationText = Set-TableValue $registrationText (U @(0x786E, 0x8BA4, 0x4EBA, 0x5458)) $userName
$registrationText = Set-TableValue $registrationText (U @(0x6D4B, 0x8BD5, 0x673A, 0x5668)) $machineName
$registrationText = Set-TableValue $registrationText (U @(0x57, 0x69, 0x6E, 0x64, 0x6F, 0x77, 0x73, 0x20, 0x7248, 0x672C)) $windowsVersion
$registrationText = Set-TableValue $registrationText (U @(0x43, 0x6F, 0x72, 0x65, 0x6C, 0x44, 0x52, 0x41, 0x57, 0x20, 0x7248, 0x672C)) $CorelDrawVersion
$registrationText = Set-TableValue $registrationText (U @(0x43, 0x6F, 0x72, 0x65, 0x6C, 0x44, 0x52, 0x41, 0x57, 0x20, 0x7248, 0x672C, 0x6807, 0x8BC6)) $CorelVersionIdentifier
$registrationText = Set-TableValue $registrationText (U @(0x51, 0x69, 0x54, 0x75, 0x43, 0x44, 0x52, 0x20, 0x7248, 0x672C)) $version
$registrationText = Set-TableValue $registrationText (U @(0x6CE8, 0x518C, 0x8BA1, 0x5212, 0x20, 0x4A, 0x53, 0x4F, 0x4E, 0x20, 0x62A5, 0x544A)) $registrationPlanJson
$registrationText = Set-TableValue $registrationText (U @(0x6CE8, 0x518C, 0x8BA1, 0x5212, 0x20, 0x4D, 0x61, 0x72, 0x6B, 0x64, 0x6F, 0x77, 0x6E, 0x20, 0x62A5, 0x544A)) $registrationPlanMarkdown

$validationText | Set-Content -LiteralPath $validationPath -Encoding UTF8
$registrationText | Set-Content -LiteralPath $registrationPath -Encoding UTF8

Write-Host "QiTuCDR real host validation records created." -ForegroundColor Green
Write-Host "ValidationRecord: $validationPath"
Write-Host "RegistrationConfirmation: $registrationPath"
if (-not [string]::IsNullOrWhiteSpace($registrationPlanJson)) {
    Write-Host "RegistrationPlanJson: $registrationPlanJson"
}

if (-not [string]::IsNullOrWhiteSpace($registrationPlanMarkdown)) {
    Write-Host "RegistrationPlanMarkdown: $registrationPlanMarkdown"
}
