param(
    [string]$OutputPath,
    [string]$CorelVersionIdentifier,
    [string[]]$TargetCorelVersions = @("23", "24", "25", "26", "27"),
    [string]$ProductLabel = "QiTuCDR",
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

function Assert-RequiredText {
    param(
        [string]$Value,
        [string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }
}

function Test-SafeCorelRegistryPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    return $Path -match "^HK(CU|LM):\\Software\\(WOW6432Node\\)?Corel\\"
}

Assert-RequiredText $CorelVersionIdentifier "CorelVersionIdentifier"
Assert-RequiredText $RegistryPath "RegistryPath"
Assert-RequiredText $ConfirmationSource "ConfirmationSource"

if ([string]::IsNullOrWhiteSpace($ConfirmedBy)) {
    $ConfirmedBy = $env:USERNAME
}

if ([string]::IsNullOrWhiteSpace($ConfirmedAt)) {
    $ConfirmedAt = (Get-Date).ToString("o")
}

if ($RegistrationKind -ne "AddIn" -and $RegistrationKind -ne "Docker") {
    throw "RegistrationKind must be AddIn or Docker."
}

if (-not (Test-SafeCorelRegistryPath $RegistryPath)) {
    throw "RegistryPath must be under HKCU:\Software\Corel\ or HKLM:\Software\Corel\."
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $repoRoot "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-$CorelVersionIdentifier-$timestamp.json"
}

$manifestGenerator = Join-Path $PSScriptRoot "New-QiTuCorelRegistrationManifest.ps1"
$manifestValidator = Join-Path $PSScriptRoot "Test-QiTuCorelRegistrationManifest.ps1"

& $manifestGenerator `
    -OutputPath $OutputPath `
    -TargetCorelVersions $TargetCorelVersions `
    -Status CONFIRMED `
    -EnableCorelVersionIdentifier $CorelVersionIdentifier `
    -ProductLabel $ProductLabel `
    -RegistrationKind $RegistrationKind `
    -RegistryPath $RegistryPath `
    -ConfirmationSource $ConfirmationSource `
    -ConfirmedBy $ConfirmedBy `
    -ConfirmedAt $ConfirmedAt | Out-Host

& $manifestValidator `
    -ManifestPath $OutputPath `
    -RequireConfirmed `
    -FailOnError | Out-Host

Write-Host "QiTuCDR confirmed CorelDRAW registration manifest is ready." -ForegroundColor Green
Write-Host "OutputPath: $OutputPath"
Write-Host "CorelVersionIdentifier: $CorelVersionIdentifier"
Write-Host "RegistrationKind: $RegistrationKind"
Write-Host "RegistryPath: $RegistryPath"
