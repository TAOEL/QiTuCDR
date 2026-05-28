param(
    [string]$OutputDirectory,
    [string]$CorelDrawVersion = "CorelDRAW 2026",
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

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\validation"
}

if ([string]::IsNullOrWhiteSpace($ConfirmedManifestPath)) {
    $ConfirmedManifestPath = Join-Path $repoRoot "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json"
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $InstallRoot = Join-Path $localAppData "QiTuCDR"
}

$templatePath = Join-Path $repoRoot "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md"
if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Execution plan template is missing: $templatePath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$planPath = Join-Path $OutputDirectory "qitucdr-real-host-execution-plan-$timestamp.md"
$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$planText = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8
$planText = $planText.Replace("__GENERATED_AT__", $generatedAt)
$planText = $planText.Replace("__CORELDRAW_VERSION__", $CorelDrawVersion)
$planText = $planText.Replace("__COREL_VERSION_IDENTIFIER__", $CorelVersionIdentifier)
$planText = $planText.Replace("__REGISTRATION_KIND__", $RegistrationKind)
$planText = $planText.Replace("__CONFIRMED_REGISTRY_PATH__", $ConfirmedRegistryPath)
$planText = $planText.Replace("__CONFIRMED_MANIFEST_PATH__", $ConfirmedManifestPath)
$planText = $planText.Replace("__INSTALL_ROOT__", $InstallRoot)

$planText | Set-Content -LiteralPath $planPath -Encoding UTF8

Write-Host "QiTuCDR real host execution plan created." -ForegroundColor Green
Write-Host "PlanPath: $planPath"
