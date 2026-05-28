param(
    [string]$SourcePath,
    [string]$InstallRoot,
    [switch]$Force,
    [switch]$RegisterCorelDrawAddIn,
    [string]$CorelDrawAddInRegistryPath,
    [string]$CorelDrawRegistrationManifestPath,
    [switch]$PreviewCorelDrawRegistration,
    [switch]$AllowDirectRegistryPathForTesting
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Get-DefaultInstallRoot {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return Join-Path $localAppData "QiTuCDR"
}

function Assert-ChildPath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'

    if (-not $pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside install root: $Path"
    }
}

function New-DefaultConfig {
    return [ordered]@{
        WebViewPreheatDelayMs = 4000
        BatchSize = 50
        TaskTimeoutMs = 120000
        PreferTypedCorelInterop = $false
        AllowOfficialCorelDockerAdapter = $false
        DockHostMode = "Debug"
        NativePanel = [ordered]@{
            WindowTopmost = $false
            SaveWindowPosition = $true
            SaveToolSettings = $true
            AutoBackupOriginalFile = $false
            ShowTaskCompletedToast = $true
            ToolWindowPositions = @{}
            PopupWindowPositions = @{}
        }
    }
}

function Initialize-Config {
    param([string]$Path)

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        New-DefaultConfig | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
        return
    }

    try {
        Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json | Out-Null
    }
    catch {
        $backupPath = $Path + ".bad." + (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
        Copy-Item -LiteralPath $Path -Destination $backupPath -Force
        New-DefaultConfig | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
    }
}

function Assert-SafeCorelRegistryPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "CorelDRAW registry path is empty."
    }

    if ($Path -notmatch "^HK(CU|LM):\\Software\\(WOW6432Node\\)?Corel\\") {
        throw "CorelDRAW registry path must be under HKCU/HKLM Software Corel: $Path"
    }
}

function Get-RegistrationTargetsFromManifest {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "CorelDrawRegistrationManifestPath does not exist: $Path"
    }

    $manifest = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.Product -ne "QiTuCDR") {
        throw "Registration manifest Product must be QiTuCDR."
    }

    if ($manifest.SchemaVersion -ne "1.0") {
        throw "Registration manifest SchemaVersion must be 1.0."
    }

    if (([string]$manifest.Status).Trim().ToUpperInvariant() -ne "CONFIRMED") {
        throw "Registration manifest Status must be CONFIRMED before registry writes are allowed."
    }

    $targets = @($manifest.Targets | Where-Object { [bool]$_.Enabled })
    if ($targets.Count -eq 0) {
        throw "Registration manifest has no enabled targets."
    }

    $seenRegistryPaths = @{}
    foreach ($target in $targets) {
        if ([string]::IsNullOrWhiteSpace([string]$target.ProductLabel)) {
            throw "Enabled registration target is missing ProductLabel."
        }

        $registrationKind = [string]$target.RegistrationKind
        if ($registrationKind -ne "AddIn" -and $registrationKind -ne "Docker") {
            throw "Enabled registration target RegistrationKind must be AddIn or Docker."
        }

        if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmationSource)) {
            throw "Enabled registration target is missing ConfirmationSource."
        }

        if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmedBy)) {
            throw "Enabled registration target is missing ConfirmedBy."
        }

        $confirmedAt = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse([string]$target.ConfirmedAt, [ref]$confirmedAt)) {
            throw "Enabled registration target ConfirmedAt must be a valid date/time."
        }

        $registryPath = [string]$target.RegistryPath
        Assert-SafeCorelRegistryPath $registryPath
        $key = $registryPath.ToLowerInvariant()
        if ($seenRegistryPaths.ContainsKey($key)) {
            throw "Duplicate enabled registration RegistryPath: $registryPath"
        }

        $seenRegistryPaths[$key] = $true
    }

    return $targets
}

function Write-CorelRegistration {
    param(
        [string]$RegistryPath,
        [string]$AssemblyPath,
        [object]$Target
    )

    Assert-SafeCorelRegistryPath $RegistryPath
    $installedAt = (Get-Date).ToString("o")
    $corelVersionIdentifier = ""
    $registrationKind = ""
    if ($Target -ne $null) {
        $corelVersionIdentifier = [string]$Target.CorelVersionIdentifier
        $registrationKind = [string]$Target.RegistrationKind
    }

    New-Item -Path $RegistryPath -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "Name" -Value "QiTuCDR" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "AssemblyPath" -Value $AssemblyPath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "InstalledAt" -Value $installedAt -PropertyType String -Force | Out-Null

    if ($Target -ne $null) {
        New-ItemProperty -Path $RegistryPath -Name "CorelVersionIdentifier" -Value $corelVersionIdentifier -PropertyType String -Force | Out-Null
        New-ItemProperty -Path $RegistryPath -Name "RegistrationKind" -Value $registrationKind -PropertyType String -Force | Out-Null
    }

    return [pscustomobject]@{
        RegistryPath = $RegistryPath
        Name = "QiTuCDR"
        AssemblyPath = $AssemblyPath
        InstalledAt = $installedAt
        CorelVersionIdentifier = $corelVersionIdentifier
        RegistrationKind = $registrationKind
    }
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repoRoot "src\Host\bin\Debug\net48"
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Get-DefaultInstallRoot
}

$sourceFull = [System.IO.Path]::GetFullPath($SourcePath)
$installRootFull = [System.IO.Path]::GetFullPath($InstallRoot)
$appDirectory = Join-Path $installRootFull "App"
$configDirectory = Join-Path $installRootFull "Config"
$logDirectory = Join-Path $installRootFull "Logs"
$settingsPath = Join-Path $configDirectory "settings.json"
$manifestPath = Join-Path $installRootFull "install-manifest.json"

$hostDll = Join-Path $sourceFull "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $sourceFull "WebUI\index.html"

if (-not (Test-Path -LiteralPath $hostDll)) {
    throw "QiTuCDR.Host.dll is missing: $hostDll"
}

if (-not (Test-Path -LiteralPath $webUiIndex)) {
    throw "WebUI index is missing: $webUiIndex"
}

if ($PreviewCorelDrawRegistration) {
    if ([string]::IsNullOrWhiteSpace($CorelDrawRegistrationManifestPath)) {
        throw "CorelDrawRegistrationManifestPath is required when PreviewCorelDrawRegistration is set."
    }

    $previewScript = Join-Path $PSScriptRoot "Get-QiTuCorelRegistrationPreview.ps1"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $previewScript `
        -ManifestPath $CorelDrawRegistrationManifestPath `
        -InstallRoot $installRootFull `
        -AssemblyPath (Join-Path $appDirectory "QiTuCDR.Host.dll") `
        -FailOnError | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "CorelDRAW registration preview failed with exit code $LASTEXITCODE."
    }

    Write-Host "Preview only. No files or registry entries were changed." -ForegroundColor Yellow
    return
}

New-Item -ItemType Directory -Force -Path $installRootFull, $configDirectory, $logDirectory | Out-Null

if (Test-Path -LiteralPath $appDirectory) {
    if (-not $Force) {
        throw "App directory already exists. Use -Force to replace it: $appDirectory"
    }

    Assert-ChildPath $installRootFull $appDirectory
    Remove-Item -LiteralPath $appDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $appDirectory | Out-Null
Get-ChildItem -LiteralPath $sourceFull -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $appDirectory -Recurse -Force
}

Initialize-Config $settingsPath

$registryWritten = $false
$registeredPaths = New-Object "System.Collections.Generic.List[string]"
$registeredEntries = New-Object "System.Collections.Generic.List[object]"
if ($RegisterCorelDrawAddIn) {
    $assemblyPath = Join-Path $appDirectory "QiTuCDR.Host.dll"

    if (-not [string]::IsNullOrWhiteSpace($CorelDrawRegistrationManifestPath)) {
        $targets = Get-RegistrationTargetsFromManifest $CorelDrawRegistrationManifestPath
        foreach ($target in $targets) {
            $registryPath = [string]$target.RegistryPath
            $entry = Write-CorelRegistration $registryPath $assemblyPath $target
            $registeredPaths.Add($registryPath) | Out-Null
            $registeredEntries.Add($entry) | Out-Null
        }

        $registryWritten = $true
    }
    else {
        if (-not $AllowDirectRegistryPathForTesting) {
            throw "CorelDrawRegistrationManifestPath is required for registration writes. Use -AllowDirectRegistryPathForTesting only for controlled tests."
        }

        if ([string]::IsNullOrWhiteSpace($CorelDrawAddInRegistryPath)) {
            throw "CorelDrawAddInRegistryPath is required when AllowDirectRegistryPathForTesting is set."
        }

        $entry = Write-CorelRegistration $CorelDrawAddInRegistryPath $assemblyPath $null
        $registeredPaths.Add($CorelDrawAddInRegistryPath) | Out-Null
        $registeredEntries.Add($entry) | Out-Null
        $registryWritten = $true
    }
}

$manifest = [ordered]@{
    Product = "QiTuCDR"
    InstalledAt = (Get-Date).ToString("o")
    SourcePath = $sourceFull
    InstallRoot = $installRootFull
    AppDirectory = $appDirectory
    ConfigDirectory = $configDirectory
    LogDirectory = $logDirectory
    HostAssembly = (Join-Path $appDirectory "QiTuCDR.Host.dll")
    WebUiIndex = (Join-Path $appDirectory "WebUI\index.html")
    RegisterCorelDrawAddIn = [bool]$RegisterCorelDrawAddIn
    CorelDrawAddInRegistryPath = $CorelDrawAddInRegistryPath
    CorelDrawRegistrationManifestPath = $CorelDrawRegistrationManifestPath
    AllowDirectRegistryPathForTesting = [bool]$AllowDirectRegistryPathForTesting
    RegisteredCorelDrawAddInRegistryPaths = @($registeredPaths.ToArray())
    RegisteredCorelDrawAddInEntries = @($registeredEntries.ToArray())
    RegistryWritten = $registryWritten
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "QiTuCDR install completed." -ForegroundColor Green
Write-Host "InstallRoot: $installRootFull"
Write-Host "AppDirectory: $appDirectory"
Write-Host "Manifest: $manifestPath"
Write-Host "RegistryWritten: $registryWritten"
if ($registryWritten) {
    Write-Host "RegisteredPaths:"
    foreach ($path in $registeredPaths.ToArray()) {
        Write-Host "  - $path"
    }
}
