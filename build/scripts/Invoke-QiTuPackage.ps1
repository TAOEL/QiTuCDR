param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$SourcePath,
    [string]$OutputDirectory,
    [switch]$SkipPrerequisites,
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Assert-ChildPath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'

    if (-not $pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside root: $Path"
    }
}

function Copy-DirectoryContents {
    param(
        [string]$From,
        [string]$To
    )

    New-Item -ItemType Directory -Force -Path $To | Out-Null
    Get-ChildItem -LiteralPath $From -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $To -Recurse -Force
    }
}

function Copy-FileIfExists {
    param(
        [string]$From,
        [string]$To
    )

    if (Test-Path -LiteralPath $From) {
        $directory = Split-Path -Parent $To
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            New-Item -ItemType Directory -Force -Path $directory | Out-Null
        }

        Copy-Item -LiteralPath $From -Destination $To -Force
    }
}

function Get-PackageVersion {
    param([string]$ExplicitVersion)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitVersion)) {
        return $ExplicitVersion.Trim()
    }

    $versionFile = Join-Path $repoRoot "VERSION"
    if (-not (Test-Path -LiteralPath $versionFile)) {
        throw "VERSION file is missing and -Version was not provided."
    }

    $fileVersion = (Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8).Trim()
    if ([string]::IsNullOrWhiteSpace($fileVersion)) {
        throw "VERSION file is empty."
    }

    return $fileVersion
}

$Version = Get-PackageVersion $Version

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repoRoot "src\Host\bin\$Configuration\net48"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\release"
}

$sourceFull = [System.IO.Path]::GetFullPath($SourcePath)
$outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$packageId = "qitucdr-v$Version-$timestamp"
$stageRoot = Join-Path $outputFull $packageId
$appStage = Join-Path $stageRoot "App"
$installerStage = Join-Path $stageRoot "installer"
$toolsStage = Join-Path $stageRoot "tools"
$docsStage = Join-Path $stageRoot "docs"
$manifestPath = Join-Path $stageRoot "package-manifest.json"
$checksumPath = Join-Path $stageRoot "SHA256SUMS.txt"
$zipPath = Join-Path $outputFull "$packageId.zip"

$hostDll = Join-Path $sourceFull "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $sourceFull "WebUI\index.html"

if (-not $SkipPrerequisites) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "installer\Test-QiTuInstallPrerequisites.ps1") -SourcePath $sourceFull -Configuration $Configuration -FailOnError | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Install prerequisites failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $hostDll)) {
    throw "QiTuCDR.Host.dll is missing: $hostDll"
}

if (-not (Test-Path -LiteralPath $webUiIndex)) {
    throw "WebUI index is missing: $webUiIndex"
}

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
if (Test-Path -LiteralPath $stageRoot) {
    Assert-ChildPath $outputFull $stageRoot
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $stageRoot, $docsStage | Out-Null
Copy-DirectoryContents $sourceFull $appStage
Copy-DirectoryContents (Join-Path $repoRoot "installer") $installerStage
Copy-DirectoryContents (Join-Path $repoRoot "tools\validation") (Join-Path $toolsStage "validation")

Copy-FileIfExists (Join-Path $repoRoot "README.md") (Join-Path $stageRoot "README.md")
Copy-FileIfExists (Join-Path $repoRoot "VERSION") (Join-Path $stageRoot "VERSION")
Copy-FileIfExists (Join-Path $repoRoot "CHANGELOG.md") (Join-Path $stageRoot "CHANGELOG.md")
Copy-FileIfExists (Join-Path $repoRoot "PRD.md") (Join-Path $stageRoot "PRD.md")
Copy-FileIfExists (Join-Path $repoRoot "AGENTS.md") (Join-Path $stageRoot "AGENTS.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\RELEASE_CHECKLIST.md") (Join-Path $docsStage "RELEASE_CHECKLIST.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\MILESTONES.md") (Join-Path $docsStage "MILESTONES.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\STABILITY_TEST_PLAN.md") (Join-Path $docsStage "STABILITY_TEST_PLAN.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md") (Join-Path $docsStage "REAL_HOST_ACCEPTANCE_QUICKSTART.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md") (Join-Path $docsStage "REAL_HOST_EXECUTION_PLAN_TEMPLATE.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md") (Join-Path $docsStage "REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\REAL_HOST_VALIDATION_TEMPLATE.md") (Join-Path $docsStage "REAL_HOST_VALIDATION_TEMPLATE.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md") (Join-Path $docsStage "CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\CORELDRAW_SDK_INTEGRATION.md") (Join-Path $docsStage "CORELDRAW_SDK_INTEGRATION.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\CORELDRAW_HOST_BINDING_CHECKLIST.md") (Join-Path $docsStage "CORELDRAW_HOST_BINDING_CHECKLIST.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md") (Join-Path $docsStage "CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md")
Copy-FileIfExists (Join-Path $repoRoot "docs\CORELDRAW_ADDONS_MOUNT_REFERENCE.md") (Join-Path $docsStage "CORELDRAW_ADDONS_MOUNT_REFERENCE.md")

$files = Get-ChildItem -LiteralPath $stageRoot -Recurse -File | Sort-Object FullName
$checksumLines = New-Object "System.Collections.Generic.List[string]"
$manifestFiles = foreach ($file in $files) {
    $relative = $file.FullName.Substring($stageRoot.Length).TrimStart('\')
    $hash = Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256
    $checksumLines.Add("$($hash.Hash.ToLowerInvariant())  $relative")

    [pscustomobject]@{
        Path = $relative
        Length = $file.Length
        Sha256 = $hash.Hash.ToLowerInvariant()
    }
}

$checksumLines | Set-Content -LiteralPath $checksumPath -Encoding UTF8

$manifest = [ordered]@{
    Product = "QiTuCDR"
    PackageId = $packageId
    Version = $Version
    Configuration = $Configuration
    CreatedAt = (Get-Date).ToString("o")
    SourcePath = $sourceFull
    StageRoot = $stageRoot
    HostAssembly = "App\QiTuCDR.Host.dll"
    WebUiIndex = "App\WebUI\index.html"
    Installer = "installer\Install-QiTuCDR.ps1"
    Uninstaller = "installer\Uninstall-QiTuCDR.ps1"
    ValidationRecordGenerator = "tools\validation\New-QiTuRealHostValidationRecord.ps1"
    RuntimeSafety = [ordered]@{
        DefaultDockHostMode = "Debug"
        CorelDockerStatus = "PlaceholderFallbackRequired"
        SingleWebViewRequired = $true
        RealCorelDrawValidationRequired = $true
        OfficialCorelDockerAdapterDefaultEnabled = $false
    }
    FileCount = @($manifestFiles).Count
    Files = @($manifestFiles)
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

if (-not $NoZip) {
    if (Test-Path -LiteralPath $zipPath) {
        Assert-ChildPath $outputFull $zipPath
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $stageRoot "*") -DestinationPath $zipPath -Force
}

Write-Host "QiTuCDR package completed." -ForegroundColor Green
Write-Host "StageRoot: $stageRoot"
Write-Host "Manifest: $manifestPath"
Write-Host "Checksums: $checksumPath"
if (-not $NoZip) {
    Write-Host "Zip: $zipPath"
}
