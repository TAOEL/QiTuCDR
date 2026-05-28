param(
    [string]$PackagePath,
    [string]$OutputDirectory,
    [string]$CorelDrawVersion = "CorelDRAW 2026",
    [string]$CorelVersionIdentifier = "27",
    [string]$RegistrationKind = "AddIn",
    [string]$ConfirmedRegistryPath = "HKCU:\Software\Corel\...",
    [string]$ConfirmedManifestPath,
    [string]$InstallRoot,
    [switch]$SkipRegistrationPlan,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Get-DefaultPackagePath {
    if (Test-Path -LiteralPath (Join-Path $repoRoot "package-manifest.json")) {
        return [string]$repoRoot
    }

    $releaseRoot = Join-Path $repoRoot "artifacts\release"
    if (-not (Test-Path -LiteralPath $releaseRoot)) {
        return $null
    }

    $zip = Get-ChildItem -LiteralPath $releaseRoot -Filter "qitucdr-v*.zip" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($zip) {
        return $zip.FullName
    }

    return $null
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

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Get-DefaultPackagePath
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    throw "PackagePath was not provided and no release package was found."
}

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

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$kitRoot = Join-Path $OutputDirectory "real-host-acceptance-kit-$timestamp"
New-Item -ItemType Directory -Force -Path $kitRoot | Out-Null

$readinessTool = Join-Path $PSScriptRoot "Test-QiTuRealHostReadiness.ps1"
$executionPlanTool = Join-Path $PSScriptRoot "New-QiTuRealHostExecutionPlan.ps1"
$validationRecordTool = Join-Path $PSScriptRoot "New-QiTuRealHostValidationRecord.ps1"
$commandChecklistTool = Join-Path $PSScriptRoot "New-QiTuRealHostCommandChecklist.ps1"

foreach ($tool in @($readinessTool, $executionPlanTool, $validationRecordTool)) {
    if (-not (Test-Path -LiteralPath $tool)) {
        throw "Required validation tool is missing: $tool"
    }
}

$readinessJsonText = & powershell -NoProfile -ExecutionPolicy Bypass -File $readinessTool `
    -PackagePath $PackagePath `
    -OutputDirectory $kitRoot `
    -Json

if ($LASTEXITCODE -ne 0) {
    throw "Test-QiTuRealHostReadiness.ps1 failed with exit code $LASTEXITCODE."
}

$readiness = $readinessJsonText | ConvertFrom-Json

& powershell -NoProfile -ExecutionPolicy Bypass -File $executionPlanTool `
    -OutputDirectory $kitRoot `
    -CorelDrawVersion $CorelDrawVersion `
    -CorelVersionIdentifier $CorelVersionIdentifier `
    -RegistrationKind $RegistrationKind `
    -ConfirmedRegistryPath $ConfirmedRegistryPath `
    -ConfirmedManifestPath $ConfirmedManifestPath `
    -InstallRoot $InstallRoot *> $null

if ($LASTEXITCODE -ne 0) {
    throw "New-QiTuRealHostExecutionPlan.ps1 failed with exit code $LASTEXITCODE."
}

$validationArgs = @(
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $validationRecordTool,
    "-OutputDirectory",
    $kitRoot,
    "-CorelDrawVersion",
    $CorelDrawVersion,
    "-CorelVersionIdentifier",
    $CorelVersionIdentifier,
    "-RegistrationManifestPath",
    $ConfirmedManifestPath
)

if ($SkipRegistrationPlan) {
    $validationArgs += "-SkipRegistrationPlan"
}

& powershell @validationArgs *> $null
if ($LASTEXITCODE -ne 0) {
    throw "New-QiTuRealHostValidationRecord.ps1 failed with exit code $LASTEXITCODE."
}

$readinessMarkdown = Get-LatestFile $kitRoot "qitucdr-real-host-readiness-*.md"
$executionPlan = Get-LatestFile $kitRoot "qitucdr-real-host-execution-plan-*.md"
$validationRecord = Get-LatestFile $kitRoot "qitucdr-real-host-validation-*.md"
$registrationConfirmation = Get-LatestFile $kitRoot "qitucdr-registration-confirmation-*.md"
$commandChecklist = $null

if (Test-Path -LiteralPath $commandChecklistTool) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $commandChecklistTool `
        -AcceptanceKitDirectory $kitRoot `
        -PackagePath $PackagePath `
        -CorelVersionIdentifier $CorelVersionIdentifier `
        -RegistrationKind $RegistrationKind `
        -ConfirmedRegistryPath $ConfirmedRegistryPath `
        -ConfirmedManifestPath $ConfirmedManifestPath `
        -InstallRoot $InstallRoot *> $null

    if ($LASTEXITCODE -ne 0) {
        throw "New-QiTuRealHostCommandChecklist.ps1 failed with exit code $LASTEXITCODE."
    }

    $commandChecklist = Get-LatestFile $kitRoot "qitucdr-real-host-command-checklist-*.md"
}

$indexPath = Join-Path $kitRoot "README.md"
$indexLines = New-Object "System.Collections.Generic.List[string]"
$indexLines.Add("# QiTuCDR Real Host Acceptance Kit") | Out-Null
$indexLines.Add("") | Out-Null
$indexLines.Add("GeneratedAt: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")") | Out-Null
$indexLines.Add("PackagePath: $([System.IO.Path]::GetFullPath($PackagePath))") | Out-Null
$indexLines.Add("ReadinessStatus: $($readiness.Status)") | Out-Null
$indexLines.Add("CorelDrawVersion: $CorelDrawVersion") | Out-Null
$indexLines.Add("CorelVersionIdentifier: $CorelVersionIdentifier") | Out-Null
$indexLines.Add("") | Out-Null
$indexLines.Add("## Files") | Out-Null
$indexLines.Add("- Readiness report: $($readinessMarkdown.FullName)") | Out-Null
$indexLines.Add("- Execution plan: $($executionPlan.FullName)") | Out-Null
$indexLines.Add("- Command checklist: $($commandChecklist.FullName)") | Out-Null
$indexLines.Add("- Validation record draft: $($validationRecord.FullName)") | Out-Null
$indexLines.Add("- Registration confirmation draft: $($registrationConfirmation.FullName)") | Out-Null
$indexLines.Add("") | Out-Null
$indexLines.Add("## Next Steps") | Out-Null
$indexLines.Add("1. Open the execution plan first.") | Out-Null
$indexLines.Add("2. Confirm the real CorelDRAW registration path before writing registry values.") | Out-Null
$indexLines.Add("3. Generate a CONFIRMED registration manifest only after the path is verified.") | Out-Null
$indexLines.Add("4. Preview registration writes before installing into CorelDRAW.") | Out-Null
$indexLines.Add("5. Fill the validation record only with real CorelDRAW host results.") | Out-Null
$indexLines | Set-Content -LiteralPath $indexPath -Encoding UTF8

$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
    KitRoot = [System.IO.Path]::GetFullPath($kitRoot)
    ReadinessStatus = [string]$readiness.Status
    ReadinessReport = if ($readinessMarkdown) { $readinessMarkdown.FullName } else { $null }
    ExecutionPlan = if ($executionPlan) { $executionPlan.FullName } else { $null }
    CommandChecklist = if ($commandChecklist) { $commandChecklist.FullName } else { $null }
    ValidationRecord = if ($validationRecord) { $validationRecord.FullName } else { $null }
    RegistrationConfirmation = if ($registrationConfirmation) { $registrationConfirmation.FullName } else { $null }
    Index = $indexPath
}

if ($Json) {
    $result | ConvertTo-Json -Depth 5
    if ($FailOnError -and $readiness.Status -ne "READY_FOR_MANUAL_HOST_VALIDATION") {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR real host acceptance kit created." -ForegroundColor Green
Write-Host "KitRoot: $kitRoot"
Write-Host "Index: $indexPath"
Write-Host "ReadinessStatus: $($readiness.Status)" -ForegroundColor $(if ($readiness.Status -eq "READY_FOR_MANUAL_HOST_VALIDATION") { "Green" } else { "Yellow" })
if ($readinessMarkdown) {
    Write-Host "ReadinessReport: $($readinessMarkdown.FullName)"
}
if ($executionPlan) {
    Write-Host "ExecutionPlan: $($executionPlan.FullName)"
}
if ($commandChecklist) {
    Write-Host "CommandChecklist: $($commandChecklist.FullName)"
}
if ($validationRecord) {
    Write-Host "ValidationRecord: $($validationRecord.FullName)"
}
if ($registrationConfirmation) {
    Write-Host "RegistrationConfirmation: $($registrationConfirmation.FullName)"
}

if ($FailOnError -and $readiness.Status -ne "READY_FOR_MANUAL_HOST_VALIDATION") {
    exit 1
}
