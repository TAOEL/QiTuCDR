param(
    [string]$InstallRoot,
    [string]$CorelDrawRegistrationManifestPath,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
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

function Get-EnabledManifestTargets {
    param(
        [string]$Path,
        [System.Collections.Generic.List[string]]$Failures
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return @()
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        Add-Failure $Failures "Registration manifest does not exist: $Path"
        return @()
    }

    try {
        $manifest = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($manifest.Product -ne "QiTuCDR") {
            Add-Failure $Failures "Registration manifest Product must be QiTuCDR."
        }

        if ($manifest.SchemaVersion -ne "1.0") {
            Add-Failure $Failures "Registration manifest SchemaVersion must be 1.0."
        }

        if (([string]$manifest.Status).Trim().ToUpperInvariant() -ne "CONFIRMED") {
            Add-Failure $Failures "Registration manifest Status must be CONFIRMED."
        }

        return @($manifest.Targets | Where-Object { [bool]$_.Enabled })
    }
    catch {
        Add-Failure $Failures "Registration manifest cannot be read: $($_.Exception.Message)"
        return @()
    }
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Get-DefaultInstallRoot
}

$installRootFull = [System.IO.Path]::GetFullPath($InstallRoot)
$appDirectory = Join-Path $installRootFull "App"
$hostAssembly = Join-Path $appDirectory "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $appDirectory "WebUI\index.html"
$settingsPath = Join-Path $installRootFull "Config\settings.json"
$logDirectory = Join-Path $installRootFull "Logs"
$installManifestPath = Join-Path $installRootFull "install-manifest.json"
$failures = New-Object "System.Collections.Generic.List[string]"
$registryChecks = New-Object "System.Collections.Generic.List[object]"

foreach ($required in @(
    @{ Name = "InstallRoot"; Path = $installRootFull },
    @{ Name = "App directory"; Path = $appDirectory },
    @{ Name = "Host assembly"; Path = $hostAssembly },
    @{ Name = "WebUI index"; Path = $webUiIndex },
    @{ Name = "settings.json"; Path = $settingsPath },
    @{ Name = "Logs directory"; Path = $logDirectory },
    @{ Name = "install-manifest.json"; Path = $installManifestPath }
)) {
    if (-not (Test-Path -LiteralPath $required.Path)) {
        Add-Failure $failures "$($required.Name) is missing: $($required.Path)"
    }
}

if (Test-Path -LiteralPath $settingsPath) {
    try {
        Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json | Out-Null
    }
    catch {
        Add-Failure $failures "settings.json is not valid JSON: $($_.Exception.Message)"
    }
}

$installManifest = $null
if (Test-Path -LiteralPath $installManifestPath) {
    try {
        $installManifest = Get-Content -LiteralPath $installManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($installManifest.Product -ne "QiTuCDR") {
            Add-Failure $failures "install-manifest.json Product must be QiTuCDR."
        }

        if ([string]$installManifest.HostAssembly -ne $hostAssembly) {
            Add-Failure $failures "install-manifest.json HostAssembly does not match expected installed path."
        }

        if ([string]$installManifest.WebUiIndex -ne $webUiIndex) {
            Add-Failure $failures "install-manifest.json WebUiIndex does not match expected installed path."
        }
    }
    catch {
        Add-Failure $failures "install-manifest.json cannot be read: $($_.Exception.Message)"
    }
}

$manifestTargets = @(Get-EnabledManifestTargets $CorelDrawRegistrationManifestPath $failures)
$expectedRegistryPaths = New-Object "System.Collections.Generic.List[string]"

foreach ($target in $manifestTargets) {
    $registryPath = [string]$target.RegistryPath
    if (-not (Test-SafeCorelRegistryPath $registryPath)) {
        Add-Failure $failures "Enabled manifest target has unsafe RegistryPath: $registryPath"
        continue
    }

    $expectedRegistryPaths.Add($registryPath) | Out-Null
}

if ($installManifest -ne $null) {
    foreach ($entry in @($installManifest.RegisteredCorelDrawAddInEntries)) {
        $registryPath = [string]$entry.RegistryPath
        if (-not [string]::IsNullOrWhiteSpace($registryPath)) {
            if (-not (Test-SafeCorelRegistryPath $registryPath)) {
                Add-Failure $failures "install-manifest.json contains unsafe registered path: $registryPath"
            }
            elseif (-not $expectedRegistryPaths.Contains($registryPath)) {
                $expectedRegistryPaths.Add($registryPath) | Out-Null
            }
        }
    }
}

foreach ($registryPath in $expectedRegistryPaths.ToArray()) {
    $exists = Test-Path -Path $registryPath
    $registryName = ""
    $registryAssemblyPath = ""
    $registryVersion = ""
    $registryKind = ""

    if ($exists) {
        try {
            $item = Get-ItemProperty -Path $registryPath -ErrorAction Stop
            $registryName = [string]$item.Name
            $registryAssemblyPath = [string]$item.AssemblyPath
            $registryVersion = [string]$item.CorelVersionIdentifier
            $registryKind = [string]$item.RegistrationKind

            if ($registryName -ne "QiTuCDR") {
                Add-Failure $failures "Registry Name is not QiTuCDR: $registryPath"
            }

            if ($registryAssemblyPath -ne $hostAssembly) {
                Add-Failure $failures "Registry AssemblyPath does not match installed Host assembly: $registryPath"
            }

            if ([string]::IsNullOrWhiteSpace($registryVersion)) {
                Add-Failure $failures "Registry CorelVersionIdentifier is missing: $registryPath"
            }

            if ($registryKind -ne "AddIn" -and $registryKind -ne "Docker") {
                Add-Failure $failures "Registry RegistrationKind must be AddIn or Docker: $registryPath"
            }
        }
        catch {
            Add-Failure $failures "Registry path cannot be read: $registryPath / $($_.Exception.Message)"
        }
    }
    else {
        Add-Failure $failures "Expected CorelDRAW registration path is missing: $registryPath"
    }

    $registryChecks.Add([pscustomobject]@{
        RegistryPath = $registryPath
        Exists = [bool]$exists
        Name = $registryName
        AssemblyPath = $registryAssemblyPath
        CorelVersionIdentifier = $registryVersion
        RegistrationKind = $registryKind
    }) | Out-Null
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    InstallRoot = $installRootFull
    HostAssembly = $hostAssembly
    WebUiIndex = $webUiIndex
    SettingsPath = $settingsPath
    LogDirectory = $logDirectory
    InstallManifestPath = $installManifestPath
    CorelDrawRegistrationManifestPath = $CorelDrawRegistrationManifestPath
    RegistryChecks = @($registryChecks.ToArray())
    FatalFailures = @($failures)
    Status = $status
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR real host install state" -ForegroundColor Cyan
Write-Host "InstallRoot: $installRootFull"
Write-Host "RegistryCheckCount: $($registryChecks.Count)"
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })

if ($registryChecks.Count -gt 0) {
    Write-Host "RegistryChecks:"
    foreach ($check in $registryChecks.ToArray()) {
        Write-Host "  - $($check.RegistryPath) / Exists=$($check.Exists) / AssemblyPath=$($check.AssemblyPath)"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
