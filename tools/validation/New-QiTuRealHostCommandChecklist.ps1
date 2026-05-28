param(
    [string]$AcceptanceKitDirectory,
    [string]$PackagePath,
    [string]$OutputPath,
    [string]$CorelVersionIdentifier = "27",
    [string]$RegistrationKind = "AddIn",
    [string]$ConfirmedRegistryPath = "HKCU:\Software\Corel\...",
    [string]$ConfirmedManifestPath,
    [string]$InstallRoot
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Get-LatestAcceptanceKitDirectory {
    $validationRoot = Join-Path $repoRoot "artifacts\validation"
    if (-not (Test-Path -LiteralPath $validationRoot)) {
        return $null
    }

    $kit = Get-ChildItem -LiteralPath $validationRoot -Directory -Filter "real-host-acceptance-kit-*" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($kit) {
        return $kit.FullName
    }

    return $null
}

function Get-DefaultPackagePath {
    if (Test-Path -LiteralPath (Join-Path $repoRoot "package-manifest.json")) {
        return [string]$repoRoot
    }

    $releaseRoot = Join-Path $repoRoot "artifacts\release"
    if (-not (Test-Path -LiteralPath $releaseRoot)) {
        return ""
    }

    $zip = Get-ChildItem -LiteralPath $releaseRoot -Filter "qitucdr-v*.zip" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($zip) {
        return $zip.FullName
    }

    return ""
}

function Get-LatestFile {
    param(
        [string]$Directory,
        [string]$Filter
    )

    if (-not (Test-Path -LiteralPath $Directory)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $Directory -Filter $Filter -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($AcceptanceKitDirectory)) {
    $AcceptanceKitDirectory = Get-LatestAcceptanceKitDirectory
}

if ([string]::IsNullOrWhiteSpace($AcceptanceKitDirectory)) {
    throw "AcceptanceKitDirectory was not provided and no real-host-acceptance-kit directory was found."
}

$kitFull = [System.IO.Path]::GetFullPath($AcceptanceKitDirectory)
if (-not (Test-Path -LiteralPath $kitFull)) {
    throw "Acceptance kit directory does not exist: $kitFull"
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Get-DefaultPackagePath
}

if ([string]::IsNullOrWhiteSpace($ConfirmedManifestPath)) {
    $ConfirmedManifestPath = Join-Path $repoRoot "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-$CorelVersionIdentifier.json"
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $InstallRoot = Join-Path $localAppData "QiTuCDR"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $kitFull "qitucdr-real-host-command-checklist-$timestamp.md"
}

$templatePath = Join-Path $repoRoot "docs\REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md"
if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Command checklist template is missing: $templatePath"
}

$kitIndex = Join-Path $kitFull "README.md"
$executionPlan = Get-LatestFile $kitFull "qitucdr-real-host-execution-plan-*.md"
$validationRecord = Get-LatestFile $kitFull "qitucdr-real-host-validation-*.md"
$executionPlanPath = ""
$validationRecordPath = ""
if ($executionPlan) {
    $executionPlanPath = $executionPlan.FullName
}

if ($validationRecord) {
    $validationRecordPath = $validationRecord.FullName
}

$registrationPlanOutputDirectory = Join-Path $kitFull "registration-plan-manual"

$text = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8
$text = $text.Replace("__GENERATED_AT__", (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
$text = $text.Replace("__PACKAGE_PATH__", $PackagePath)
$text = $text.Replace("__KIT_DIRECTORY__", $kitFull)
$text = $text.Replace("__COREL_VERSION_IDENTIFIER__", $CorelVersionIdentifier)
$text = $text.Replace("__REGISTRATION_KIND__", $RegistrationKind)
$text = $text.Replace("__CONFIRMED_REGISTRY_PATH__", $ConfirmedRegistryPath)
$text = $text.Replace("__CONFIRMED_MANIFEST_PATH__", $ConfirmedManifestPath)
$text = $text.Replace("__INSTALL_ROOT__", $InstallRoot)
$text = $text.Replace("__KIT_INDEX_PATH__", $kitIndex)
$text = $text.Replace("__EXECUTION_PLAN_PATH__", $executionPlanPath)
$text = $text.Replace("__VALIDATION_RECORD_PATH__", $validationRecordPath)
$text = $text.Replace("__REGISTRATION_PLAN_OUTPUT_DIRECTORY__", $registrationPlanOutputDirectory)

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$text | Set-Content -LiteralPath $OutputPath -Encoding UTF8

Write-Host "QiTuCDR real host command checklist created." -ForegroundColor Green
Write-Host "Checklist: $OutputPath"
