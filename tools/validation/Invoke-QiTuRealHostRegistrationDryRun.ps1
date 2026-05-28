param(
    [string]$SourcePath,
    [string]$InstallRoot,
    [string]$OutputDirectory,
    [string]$CorelVersionIdentifier = "27",
    [string]$RegistrationKind = "AddIn",
    [string]$RegistryPath,
    [string]$ProductLabel = "QiTuCDR",
    [string]$ConfirmationSource = "real CorelDRAW host validation dry run",
    [string]$ConfirmedBy,
    [string]$ManifestPath,
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

function Get-DefaultSourcePath {
    $packageApp = Join-Path $repoRoot "App"
    if (Test-Path -LiteralPath (Join-Path $packageApp "QiTuCDR.Host.dll")) {
        return $packageApp
    }

    $releaseHost = Join-Path $repoRoot "src\Host\bin\Release\net48"
    if (Test-Path -LiteralPath (Join-Path $releaseHost "QiTuCDR.Host.dll")) {
        return $releaseHost
    }

    return (Join-Path $repoRoot "src\Host\bin\Debug\net48")
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

if ([string]::IsNullOrWhiteSpace($RegistryPath)) {
    throw "RegistryPath is required. Use a manually confirmed CorelDRAW registration path."
}

if (-not (Test-SafeCorelRegistryPath $RegistryPath)) {
    throw "RegistryPath must be under HKCU:\Software\Corel\ or HKLM:\Software\Corel\."
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Get-DefaultSourcePath
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Get-DefaultInstallRoot
}

if ([string]::IsNullOrWhiteSpace($ConfirmedBy)) {
    $ConfirmedBy = $env:USERNAME
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\validation"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$dryRunRoot = Join-Path $OutputDirectory "registration-dry-run-$timestamp"
New-Item -ItemType Directory -Force -Path $dryRunRoot | Out-Null

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $dryRunRoot "qitucdr-coreldraw-registration-manifest.confirmed-$CorelVersionIdentifier.json"
}

$sourceFull = [System.IO.Path]::GetFullPath($SourcePath)
$installRootFull = [System.IO.Path]::GetFullPath($InstallRoot)
$manifestFull = [System.IO.Path]::GetFullPath($ManifestPath)
$hostDll = Join-Path $sourceFull "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $sourceFull "WebUI\index.html"
$failures = New-Object "System.Collections.Generic.List[string]"

if (-not (Test-Path -LiteralPath $hostDll)) {
    Add-Failure $failures "QiTuCDR.Host.dll is missing: $hostDll"
}

if (-not (Test-Path -LiteralPath $webUiIndex)) {
    Add-Failure $failures "WebUI index is missing: $webUiIndex"
}

if ($failures.Count -eq 0) {
    $manifestGenerator = Join-Path $repoRoot "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1"
    $previewTool = Join-Path $repoRoot "installer\Get-QiTuCorelRegistrationPreview.ps1"
    $installTool = Join-Path $repoRoot "installer\Install-QiTuCDR.ps1"

    foreach ($tool in @($manifestGenerator, $previewTool, $installTool)) {
        if (-not (Test-Path -LiteralPath $tool)) {
            Add-Failure $failures "Required tool is missing: $tool"
        }
    }
}

if ($failures.Count -eq 0) {
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1") `
            -OutputPath $manifestFull `
            -CorelVersionIdentifier $CorelVersionIdentifier `
            -ProductLabel $ProductLabel `
            -RegistrationKind $RegistrationKind `
            -RegistryPath $RegistryPath `
            -ConfirmationSource $ConfirmationSource `
            -ConfirmedBy $ConfirmedBy *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Confirmed registration manifest generation failed with exit code $LASTEXITCODE."
        }
    }
    catch {
        Add-Failure $failures "Confirmed registration manifest generation threw an exception: $($_.Exception.Message)"
    }
}

$previewJsonPath = Join-Path $dryRunRoot "qitucdr-coreldraw-registration-preview-$timestamp.json"
$previewMarkdownPath = Join-Path $dryRunRoot "qitucdr-coreldraw-registration-dry-run-$timestamp.md"
$preview = $null

if ($failures.Count -eq 0) {
    try {
        $previewJson = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "installer\Get-QiTuCorelRegistrationPreview.ps1") `
            -ManifestPath $manifestFull `
            -InstallRoot $installRootFull `
            -Json `
            -FailOnError

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Registration preview JSON failed with exit code $LASTEXITCODE."
        }
        else {
            $previewJson | Set-Content -LiteralPath $previewJsonPath -Encoding UTF8
            $preview = $previewJson | ConvertFrom-Json
        }
    }
    catch {
        Add-Failure $failures "Registration preview JSON threw an exception: $($_.Exception.Message)"
    }
}

if ($failures.Count -eq 0) {
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "installer\Install-QiTuCDR.ps1") `
            -SourcePath $sourceFull `
            -InstallRoot $installRootFull `
            -CorelDrawRegistrationManifestPath $manifestFull `
            -PreviewCorelDrawRegistration *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Installer preview failed with exit code $LASTEXITCODE."
        }
    }
    catch {
        Add-Failure $failures "Installer preview threw an exception: $($_.Exception.Message)"
    }
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$wouldWriteCount = 0
if ($preview -ne $null) {
    $wouldWriteCount = [int]$preview.WouldWriteCount
}

$lines = New-Object "System.Collections.Generic.List[string]"
$lines.Add("# QiTuCDR CorelDRAW Registration Dry Run") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("Status: $status") | Out-Null
$lines.Add("GeneratedAt: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")") | Out-Null
$lines.Add("SourcePath: $sourceFull") | Out-Null
$lines.Add("InstallRoot: $installRootFull") | Out-Null
$lines.Add("ManifestPath: $manifestFull") | Out-Null
$lines.Add("PreviewJson: $previewJsonPath") | Out-Null
$lines.Add("CorelVersionIdentifier: $CorelVersionIdentifier") | Out-Null
$lines.Add("RegistrationKind: $RegistrationKind") | Out-Null
$lines.Add("RegistryPath: $RegistryPath") | Out-Null
$lines.Add("WouldWriteCount: $wouldWriteCount") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("## Safety Result") | Out-Null
$lines.Add("") | Out-Null
if ($failures.Count -eq 0) {
    $lines.Add("- Confirmed manifest was generated.") | Out-Null
    $lines.Add("- Structured preview completed.") | Out-Null
    $lines.Add("- Installer preview completed.") | Out-Null
    $lines.Add("- No registry values were written by this dry run.") | Out-Null
}
else {
    foreach ($failure in $failures) {
        $lines.Add("- $failure") | Out-Null
    }
}

$lines | Set-Content -LiteralPath $previewMarkdownPath -Encoding UTF8

$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    SourcePath = $sourceFull
    InstallRoot = $installRootFull
    DryRunRoot = [System.IO.Path]::GetFullPath($dryRunRoot)
    ManifestPath = $manifestFull
    PreviewJson = $previewJsonPath
    PreviewMarkdown = $previewMarkdownPath
    CorelVersionIdentifier = $CorelVersionIdentifier
    RegistrationKind = $RegistrationKind
    RegistryPath = $RegistryPath
    WouldWriteCount = $wouldWriteCount
    FatalFailures = @($failures)
    Status = $status
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW registration dry run" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "DryRunRoot: $dryRunRoot"
Write-Host "ManifestPath: $manifestFull"
Write-Host "PreviewJson: $previewJsonPath"
Write-Host "PreviewMarkdown: $previewMarkdownPath"
Write-Host "WouldWriteCount: $wouldWriteCount"
if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
