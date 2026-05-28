param(
    [string]$PackagePath,
    [string]$OutputDirectory,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$tempRoot = $null

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

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

function Resolve-PackageRoot {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Package path does not exist: $fullPath"
    }

    $item = Get-Item -LiteralPath $fullPath
    if ($item.PSIsContainer) {
        return [pscustomobject]@{
            Root = $item.FullName
            Source = $item.FullName
            IsZip = $false
            TempRoot = $null
        }
    }

    if ($item.Extension -ne ".zip") {
        throw "Package path must be a directory or .zip file: $fullPath"
    }

    $temp = Join-Path $env:TEMP ("qitucdr-real-host-readiness-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    Expand-Archive -LiteralPath $item.FullName -DestinationPath $temp -Force

    return [pscustomobject]@{
        Root = $temp
        Source = $item.FullName
        IsZip = $true
        TempRoot = $temp
    }
}

function Test-RequiredFile {
    param(
        [string]$Root,
        [string]$RelativePath,
        [System.Collections.Generic.List[string]]$Failures
    )

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Failure $Failures "Missing required file: $RelativePath"
        return $false
    }

    return $true
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

function Get-CorelTypeLibCount {
    $paths = @(
        "C:\Program Files\Corel\CorelDRAW Graphics Suite\23\Programs64\TypeLibs\CorelDRAW.tlb",
        "C:\Program Files\Corel\CorelDRAW Graphics Suite\24\Programs64\TypeLibs\CorelDRAW.tlb",
        "C:\Program Files\Corel\CorelDRAW Graphics Suite\25\Programs64\TypeLibs\CorelDRAW.tlb",
        "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64\TypeLibs\CorelDRAW.tlb",
        "C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Programs64\TypeLibs\CorelDRAW.tlb"
    )

    return @($paths | Where-Object { Test-Path -LiteralPath $_ }).Count
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

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$resolved = Resolve-PackageRoot $PackagePath
$tempRoot = $resolved.TempRoot
$root = $resolved.Root
$failures = New-Object "System.Collections.Generic.List[string]"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$smokeRoot = Join-Path $OutputDirectory "readiness-smoke-$timestamp"
New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null

$requiredFiles = @(
    "App\QiTuCDR.Host.dll",
    "App\WebUI\index.html",
    "installer\Install-QiTuCDR.ps1",
    "installer\Uninstall-QiTuCDR.ps1",
    "installer\Get-QiTuCorelRegistrationPlan.ps1",
    "installer\Get-QiTuCorelRegistrationPreview.ps1",
    "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1",
    "installer\Test-QiTuCorelRegistrationManifest.ps1",
    "tools\validation\New-QiTuRealHostExecutionPlan.ps1",
    "tools\validation\New-QiTuRealHostValidationRecord.ps1",
    "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md",
    "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md",
    "docs\REAL_HOST_VALIDATION_TEMPLATE.md",
    "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md",
    "package-manifest.json",
    "SHA256SUMS.txt"
)

try {
    foreach ($relative in $requiredFiles) {
        Test-RequiredFile $root $relative $failures | Out-Null
    }

    $executionPlanTool = Join-Path $root "tools\validation\New-QiTuRealHostExecutionPlan.ps1"
    if (Test-Path -LiteralPath $executionPlanTool) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $executionPlanTool `
            -OutputDirectory $smokeRoot `
            -CorelDrawVersion "CorelDRAW Readiness" `
            -CorelVersionIdentifier 27 *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Execution plan generator failed with exit code $LASTEXITCODE."
        }
    }

    $validationRecordTool = Join-Path $root "tools\validation\New-QiTuRealHostValidationRecord.ps1"
    if (Test-Path -LiteralPath $validationRecordTool) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $validationRecordTool `
            -OutputDirectory $smokeRoot `
            -CorelDrawVersion "CorelDRAW Readiness" `
            -CorelVersionIdentifier 27 `
            -SkipRegistrationPlan *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Validation record generator failed with exit code $LASTEXITCODE."
        }
    }

    if (-not (Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-real-host-execution-plan-*.md" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Add-Failure $failures "Execution plan smoke output was not created."
    }

    if (-not (Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-real-host-validation-*.md" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Add-Failure $failures "Validation record smoke output was not created."
    }

    if (-not (Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-registration-confirmation-*.md" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Add-Failure $failures "Registration confirmation smoke output was not created."
    }

    $webView2Version = Get-WebView2RuntimeVersion
    $corelTypeLibCount = Get-CorelTypeLibCount
    if ($webView2Version -eq "UNKNOWN") {
        Add-Failure $failures "WebView2 Runtime was not detected on this machine."
    }

    if ($corelTypeLibCount -eq 0) {
        Add-Failure $failures "No CorelDRAW TypeLib was detected on this machine."
    }

    $status = if ($failures.Count -eq 0) { "READY_FOR_MANUAL_HOST_VALIDATION" } else { "BLOCKED" }
    $report = [pscustomobject]@{
        Timestamp = (Get-Date).ToString("o")
        PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
        PackageRoot = $root
        PackageIsZip = [bool]$resolved.IsZip
        OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
        SmokeOutputDirectory = [System.IO.Path]::GetFullPath($smokeRoot)
        WebView2RuntimeVersion = $webView2Version
        CorelDrawTypeLibCount = $corelTypeLibCount
        RequiredFiles = $requiredFiles
        FatalFailures = @($failures)
        Status = $status
    }

    $jsonPath = Join-Path $OutputDirectory "qitucdr-real-host-readiness-$timestamp.json"
    $markdownPath = Join-Path $OutputDirectory "qitucdr-real-host-readiness-$timestamp.md"

    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    $lines = New-Object "System.Collections.Generic.List[string]"
    $lines.Add("# QiTuCDR Real Host Readiness") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("Status: $status") | Out-Null
    $lines.Add("PackagePath: $($report.PackagePath)") | Out-Null
    $lines.Add("SmokeOutputDirectory: $($report.SmokeOutputDirectory)") | Out-Null
    $lines.Add("WebView2RuntimeVersion: $webView2Version") | Out-Null
    $lines.Add("CorelDrawTypeLibCount: $corelTypeLibCount") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("## Required Files") | Out-Null
    foreach ($relative in $requiredFiles) {
        $exists = Test-Path -LiteralPath (Join-Path $root $relative)
        $lines.Add("- $relative : $exists") | Out-Null
    }
    $lines.Add("") | Out-Null
    $lines.Add("## Failures") | Out-Null
    if ($failures.Count -eq 0) {
        $lines.Add("- None") | Out-Null
    }
    else {
        foreach ($failure in $failures) {
            $lines.Add("- $failure") | Out-Null
        }
    }

    $lines | Set-Content -LiteralPath $markdownPath -Encoding UTF8

    if ($Json) {
        $report | ConvertTo-Json -Depth 6
        if ($FailOnError -and $failures.Count -gt 0) {
            exit 1
        }

        exit 0
    }

    Write-Host "QiTuCDR real host readiness" -ForegroundColor Cyan
    Write-Host "PackagePath: $PackagePath"
    Write-Host "ReportJson: $jsonPath"
    Write-Host "ReportMarkdown: $markdownPath"
    Write-Host "SmokeOutputDirectory: $smokeRoot"
    Write-Host "WebView2RuntimeVersion: $webView2Version"
    Write-Host "CorelDrawTypeLibCount: $corelTypeLibCount"
    Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "READY_FOR_MANUAL_HOST_VALIDATION") { "Green" } else { "Yellow" })

    if ($failures.Count -gt 0) {
        Write-Host "Required actions:" -ForegroundColor Yellow
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }

    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($tempRoot) -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
