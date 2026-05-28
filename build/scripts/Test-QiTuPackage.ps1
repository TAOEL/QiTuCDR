param(
    [string]$PackagePath,
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

    $directory = Get-ChildItem -LiteralPath $releaseRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "qitucdr-v*" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($directory) {
        return $directory.FullName
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

    $temp = Join-Path $env:TEMP ("qitucdr-package-verify-" + [Guid]::NewGuid().ToString("N"))
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

function Test-ValidationToolSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $validationTool = Join-Path $Root "tools\validation\New-QiTuRealHostValidationRecord.ps1"
    $executionPlanTool = Join-Path $Root "tools\validation\New-QiTuRealHostExecutionPlan.ps1"
    if (-not (Test-Path -LiteralPath $validationTool)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-validation-tool-smoke-" + [Guid]::NewGuid().ToString("N"))

    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $validationTool `
            -OutputDirectory $smokeRoot `
            -CorelDrawVersion "CorelDRAW Package Verify" `
            -CorelVersionIdentifier 27 `
            -DockHostMode Debug `
            -DisableOfficialCorelDockerAdapter `
            -SkipRegistrationPlan *> $null
        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Validation record generator smoke failed with exit code $LASTEXITCODE."
            return
        }

        $validationRecord = Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-real-host-validation-*.md" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        $registrationRecord = Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-registration-confirmation-*.md" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $validationRecord) {
            Add-Failure $Failures "Validation record generator did not create a real host validation record."
            return
        }

        if (-not $registrationRecord) {
            Add-Failure $Failures "Validation record generator did not create a registration confirmation record."
            return
        }

        $validationText = Get-Content -LiteralPath $validationRecord.FullName -Raw -Encoding UTF8
        $registrationText = Get-Content -LiteralPath $registrationRecord.FullName -Raw -Encoding UTF8

        if ($validationText -notmatch [regex]::Escape("CorelDRAW Package Verify")) {
            Add-Failure $Failures "Validation record generator did not prefill CorelDRAW version."
        }

        foreach ($requiredText in @(
            "AllowOfficialCorelDockerAdapter",
            "False",
            "Docker Adapter",
            "ActiveDockPanelHostKind = CorelDocker",
            "ActiveDockerAdapterType = CorelDockerAdapter",
            "IsDockerAdapterAttached = True",
            "WebViewCreateCount <= 1",
            "OfficialCorelDockerAdapterDefaultEnabled = false")) {
            if ($validationText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $Failures "Validation record generator output is missing Docker gate text: $requiredText"
            }
        }

        if ($registrationText -notmatch "\|\s*27\s*\|") {
            Add-Failure $Failures "Validation record generator did not prefill CorelDRAW version identifier."
        }

        if (Test-Path -LiteralPath $executionPlanTool) {
            & powershell -NoProfile -ExecutionPolicy Bypass -File $executionPlanTool `
                -OutputDirectory $smokeRoot `
                -CorelDrawVersion "CorelDRAW Package Verify" `
                -CorelVersionIdentifier 27 `
                -ConfirmedRegistryPath "HKCU:\Software\Corel\QiTuCDRPackageVerify\27" *> $null

            if ($LASTEXITCODE -ne 0) {
                Add-Failure $Failures "Real host execution plan generator smoke failed with exit code $LASTEXITCODE."
                return
            }

            $executionPlan = Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-real-host-execution-plan-*.md" -File -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1

            if (-not $executionPlan) {
                Add-Failure $Failures "Real host execution plan generator did not create a plan."
                return
            }

            $executionPlanText = Get-Content -LiteralPath $executionPlan.FullName -Raw -Encoding UTF8
            foreach ($requiredText in @(
                "New-QiTuRealHostValidationRecord.ps1",
                "New-QiTuConfirmedCorelRegistrationManifest.ps1",
                "PreviewCorelDrawRegistration",
                "RegisterCorelDrawAddIn",
                "UnregisterCorelDrawAddIn",
                "Stop conditions")) {
                if ($executionPlanText -notmatch [regex]::Escape($requiredText)) {
                    Add-Failure $Failures "Real host execution plan is missing required text: $requiredText"
                }
            }
        }

        $overrideRoot = Join-Path $smokeRoot "override"
        & powershell -NoProfile -ExecutionPolicy Bypass -File $validationTool `
            -OutputDirectory $overrideRoot `
            -CorelDrawVersion "CorelDRAW Package Verify" `
            -CorelVersionIdentifier 27 `
            -DockHostMode CorelDocker `
            -AllowOfficialCorelDockerAdapter `
            -ActiveDockPanelHostKind CorelDocker `
            -ActiveDockerAdapterType CorelDockerAdapter `
            -IsDockerAdapterAttached True `
            -WebViewCreateCount 1 `
            -SkipRegistrationPlan *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Validation record generator override smoke failed with exit code $LASTEXITCODE."
            return
        }

        $overrideRecord = Get-ChildItem -LiteralPath $overrideRoot -Filter "qitucdr-real-host-validation-*.md" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $overrideRecord) {
            Add-Failure $Failures "Validation record generator override smoke did not create a real host validation record."
            return
        }

        $overrideText = Get-Content -LiteralPath $overrideRecord.FullName -Raw -Encoding UTF8
        foreach ($requiredText in @(
            "| DockHostMode | CorelDocker |",
            "| AllowOfficialCorelDockerAdapter | True |",
            "| ActiveDockPanelHostKind | CorelDocker |",
            "| ActiveDockerAdapterType | CorelDockerAdapter |",
            "| IsDockerAdapterAttached | True |",
            "| WebViewCreateCount | 1 |")) {
            if ($overrideText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $Failures "Validation record generator override output is missing snapshot text: $requiredText"
            }
        }
    }
    catch {
        Add-Failure $Failures "Validation record generator smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Test-RegistrationManifestGeneratorSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $generator = Join-Path $Root "installer\New-QiTuCorelRegistrationManifest.ps1"
    $confirmedGenerator = Join-Path $Root "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1"
    $previewTool = Join-Path $Root "installer\Get-QiTuCorelRegistrationPreview.ps1"
    $validator = Join-Path $Root "installer\Test-QiTuCorelRegistrationManifest.ps1"
    if (-not (Test-Path -LiteralPath $generator) -or -not (Test-Path -LiteralPath $validator)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-registration-manifest-smoke-" + [Guid]::NewGuid().ToString("N"))
    $manifestPath = Join-Path $smokeRoot "qitucdr-coreldraw-registration-manifest.confirmed-smoke.json"

    try {
        New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null
        $confirmedAt = (Get-Date).ToString("o")

        & powershell -NoProfile -ExecutionPolicy Bypass -File $generator `
            -OutputPath $manifestPath `
            -Status CONFIRMED `
            -EnableCorelVersionIdentifier 27 `
            -ProductLabel "QiTuCDR" `
            -RegistrationKind "AddIn" `
            -RegistryPath "HKCU:\Software\Corel\QiTuCDRPackageVerify\27" `
            -ConfirmationSource "package verification smoke" `
            -ConfirmedBy "PackageVerifier" `
            -ConfirmedAt $confirmedAt *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Registration manifest generator smoke failed with exit code $LASTEXITCODE."
            return
        }

        & powershell -NoProfile -ExecutionPolicy Bypass -File $validator -ManifestPath $manifestPath -RequireConfirmed -FailOnError *> $null
        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Generated registration manifest did not pass RequireConfirmed validation."
            return
        }

        $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $enabledTargets = @($manifest.Targets | Where-Object { [bool]$_.Enabled })
        if ($enabledTargets.Count -ne 1 -or [string]$enabledTargets[0].CorelVersionIdentifier -ne "27") {
            Add-Failure $Failures "Registration manifest generator did not enable exactly one target version 27."
        }

        if (Test-Path -LiteralPath $previewTool) {
            $previewJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $previewTool `
                -ManifestPath $manifestPath `
                -InstallRoot $smokeRoot `
                -Json `
                -FailOnError

            if ($LASTEXITCODE -ne 0) {
                Add-Failure $Failures "Registration preview smoke failed with exit code $LASTEXITCODE."
                return
            }

            $preview = $previewJson | ConvertFrom-Json
            if ($preview.Status -ne "OK" -or [int]$preview.WouldWriteCount -ne 1) {
                Add-Failure $Failures "Registration preview did not report exactly one safe write."
            }

            $firstWrite = @($preview.WouldWrite | Select-Object -First 1)
            if (-not $firstWrite -or [string]$firstWrite.RegistryPath -ne "HKCU:\Software\Corel\QiTuCDRPackageVerify\27") {
                Add-Failure $Failures "Registration preview did not report the expected registry path."
            }
        }

        if (Test-Path -LiteralPath $confirmedGenerator) {
            $confirmedManifestPath = Join-Path $smokeRoot "qitucdr-coreldraw-registration-manifest.confirmed-helper-smoke.json"
            & powershell -NoProfile -ExecutionPolicy Bypass -File $confirmedGenerator `
                -OutputPath $confirmedManifestPath `
                -CorelVersionIdentifier 27 `
                -ProductLabel "QiTuCDR" `
                -RegistrationKind "AddIn" `
                -RegistryPath "HKCU:\Software\Corel\QiTuCDRPackageVerifyConfirmedHelper\27" `
                -ConfirmationSource "package verification confirmed helper smoke" `
                -ConfirmedBy "PackageVerifier" `
                -ConfirmedAt $confirmedAt *> $null

            if ($LASTEXITCODE -ne 0) {
                Add-Failure $Failures "Confirmed registration manifest helper smoke failed with exit code $LASTEXITCODE."
                return
            }

            & powershell -NoProfile -ExecutionPolicy Bypass -File $validator -ManifestPath $confirmedManifestPath -RequireConfirmed -FailOnError *> $null
            if ($LASTEXITCODE -ne 0) {
                Add-Failure $Failures "Confirmed registration manifest helper output did not pass RequireConfirmed validation."
                return
            }
        }
    }
    catch {
        Add-Failure $Failures "Registration manifest generator smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Test-RegistrationPlanSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $planTool = Join-Path $Root "installer\Get-QiTuCorelRegistrationPlan.ps1"
    if (-not (Test-Path -LiteralPath $planTool)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-registration-plan-smoke-" + [Guid]::NewGuid().ToString("N"))

    try {
        New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null
        & powershell -NoProfile -ExecutionPolicy Bypass -File $planTool -OutputDirectory $smokeRoot *> $null

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Registration plan smoke failed with exit code $LASTEXITCODE."
            return
        }

        $jsonReport = Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-coreldraw-registration-plan-*.json" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        $markdownReport = Get-ChildItem -LiteralPath $smokeRoot -Filter "qitucdr-coreldraw-registration-plan-*.md" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $jsonReport) {
            Add-Failure $Failures "Registration plan smoke did not create a JSON report."
            return
        }

        if (-not $markdownReport) {
            Add-Failure $Failures "Registration plan smoke did not create a Markdown report."
            return
        }

        $report = Get-Content -LiteralPath $jsonReport.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($report.PSObject.Properties["EvidenceSummary"] -eq $null) {
            Add-Failure $Failures "Registration plan JSON is missing EvidenceSummary."
        }

        if ($report.PSObject.Properties["ManifestFieldChecklist"] -eq $null -or @($report.ManifestFieldChecklist).Count -eq 0) {
            Add-Failure $Failures "Registration plan JSON is missing ManifestFieldChecklist."
        }

        $markdownText = Get-Content -LiteralPath $markdownReport.FullName -Raw -Encoding UTF8
        if ($markdownText -notmatch "Manifest Confirmation Checklist") {
            Add-Failure $Failures "Registration plan Markdown is missing Manifest Confirmation Checklist."
        }

        if ($markdownText -notmatch "These candidates are evidence for manual review only") {
            Add-Failure $Failures "Registration plan Markdown does not mark registry candidates as manual-review evidence."
        }
    }
    catch {
        Add-Failure $Failures "Registration plan smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Test-RealHostReadinessSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $readinessTool = Join-Path $Root "tools\validation\Test-QiTuRealHostReadiness.ps1"
    if (-not (Test-Path -LiteralPath $readinessTool)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-real-host-readiness-smoke-" + [Guid]::NewGuid().ToString("N"))

    try {
        New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null
        $readinessJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $readinessTool `
            -PackagePath $Root `
            -OutputDirectory $smokeRoot `
            -Json

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Real host readiness smoke failed with exit code $LASTEXITCODE."
            return
        }

        $readiness = $readinessJson | ConvertFrom-Json
        if ($readiness.Status -ne "READY_FOR_MANUAL_HOST_VALIDATION" -and $readiness.Status -ne "BLOCKED") {
            Add-Failure $Failures "Real host readiness smoke returned an unknown status: $($readiness.Status)"
        }

        $fatalFailures = @($readiness.FatalFailures)
        foreach ($failure in $fatalFailures) {
            if ($failure -match "Missing required file" `
                -or $failure -match "generator failed" `
                -or $failure -match "smoke output was not created") {
                Add-Failure $Failures "Real host readiness smoke reported a package/tool failure: $failure"
            }
        }

        foreach ($requiredText in @(
            "tools\validation\New-QiTuRealHostExecutionPlan.ps1",
            "tools\validation\New-QiTuRealHostValidationRecord.ps1",
            "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md",
            "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md")) {
            if (@($readiness.RequiredFiles) -notcontains $requiredText) {
                Add-Failure $Failures "Real host readiness smoke is missing required file check: $requiredText"
            }
        }

        if ([string]::IsNullOrWhiteSpace([string]$readiness.SmokeOutputDirectory) -or -not (Test-Path -LiteralPath ([string]$readiness.SmokeOutputDirectory))) {
            Add-Failure $Failures "Real host readiness smoke did not create a smoke output directory."
        }
    }
    catch {
        Add-Failure $Failures "Real host readiness smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Test-RealHostAcceptanceKitSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $kitTool = Join-Path $Root "tools\validation\New-QiTuRealHostAcceptanceKit.ps1"
    if (-not (Test-Path -LiteralPath $kitTool)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-real-host-kit-smoke-" + [Guid]::NewGuid().ToString("N"))

    try {
        New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null
        $kitJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $kitTool `
            -PackagePath $Root `
            -OutputDirectory $smokeRoot `
            -CorelDrawVersion "CorelDRAW Package Verify" `
            -CorelVersionIdentifier 27 `
            -SkipRegistrationPlan `
            -Json

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Real host acceptance kit smoke failed with exit code $LASTEXITCODE."
            return
        }

        $kit = $kitJson | ConvertFrom-Json
        if ($kit.ReadinessStatus -ne "READY_FOR_MANUAL_HOST_VALIDATION" -and $kit.ReadinessStatus -ne "BLOCKED") {
            Add-Failure $Failures "Real host acceptance kit returned an unknown readiness status: $($kit.ReadinessStatus)"
        }

        foreach ($field in @("Index", "ReadinessReport", "ExecutionPlan", "CommandChecklist", "ValidationRecord", "RegistrationConfirmation")) {
            $path = [string]$kit.$field
            if ([string]::IsNullOrWhiteSpace($path) -or -not (Test-Path -LiteralPath $path)) {
                Add-Failure $Failures "Real host acceptance kit did not create file: $field"
            }
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$kit.Index) -and (Test-Path -LiteralPath ([string]$kit.Index))) {
            $indexText = Get-Content -LiteralPath ([string]$kit.Index) -Raw -Encoding UTF8
            foreach ($requiredText in @("ReadinessStatus", "Execution plan", "Command checklist", "Validation record draft", "CONFIRMED registration manifest")) {
                if ($indexText -notmatch [regex]::Escape($requiredText)) {
                    Add-Failure $Failures "Real host acceptance kit index is missing required text: $requiredText"
                }
            }
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$kit.CommandChecklist) -and (Test-Path -LiteralPath ([string]$kit.CommandChecklist))) {
            $checklistText = Get-Content -LiteralPath ([string]$kit.CommandChecklist) -Raw -Encoding UTF8
            foreach ($requiredText in @("Invoke-QiTuCorelDrawComSmoke.ps1", "Invoke-QiTuRealHostRegistrationDryRun.ps1", "Test-QiTuRealHostInstallState.ps1", "New-QiTuConfirmedCorelRegistrationManifest.ps1", "PreviewCorelDrawRegistration", "RegisterCorelDrawAddIn", "UnregisterCorelDrawAddIn")) {
                if ($checklistText -notmatch [regex]::Escape($requiredText)) {
                    Add-Failure $Failures "Real host command checklist is missing required command text: $requiredText"
                }
            }
        }
    }
    catch {
        Add-Failure $Failures "Real host acceptance kit smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Test-RealHostRegistrationDryRunSmoke {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Failures
    )

    $dryRunTool = Join-Path $Root "tools\validation\Invoke-QiTuRealHostRegistrationDryRun.ps1"
    if (-not (Test-Path -LiteralPath $dryRunTool)) {
        return
    }

    $smokeRoot = Join-Path $env:TEMP ("qitucdr-registration-dry-run-smoke-" + [Guid]::NewGuid().ToString("N"))
    $sourcePath = Join-Path $Root "App"

    try {
        New-Item -ItemType Directory -Force -Path $smokeRoot | Out-Null
        $dryRunJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $dryRunTool `
            -SourcePath $sourcePath `
            -InstallRoot $smokeRoot `
            -OutputDirectory $smokeRoot `
            -CorelVersionIdentifier 27 `
            -RegistrationKind AddIn `
            -RegistryPath "HKCU:\Software\Corel\QiTuCDRPackageVerifyDryRun\27" `
            -Json `
            -FailOnError

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $Failures "Real host registration dry run smoke failed with exit code $LASTEXITCODE."
            return
        }

        $dryRun = $dryRunJson | ConvertFrom-Json
        if ($dryRun.Status -ne "OK") {
            Add-Failure $Failures "Real host registration dry run status is not OK: $($dryRun.Status)"
        }

        if ([int]$dryRun.WouldWriteCount -ne 1) {
            Add-Failure $Failures "Real host registration dry run did not preview exactly one registry write."
        }

        foreach ($field in @("ManifestPath", "PreviewJson", "PreviewMarkdown")) {
            $path = [string]$dryRun.$field
            if ([string]::IsNullOrWhiteSpace($path) -or -not (Test-Path -LiteralPath $path)) {
                Add-Failure $Failures "Real host registration dry run did not create file: $field"
            }
        }
    }
    catch {
        Add-Failure $Failures "Real host registration dry run smoke threw an exception: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force
        }
    }
}

function Read-ChecksumFile {
    param([string]$Path)

    $entries = @{}
    $lines = Get-Content -LiteralPath $Path -Encoding UTF8
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $match = [regex]::Match($line, "^([0-9a-fA-F]{64})\s\s(.+)$")
        if (-not $match.Success) {
            throw "Invalid checksum line: $line"
        }

        $entries[$match.Groups[2].Value] = $match.Groups[1].Value.ToLowerInvariant()
    }

    return $entries
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Get-DefaultPackagePath
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    throw "PackagePath was not provided and no release package was found."
}

$resolved = Resolve-PackageRoot $PackagePath
$tempRoot = $resolved.TempRoot
$root = $resolved.Root
$failures = New-Object "System.Collections.Generic.List[string]"
$requiredFiles = @(
    "App\QiTuCDR.Host.dll",
    "App\WebUI\index.html",
    "installer\Install-QiTuCDR.ps1",
    "installer\Uninstall-QiTuCDR.ps1",
    "installer\Test-QiTuInstallPrerequisites.ps1",
    "installer\Get-QiTuCorelRegistrationPlan.ps1",
    "installer\Get-QiTuCorelRegistrationPreview.ps1",
    "installer\New-QiTuCorelRegistrationManifest.ps1",
    "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1",
    "installer\Test-QiTuCorelRegistrationManifest.ps1",
    "tools\validation\New-QiTuRealHostValidationRecord.ps1",
    "tools\validation\New-QiTuRealHostExecutionPlan.ps1",
    "tools\validation\Test-QiTuRealHostReadiness.ps1",
    "tools\validation\New-QiTuRealHostAcceptanceKit.ps1",
    "tools\validation\New-QiTuRealHostCommandChecklist.ps1",
    "tools\validation\Invoke-QiTuCorelDrawComSmoke.ps1",
    "tools\validation\Install-QiTuCorelDrawAddon.ps1",
    "tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1",
    "tools\validation\Test-QiTuCorelDrawAddonLoad.ps1",
    "tools\validation\Test-QiTuCorelDrawAddonState.ps1",
    "tools\validation\Invoke-QiTuRealHostRegistrationDryRun.ps1",
    "tools\validation\Test-QiTuRealHostInstallState.ps1",
    "tools\validation\README.md",
    "package-manifest.json",
    "SHA256SUMS.txt",
    "VERSION",
    "README.md",
    "CHANGELOG.md",
    "docs\RELEASE_CHECKLIST.md",
    "docs\STABILITY_TEST_PLAN.md",
    "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md",
    "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md",
    "docs\REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md",
    "docs\REAL_HOST_VALIDATION_TEMPLATE.md",
    "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md",
    "docs\CORELDRAW_HOST_BINDING_CHECKLIST.md",
    "docs\CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md",
    "docs\CORELDRAW_ADDONS_MOUNT_REFERENCE.md"
)

try {
    foreach ($relative in $requiredFiles) {
        Test-RequiredFile $root $relative $failures | Out-Null
    }

    $manifestPath = Join-Path $root "package-manifest.json"
    $checksumPath = Join-Path $root "SHA256SUMS.txt"
    $manifest = $null
    $checksumEntries = @{}

    if (Test-Path -LiteralPath $manifestPath) {
        try {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        }
        catch {
            Add-Failure $failures "package-manifest.json is not valid JSON: $($_.Exception.Message)"
        }
    }

    if (Test-Path -LiteralPath $checksumPath) {
        try {
            $checksumEntries = Read-ChecksumFile $checksumPath
        }
        catch {
            Add-Failure $failures "SHA256SUMS.txt is invalid: $($_.Exception.Message)"
        }
    }

    $files = Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName
    $actualFileCount = @($files).Count

    if ($manifest -ne $null) {
        if ($manifest.Product -ne "QiTuCDR") {
            Add-Failure $failures "Manifest Product is not QiTuCDR."
        }

        if ([string]::IsNullOrWhiteSpace([string]$manifest.Version)) {
            Add-Failure $failures "Manifest Version is missing."
        }

        if ([string]::IsNullOrWhiteSpace([string]$manifest.Configuration)) {
            Add-Failure $failures "Manifest Configuration is missing."
        }

        $versionFile = Join-Path $root "VERSION"
        if (Test-Path -LiteralPath $versionFile) {
            $versionText = (Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8).Trim()
            if ($versionText -ne [string]$manifest.Version) {
                Add-Failure $failures "Manifest Version does not match VERSION file."
            }
        }

        $hostAssemblyPath = Join-Path $root "App\QiTuCDR.Host.dll"
        if (Test-Path -LiteralPath $hostAssemblyPath) {
            $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($hostAssemblyPath)
            if ($versionInfo.ProductVersion -ne [string]$manifest.Version) {
                Add-Failure $failures "Host assembly ProductVersion does not match manifest Version."
            }
        }

        if ([int]$manifest.FileCount -ne $actualFileCount -and [int]$manifest.FileCount -ne ($actualFileCount - 2)) {
            Add-Failure $failures "Manifest FileCount does not match package file count."
        }

        foreach ($field in @("HostAssembly", "WebUiIndex", "Installer", "Uninstaller", "ValidationRecordGenerator")) {
            if ([string]::IsNullOrWhiteSpace([string]$manifest.$field)) {
                Add-Failure $failures "Manifest field is missing: $field"
            }
        }

        if ($manifest.PSObject.Properties["RuntimeSafety"] -eq $null) {
            Add-Failure $failures "Manifest RuntimeSafety section is missing."
        }
        else {
            if ([string]$manifest.RuntimeSafety.DefaultDockHostMode -ne "Debug") {
                Add-Failure $failures "RuntimeSafety DefaultDockHostMode must be Debug."
            }

            if ([string]$manifest.RuntimeSafety.CorelDockerStatus -ne "PlaceholderFallbackRequired") {
                Add-Failure $failures "RuntimeSafety CorelDockerStatus must be PlaceholderFallbackRequired."
            }

            if ([bool]$manifest.RuntimeSafety.SingleWebViewRequired -ne $true) {
                Add-Failure $failures "RuntimeSafety SingleWebViewRequired must be true."
            }

            if ([bool]$manifest.RuntimeSafety.RealCorelDrawValidationRequired -ne $true) {
                Add-Failure $failures "RuntimeSafety RealCorelDrawValidationRequired must be true."
            }

            if ($manifest.RuntimeSafety.PSObject.Properties["OfficialCorelDockerAdapterDefaultEnabled"] -eq $null) {
                Add-Failure $failures "RuntimeSafety OfficialCorelDockerAdapterDefaultEnabled is missing."
            }
            elseif ([bool]$manifest.RuntimeSafety.OfficialCorelDockerAdapterDefaultEnabled -ne $false) {
                Add-Failure $failures "RuntimeSafety OfficialCorelDockerAdapterDefaultEnabled must be false until real Docker validation is complete."
            }
        }
    }

    $stabilityPlan = Join-Path $root "docs\STABILITY_TEST_PLAN.md"
    if (Test-Path -LiteralPath $stabilityPlan) {
        $stabilityText = Get-Content -LiteralPath $stabilityPlan -Raw -Encoding UTF8
        if ($stabilityText -notmatch "DockHostMode CorelDocker") {
            Add-Failure $failures "STABILITY_TEST_PLAN.md does not document CorelDocker fallback smoke command."
        }
    }

    $hostBindingChecklist = Join-Path $root "docs\CORELDRAW_HOST_BINDING_CHECKLIST.md"
    if (Test-Path -LiteralPath $hostBindingChecklist) {
        $hostBindingText = Get-Content -LiteralPath $hostBindingChecklist -Raw -Encoding UTF8
        if ($hostBindingText -notmatch "DockHostFallbackCount") {
            Add-Failure $failures "CORELDRAW_HOST_BINDING_CHECKLIST.md does not document DockHostFallbackCount."
        }
    }

    $dockerEnablement = Join-Path $root "docs\CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md"
    if (Test-Path -LiteralPath $dockerEnablement) {
        $dockerEnablementText = Get-Content -LiteralPath $dockerEnablement -Raw -Encoding UTF8
        foreach ($requiredText in @(
            "AllowOfficialCorelDockerAdapter",
            "OfficialCorelDockerAdapterDefaultEnabled",
            "ActiveDockPanelHostKind = CorelDocker",
            "ActiveDockerAdapterType = CorelDockerAdapter",
            "IsDockerAdapterAttached = True",
            "WebViewCreateCount <= 1")) {
            if ($dockerEnablementText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md is missing enablement rule: $requiredText"
            }
        }
    }

    $realHostQuickstart = Join-Path $root "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md"
    if (Test-Path -LiteralPath $realHostQuickstart) {
        $quickstartText = Get-Content -LiteralPath $realHostQuickstart -Raw -Encoding UTF8
        foreach ($requiredText in @(
            "New-QiTuRealHostExecutionPlan.ps1",
            "New-QiTuRealHostValidationRecord.ps1",
            "New-QiTuConfirmedCorelRegistrationManifest.ps1",
            "PreviewCorelDrawRegistration",
            "RegisterCorelDrawAddIn",
            "UnregisterCorelDrawAddIn",
            "Stop conditions")) {
            if ($quickstartText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "REAL_HOST_ACCEPTANCE_QUICKSTART.md is missing required step text: $requiredText"
            }
        }
    }

    $registrationTemplate = Join-Path $root "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md"
    if (Test-Path -LiteralPath $registrationTemplate) {
        $registrationText = Get-Content -LiteralPath $registrationTemplate -Raw -Encoding UTF8
        foreach ($requiredText in @("CorelVersionIdentifier", "ProductLabel", "RegistrationKind", "RegistryPath", "ConfirmationSource", "ConfirmedBy", "ConfirmedAt")) {
            if ($registrationText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md is missing field: $requiredText"
            }
        }
    }

    $validationTool = Join-Path $root "tools\validation\New-QiTuRealHostValidationRecord.ps1"
    if (Test-Path -LiteralPath $validationTool) {
        $validationToolText = Get-Content -LiteralPath $validationTool -Raw -Encoding UTF8
        foreach ($requiredText in @("REAL_HOST_VALIDATION_TEMPLATE.md", "CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md", "Get-QiTuCorelRegistrationPlan.ps1")) {
            if ($validationToolText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "New-QiTuRealHostValidationRecord.ps1 is missing dependency reference: $requiredText"
            }
        }
    }

    $executionPlanTool = Join-Path $root "tools\validation\New-QiTuRealHostExecutionPlan.ps1"
    if (Test-Path -LiteralPath $executionPlanTool) {
        $executionPlanToolText = Get-Content -LiteralPath $executionPlanTool -Raw -Encoding UTF8
        foreach ($requiredText in @("REAL_HOST_EXECUTION_PLAN_TEMPLATE.md")) {
            if ($executionPlanToolText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "New-QiTuRealHostExecutionPlan.ps1 is missing execution step text: $requiredText"
            }
        }
    }

    $executionPlanTemplate = Join-Path $root "docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md"
    if (Test-Path -LiteralPath $executionPlanTemplate) {
        $executionPlanTemplateText = Get-Content -LiteralPath $executionPlanTemplate -Raw -Encoding UTF8
        foreach ($requiredText in @("PreviewCorelDrawRegistration", "RegisterCorelDrawAddIn", "UnregisterCorelDrawAddIn", "Stop conditions")) {
            if ($executionPlanTemplateText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "REAL_HOST_EXECUTION_PLAN_TEMPLATE.md is missing execution step text: $requiredText"
            }
        }
    }

    $acceptanceQuickstart = Join-Path $root "docs\REAL_HOST_ACCEPTANCE_QUICKSTART.md"
    if (Test-Path -LiteralPath $acceptanceQuickstart) {
        $acceptanceQuickstartText = Get-Content -LiteralPath $acceptanceQuickstart -Raw -Encoding UTF8
        foreach ($requiredText in @("New-QiTuRealHostExecutionPlan.ps1", "New-QiTuRealHostValidationRecord.ps1", "New-QiTuConfirmedCorelRegistrationManifest.ps1", "Install-QiTuCDR.ps1", "Uninstall-QiTuCDR.ps1")) {
            if ($acceptanceQuickstartText -notmatch [regex]::Escape($requiredText)) {
                Add-Failure $failures "REAL_HOST_ACCEPTANCE_QUICKSTART.md is missing step text: $requiredText"
            }
        }
    }

    Test-ValidationToolSmoke $root $failures
    Test-RegistrationManifestGeneratorSmoke $root $failures
    Test-RegistrationPlanSmoke $root $failures
    Test-RealHostReadinessSmoke $root $failures
    Test-RealHostAcceptanceKitSmoke $root $failures
    Test-RealHostRegistrationDryRunSmoke $root $failures

    foreach ($entry in $checksumEntries.GetEnumerator()) {
        $path = Join-Path $root $entry.Key
        if (-not (Test-Path -LiteralPath $path)) {
            Add-Failure $failures "Checksum entry points to missing file: $($entry.Key)"
            continue
        }

        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $entry.Value) {
            Add-Failure $failures "Checksum mismatch: $($entry.Key)"
        }
    }

    $status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
    $result = [pscustomobject]@{
        Timestamp = (Get-Date).ToString("o")
        PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
        PackageRoot = $root
        IsZip = [bool]$resolved.IsZip
        FileCount = $actualFileCount
        ChecksumEntryCount = $checksumEntries.Count
        RequiredFiles = $requiredFiles
        FatalFailures = @($failures)
        Status = $status
    }

    if ($Json) {
        $result | ConvertTo-Json -Depth 5
        if ($FailOnError -and $failures.Count -gt 0) {
            exit 1
        }

        exit 0
    }

    Write-Host "QiTuCDR package verification" -ForegroundColor Cyan
    Write-Host "PackagePath: $PackagePath"
    Write-Host "PackageRoot: $root"
    Write-Host "IsZip: $($resolved.IsZip)"
    Write-Host "FileCount: $actualFileCount"
    Write-Host "ChecksumEntryCount: $($checksumEntries.Count)"
    Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })

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
