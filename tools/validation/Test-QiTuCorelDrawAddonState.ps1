param(
    [string]$CorelProgramsDirectory = "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64",
    [string]$AddonName = "QiTuCDR",
    [string]$HostedType = "QiTuCDR.Host.Addons.AddonEntry",
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

function Add-Failure {
    param(
        [System.Collections.IList]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

function Test-XmlFile {
    param(
        [string]$Path,
        [System.Collections.IList]$Failures,
        [string]$Name
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        Add-Failure $Failures "Missing $Name`: $Path"
        return $false
    }

    try {
        [xml](Get-Content -LiteralPath $Path -Raw -Encoding UTF8) | Out-Null
        return $true
    }
    catch {
        Add-Failure $Failures "$Name is not valid XML: $($_.Exception.Message)"
        return $false
    }
}

function Test-AssemblyType {
    param(
        [string]$AssemblyPath,
        [string]$TypeName
    )

    $assemblyDirectory = Split-Path -Parent $AssemblyPath
    $resolveHandler = [System.ResolveEventHandler]{
        param($sender, $args)

        $requested = New-Object System.Reflection.AssemblyName -ArgumentList $args.Name
        $candidate = Join-Path $assemblyDirectory ($requested.Name + ".dll")
        if (Test-Path -LiteralPath $candidate) {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }

        return $null
    }

    [AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
    try {
        $assembly = [System.Reflection.Assembly]::LoadFrom($AssemblyPath)
        return $assembly.GetType($TypeName, $false) -ne $null
    }
    finally {
        [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveHandler)
    }
}

function Get-NormalizedDirectoryPrefix {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    return $fullPath + [System.IO.Path]::DirectorySeparatorChar
}

$failures = New-Object System.Collections.ArrayList
$warnings = New-Object System.Collections.ArrayList

$programsFull = [System.IO.Path]::GetFullPath($CorelProgramsDirectory)
$addonDirectory = Join-Path $programsFull "Addons\$AddonName"
$addonPrefix = Get-NormalizedDirectoryPrefix $addonDirectory
$corelExe = Join-Path $programsFull "CorelDRW.exe"
$appUi = Join-Path $addonDirectory "AppUI.xslt"
$userUi = Join-Path $addonDirectory "UserUI.xslt"
$enabledMarker = Join-Path $addonDirectory "CorelDrw.addon"
$disabledMarker = Join-Path $addonDirectory "CorelDrw.addon.disabled"
$hostDll = Join-Path $addonDirectory "QiTuCDR.Host.dll"
$webUiIndex = Join-Path $addonDirectory "WebUI\index.html"
$manifest = Join-Path $addonDirectory "qitucdr-addon-install-manifest.json"
$addonEntryLog = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "QiTuCDR\Logs\coreldraw-addon-entry.log"

foreach ($required in @(
    @{ Name = "CorelDRAW executable"; Path = $corelExe },
    @{ Name = "Addon directory"; Path = $addonDirectory },
    @{ Name = "Host DLL"; Path = $hostDll },
    @{ Name = "WebUI index"; Path = $webUiIndex },
    @{ Name = "Addon install manifest"; Path = $manifest }
)) {
    if (-not (Test-Path -LiteralPath $required.Path)) {
        Add-Failure $failures "Missing $($required.Name): $($required.Path)"
    }
}

$appUiValid = Test-XmlFile $appUi $failures "AppUI.xslt"
$userUiValid = Test-XmlFile $userUi $failures "UserUI.xslt"

$appUiHasHostedType = $false
if ($appUiValid) {
    $appUiText = Get-Content -LiteralPath $appUi -Raw -Encoding UTF8
    $appUiHasHostedType = $appUiText.Contains("QiTuCDR.Host.dll,$HostedType")
    if (-not $appUiHasHostedType) {
        Add-Failure $failures "AppUI.xslt does not contain expected hostedType: QiTuCDR.Host.dll,$HostedType"
    }
}

$hostedTypeLoadable = $false
if (Test-Path -LiteralPath $hostDll) {
    try {
        $hostedTypeLoadable = Test-AssemblyType $hostDll $HostedType
        if (-not $hostedTypeLoadable) {
            Add-Failure $failures "Hosted type was not found in Host DLL: $HostedType"
        }
    }
    catch {
        Add-Failure $failures "Hosted type reflection check failed: $($_.Exception.Message)"
    }
}

$processes = @(
    Get-Process CorelDRW -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                -not [string]::IsNullOrWhiteSpace($_.Path) -and
                    [System.IO.Path]::GetFullPath($_.Path).StartsWith((Get-NormalizedDirectoryPrefix $programsFull), [System.StringComparison]::OrdinalIgnoreCase)
            }
            catch {
                $false
            }
        }
)

if ($processes.Count -eq 0) {
    Add-Failure $failures "CorelDRAW target version is not running: $programsFull"
}

$loadedModules = New-Object System.Collections.ArrayList
foreach ($process in $processes) {
    try {
        foreach ($module in $process.Modules) {
            $modulePath = [string]$module.FileName
            if (-not [string]::IsNullOrWhiteSpace($modulePath) -and $modulePath.StartsWith($addonPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $loadedModules.Add([pscustomobject]@{
                    ProcessId = $process.Id
                    ModuleName = $module.ModuleName
                    ModulePath = $modulePath
                }) | Out-Null
            }
        }
    }
    catch {
        $warnings.Add("Could not enumerate modules for PID $($process.Id): $($_.Exception.Message)") | Out-Null
    }
}

if ($loadedModules.Count -eq 0) {
    Add-Failure $failures "No QiTuCDR addon modules are loaded by the target CorelDRAW process."
}

$webViewChildren = @()
try {
    $targetProcessIds = @($processes | ForEach-Object { [int]$_.Id })
    $webViewChildren = @(
        Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $targetProcessIds -contains [int]$_.ParentProcessId } |
            Select-Object ProcessId, ParentProcessId, Name, CommandLine
    )
}
catch {
    $warnings.Add("Could not query WebView2 child processes: $($_.Exception.Message)") | Out-Null
}

$addonEntryLogExists = Test-Path -LiteralPath $addonEntryLog
if (-not $addonEntryLogExists) {
    $warnings.Add("AddonEntry log was not found yet: $addonEntryLog") | Out-Null
}

$autoLoadEnabled = Test-Path -LiteralPath $enabledMarker
$autoLoadDisabled = Test-Path -LiteralPath $disabledMarker

$processSummaries = @(
    foreach ($process in $processes) {
        $startTime = $null
        try {
            $startTime = $process.StartTime
        }
        catch {
            $warnings.Add("Could not read StartTime for PID $($process.Id): $($_.Exception.Message)") | Out-Null
        }

        [pscustomobject]@{
            Id = $process.Id
            ProcessName = $process.ProcessName
            Path = $process.Path
            StartTime = $startTime
            MainWindowTitle = $process.MainWindowTitle
        }
    }
)

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$loadedModuleItems = @($loadedModules | ForEach-Object { $_ })
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    Status = $status
    CorelProgramsDirectory = $programsFull
    AddonDirectory = $addonDirectory
    ProcessCount = $processes.Count
    Processes = @($processSummaries)
    AppUiValid = $appUiValid
    UserUiValid = $userUiValid
    AutoLoadEnabled = $autoLoadEnabled
    AutoLoadDisabled = $autoLoadDisabled
    AppUiHasHostedType = $appUiHasHostedType
    HostedTypeLoadable = $hostedTypeLoadable
    LoadedAddonModuleCount = $loadedModuleItems.Count
    LoadedAddonModules = @($loadedModuleItems)
    WebView2ChildProcessCount = $webViewChildren.Count
    WebView2ChildProcesses = @($webViewChildren)
    AddonEntryLogExists = $addonEntryLogExists
    AddonEntryLogPath = $addonEntryLog
    Warnings = @($warnings)
    FatalFailures = @($failures)
}

if ($Json) {
    $result | ConvertTo-Json -Depth 7
    if ($FailOnError -and $failures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR CorelDRAW addon state check" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "CorelProgramsDirectory: $programsFull"
Write-Host "AddonDirectory: $addonDirectory"
Write-Host "ProcessCount: $($processes.Count)"
foreach ($process in $processes) {
    Write-Host "  - PID $($process.Id): $($process.Path)"
}
Write-Host "AppUIValid: $appUiValid"
Write-Host "UserUIValid: $userUiValid"
Write-Host "AutoLoadEnabled: $autoLoadEnabled"
Write-Host "AutoLoadDisabled: $autoLoadDisabled"
Write-Host "AppUIHostedType: $appUiHasHostedType"
Write-Host "HostedTypeLoadable: $hostedTypeLoadable"
Write-Host "LoadedAddonModuleCount: $($loadedModules.Count)"
foreach ($module in $loadedModules) {
    Write-Host "  - PID $($module.ProcessId): $($module.ModuleName)"
}
Write-Host "WebView2ChildProcessCount: $($webViewChildren.Count)"
Write-Host "AddonEntryLogExists: $addonEntryLogExists"
if ($warnings.Count -gt 0) {
    Write-Host "Warnings:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($failures.Count -gt 0) {
    Write-Host "Required actions:" -ForegroundColor Yellow
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
