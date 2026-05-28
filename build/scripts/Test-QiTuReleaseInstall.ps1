param(
    [string]$PackagePath,
    [string]$InstallRoot,
    [string]$CorelVersionIdentifier = "27",
    [switch]$KeepInstallRoot,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$tempPackageRoot = $null
$createdInstallRoot = $false

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
            TempRoot = $null
            IsZip = $false
        }
    }

    if ($item.Extension -ne ".zip") {
        throw "Package path must be a directory or .zip file: $fullPath"
    }

    $temp = Join-Path $env:TEMP ("qitucdr-release-install-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    Expand-Archive -LiteralPath $item.FullName -DestinationPath $temp -Force

    return [pscustomobject]@{
        Root = $temp
        TempRoot = $temp
        IsZip = $true
    }
}

function Test-PathRequired {
    param(
        [string]$Path,
        [string]$Name,
        [System.Collections.Generic.List[string]]$Failures
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        Add-Failure $Failures "Missing $Name`: $Path"
        return $false
    }

    return $true
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Get-DefaultPackagePath
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    throw "PackagePath was not provided and no release zip was found."
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Join-Path $env:TEMP ("qitucdr-install-smoke-" + [Guid]::NewGuid().ToString("N"))
    $createdInstallRoot = $true
}

$installRootFull = [System.IO.Path]::GetFullPath($InstallRoot)
$failures = New-Object "System.Collections.Generic.List[string]"
$package = Resolve-PackageRoot $PackagePath
$tempPackageRoot = $package.TempRoot
$packageRoot = $package.Root

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "build\scripts\Test-QiTuPackage.ps1") -PackagePath $PackagePath -FailOnError | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Add-Failure $failures "Package verification failed with exit code $LASTEXITCODE."
    }

    $installScript = Join-Path $packageRoot "installer\Install-QiTuCDR.ps1"
    $uninstallScript = Join-Path $packageRoot "installer\Uninstall-QiTuCDR.ps1"
    $registrationPlanScript = Join-Path $packageRoot "installer\Get-QiTuCorelRegistrationPlan.ps1"
    $confirmedManifestScript = Join-Path $packageRoot "installer\New-QiTuConfirmedCorelRegistrationManifest.ps1"
    $installStateScript = Join-Path $packageRoot "tools\validation\Test-QiTuRealHostInstallState.ps1"
    $sourcePath = Join-Path $packageRoot "App"

    Test-PathRequired $installScript "install script" $failures | Out-Null
    Test-PathRequired $uninstallScript "uninstall script" $failures | Out-Null
    Test-PathRequired $registrationPlanScript "registration plan script" $failures | Out-Null
    Test-PathRequired $confirmedManifestScript "confirmed registration manifest helper" $failures | Out-Null
    Test-PathRequired $installStateScript "real host install state checker" $failures | Out-Null
    Test-PathRequired (Join-Path $sourcePath "QiTuCDR.Host.dll") "package Host DLL" $failures | Out-Null
    Test-PathRequired (Join-Path $sourcePath "WebUI\index.html") "package WebUI" $failures | Out-Null

    $registrationPlanDirectory = Join-Path $installRootFull "RegistrationPlan"
    if (Test-Path -LiteralPath $registrationPlanScript) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $registrationPlanScript -InstalledAssemblyPath (Join-Path $installRootFull "App\QiTuCDR.Host.dll") -OutputDirectory $registrationPlanDirectory | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Registration plan script failed with exit code $LASTEXITCODE."
        }
    }

    if (-not (Get-ChildItem -LiteralPath $registrationPlanDirectory -Filter "qitucdr-coreldraw-registration-plan-*.json" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Add-Failure $failures "Registration plan JSON report was not created."
    }

    if (-not (Get-ChildItem -LiteralPath $registrationPlanDirectory -Filter "qitucdr-coreldraw-registration-plan-*.md" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Add-Failure $failures "Registration plan Markdown report was not created."
    }

    $registrationManifestPath = Join-Path $installRootFull "qitucdr-coreldraw-registration-manifest.confirmed-smoke.json"
    $smokeRegistryPath = "HKCU:\Software\Corel\QiTuCDRReleaseInstallSmoke\$([Guid]::NewGuid().ToString("N"))"
    if (Test-Path -LiteralPath $confirmedManifestScript) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $confirmedManifestScript `
            -OutputPath $registrationManifestPath `
            -CorelVersionIdentifier $CorelVersionIdentifier `
            -RegistrationKind AddIn `
            -RegistryPath $smokeRegistryPath `
            -ConfirmationSource "release install smoke" | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Confirmed registration manifest helper failed with exit code $LASTEXITCODE."
        }
    }

    if ($failures.Count -eq 0) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $installScript `
            -SourcePath $sourcePath `
            -InstallRoot $installRootFull `
            -PreviewCorelDrawRegistration `
            -CorelDrawRegistrationManifestPath $registrationManifestPath | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Install registration preview failed with exit code $LASTEXITCODE."
        }

        if (Test-Path -Path $smokeRegistryPath) {
            Add-Failure $failures "Registration preview unexpectedly wrote registry path: $smokeRegistryPath"
        }
    }

    if ($failures.Count -eq 0) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $installScript `
            -SourcePath $sourcePath `
            -InstallRoot $installRootFull `
            -Force `
            -RegisterCorelDrawAddIn `
            -CorelDrawRegistrationManifestPath $registrationManifestPath | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Install script failed with exit code $LASTEXITCODE."
        }
    }

    $installedHost = Join-Path $installRootFull "App\QiTuCDR.Host.dll"
    $installedWebUi = Join-Path $installRootFull "App\WebUI\index.html"
    $installedConfig = Join-Path $installRootFull "Config\settings.json"
    $installedLogs = Join-Path $installRootFull "Logs"
    $installedManifest = Join-Path $installRootFull "install-manifest.json"

    Test-PathRequired $installedHost "installed Host DLL" $failures | Out-Null
    Test-PathRequired $installedWebUi "installed WebUI" $failures | Out-Null
    Test-PathRequired $installedConfig "installed settings.json" $failures | Out-Null
    Test-PathRequired $installedLogs "installed Logs directory" $failures | Out-Null
    Test-PathRequired $installedManifest "install manifest" $failures | Out-Null

    if (Test-Path -LiteralPath $installedConfig) {
        try {
            Get-Content -LiteralPath $installedConfig -Raw -Encoding UTF8 | ConvertFrom-Json | Out-Null
        }
        catch {
            Add-Failure $failures "Installed settings.json is invalid JSON: $($_.Exception.Message)"
        }
    }

    if (-not (Test-Path -Path $smokeRegistryPath)) {
        Add-Failure $failures "Registered smoke registry path was not created."
    }
    else {
        $registryItem = Get-ItemProperty -Path $smokeRegistryPath -ErrorAction SilentlyContinue
        if ([string]$registryItem.Name -ne "QiTuCDR") {
            Add-Failure $failures "Registered smoke registry Name value is incorrect."
        }

        if ([string]$registryItem.AssemblyPath -ne $installedHost) {
            Add-Failure $failures "Registered smoke registry AssemblyPath value is incorrect."
        }

        if ([string]$registryItem.CorelVersionIdentifier -ne $CorelVersionIdentifier) {
            Add-Failure $failures "Registered smoke registry CorelVersionIdentifier value is incorrect."
        }
    }

    if (Test-Path -LiteralPath $installedManifest) {
        try {
            $installManifest = Get-Content -LiteralPath $installedManifest -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([bool]$installManifest.RegistryWritten -ne $true) {
                Add-Failure $failures "Install manifest did not record RegistryWritten = true."
            }

            if (@($installManifest.RegisteredCorelDrawAddInEntries).Count -ne 1) {
                Add-Failure $failures "Install manifest did not record exactly one registration entry."
            }
        }
        catch {
            Add-Failure $failures "Install manifest registry entry check failed: $($_.Exception.Message)"
        }
    }

    if ($failures.Count -eq 0 -and (Test-Path -LiteralPath $installStateScript)) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $installStateScript `
            -InstallRoot $installRootFull `
            -CorelDrawRegistrationManifestPath $registrationManifestPath `
            -FailOnError | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Real host install state checker failed with exit code $LASTEXITCODE."
        }
    }

    if (Test-Path -LiteralPath $uninstallScript) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $uninstallScript `
            -InstallRoot $installRootFull `
            -UnregisterCorelDrawAddIn `
            -CorelDrawRegistrationManifestPath $registrationManifestPath | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Add-Failure $failures "Uninstall script failed with exit code $LASTEXITCODE."
        }
    }

    if (Test-Path -Path $smokeRegistryPath) {
        Add-Failure $failures "Uninstall did not remove smoke registry path."
    }

    if (Test-Path -LiteralPath (Join-Path $installRootFull "App")) {
        Add-Failure $failures "Uninstall did not remove App directory."
    }

    if (-not (Test-Path -LiteralPath $installedConfig)) {
        Add-Failure $failures "Default uninstall should preserve settings.json."
    }

    if (-not (Test-Path -LiteralPath $installedLogs)) {
        Add-Failure $failures "Default uninstall should preserve Logs directory."
    }

    $status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
    $result = [pscustomobject]@{
        Timestamp = (Get-Date).ToString("o")
        PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
        PackageRoot = $packageRoot
        PackageIsZip = [bool]$package.IsZip
        InstallRoot = $installRootFull
        CorelVersionIdentifier = $CorelVersionIdentifier
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

    Write-Host "QiTuCDR release install smoke" -ForegroundColor Cyan
    Write-Host "PackagePath: $PackagePath"
    Write-Host "InstallRoot: $installRootFull"
    Write-Host "CorelVersionIdentifier: $CorelVersionIdentifier"
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
    if (-not [string]::IsNullOrWhiteSpace($tempPackageRoot) -and (Test-Path -LiteralPath $tempPackageRoot)) {
        Remove-Item -LiteralPath $tempPackageRoot -Recurse -Force
    }

    if (-not $KeepInstallRoot -and $createdInstallRoot -and (Test-Path -LiteralPath $installRootFull)) {
        Remove-Item -LiteralPath $installRootFull -Recurse -Force
    }
}
