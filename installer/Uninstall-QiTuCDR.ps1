param(
    [string]$InstallRoot,
    [switch]$RemoveConfig,
    [switch]$RemoveLogs,
    [switch]$UnregisterCorelDrawAddIn,
    [string]$CorelDrawAddInRegistryPath,
    [string]$CorelDrawRegistrationManifestPath,
    [switch]$AllowDirectRegistryPathForTesting
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

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
        throw "Refusing to remove path outside install root: $Path"
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

function Remove-PathWithRetry {
    param([string]$Path)

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            if (-not (Test-Path -LiteralPath $Path)) {
                return
            }

            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq 3) {
                throw
            }

            Start-Sleep -Milliseconds (150 * $attempt)
        }
    }
}

function Get-RegistrationPathsFromManifest {
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
        throw "Registration manifest Status must be CONFIRMED before registry cleanup is allowed."
    }

    $registryPaths = @()
    $seenRegistryPaths = @{}
    foreach ($target in @($manifest.Targets | Where-Object { [bool]$_.Enabled })) {
        if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmedBy)) {
            throw "Enabled registration target is missing ConfirmedBy."
        }

        if ([string]::IsNullOrWhiteSpace([string]$target.ConfirmationSource)) {
            throw "Enabled registration target is missing ConfirmationSource."
        }

        $registryPath = [string]$target.RegistryPath
        Assert-SafeCorelRegistryPath $registryPath
        $key = $registryPath.ToLowerInvariant()
        if ($seenRegistryPaths.ContainsKey($key)) {
            throw "Duplicate enabled registration RegistryPath: $registryPath"
        }

        $seenRegistryPaths[$key] = $true
        $registryPaths += $registryPath
    }

    return $registryPaths
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Get-DefaultInstallRoot
}

$installRootFull = [System.IO.Path]::GetFullPath($InstallRoot)
$appDirectory = Join-Path $installRootFull "App"
$configDirectory = Join-Path $installRootFull "Config"
$logDirectory = Join-Path $installRootFull "Logs"
$manifestPath = Join-Path $installRootFull "install-manifest.json"

foreach ($path in @($appDirectory, $manifestPath)) {
    if (Test-Path -LiteralPath $path) {
        Assert-ChildPath $installRootFull $path
        Remove-PathWithRetry $path
    }
}

if ($RemoveConfig -and (Test-Path -LiteralPath $configDirectory)) {
    Assert-ChildPath $installRootFull $configDirectory
    Remove-PathWithRetry $configDirectory
}

if ($RemoveLogs -and (Test-Path -LiteralPath $logDirectory)) {
    Assert-ChildPath $installRootFull $logDirectory
    Remove-PathWithRetry $logDirectory
}

if ($UnregisterCorelDrawAddIn) {
    $registryPaths = @()
    $removedRegistryPaths = New-Object "System.Collections.Generic.List[string]"
    $missingRegistryPaths = New-Object "System.Collections.Generic.List[string]"
    if (-not [string]::IsNullOrWhiteSpace($CorelDrawRegistrationManifestPath)) {
        $registryPaths = @(Get-RegistrationPathsFromManifest $CorelDrawRegistrationManifestPath)
    }
    elseif (-not [string]::IsNullOrWhiteSpace($CorelDrawAddInRegistryPath)) {
        if (-not $AllowDirectRegistryPathForTesting) {
            throw "CorelDrawRegistrationManifestPath is required for registry cleanup. Use -AllowDirectRegistryPathForTesting only for controlled tests."
        }

        $registryPaths = @($CorelDrawAddInRegistryPath)
    }
    else {
        throw "CorelDrawAddInRegistryPath or CorelDrawRegistrationManifestPath is required when UnregisterCorelDrawAddIn is set."
    }

    foreach ($registryPath in $registryPaths) {
        Assert-SafeCorelRegistryPath $registryPath
        if (Test-Path -Path $registryPath) {
            Remove-Item -Path $registryPath -Recurse -Force
            $removedRegistryPaths.Add($registryPath) | Out-Null
        }
        else {
            $missingRegistryPaths.Add($registryPath) | Out-Null
        }
    }
}

Write-Host "QiTuCDR uninstall completed." -ForegroundColor Green
Write-Host "InstallRoot: $installRootFull"
Write-Host "ConfigRemoved: $RemoveConfig"
Write-Host "LogsRemoved: $RemoveLogs"
if ($UnregisterCorelDrawAddIn) {
    Write-Host "UnregisteredPaths:"
    foreach ($path in $removedRegistryPaths.ToArray()) {
        Write-Host "  - $path"
    }

    if ($missingRegistryPaths.Count -gt 0) {
        Write-Host "MissingRegistryPaths:"
        foreach ($path in $missingRegistryPaths.ToArray()) {
            Write-Host "  - $path"
        }
    }
}
