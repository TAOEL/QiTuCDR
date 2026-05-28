param(
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

function Test-PathItem {
    param([string]$Path)

    [pscustomobject]@{
        Path = $Path
        Exists = Test-Path -LiteralPath $Path
    }
}

function Test-WritableDirectory {
    param([string]$Path)

    try {
        if (-not (Test-Path -LiteralPath $Path)) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }

        $probe = Join-Path $Path (".qitucdr-write-test-" + [Guid]::NewGuid().ToString("N") + ".tmp")
        Set-Content -LiteralPath $probe -Value "ok" -Encoding UTF8
        Remove-Item -LiteralPath $probe -Force

        return [pscustomobject]@{
            Path = $Path
            Exists = $true
            Writable = $true
            Message = "OK"
        }
    }
    catch {
        return [pscustomobject]@{
            Path = $Path
            Exists = Test-Path -LiteralPath $Path
            Writable = $false
            Message = $_.Exception.Message
        }
    }
}

function Test-JsonFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{
            Path = $Path
            Exists = $false
            ValidJson = $null
            PreferTypedCorelInterop = $null
            DockHostMode = $null
            Message = "File does not exist yet."
        }
    }

    try {
        $settings = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
        $preferTyped = $false
        if ($settings.PSObject.Properties["PreferTypedCorelInterop"]) {
            $preferTyped = [bool]$settings.PreferTypedCorelInterop
        }
        elseif ($settings.PSObject.Properties["preferTypedCorelInterop"]) {
            $preferTyped = [bool]$settings.preferTypedCorelInterop
        }

        $dockHostMode = "Debug"
        if ($settings.PSObject.Properties["DockHostMode"]) {
            $dockHostMode = [string]$settings.DockHostMode
        }
        elseif ($settings.PSObject.Properties["dockHostMode"]) {
            $dockHostMode = [string]$settings.dockHostMode
        }

        return [pscustomobject]@{
            Path = $Path
            Exists = $true
            ValidJson = $true
            PreferTypedCorelInterop = $preferTyped
            DockHostMode = $dockHostMode
            Message = "OK"
        }
    }
    catch {
        return [pscustomobject]@{
            Path = $Path
            Exists = $true
            ValidJson = $false
            PreferTypedCorelInterop = $null
            DockHostMode = $null
            Message = $_.Exception.Message
        }
    }
}

function Get-CommandVersion {
    param([string]$Command)

    $cmd = Get-Command $Command -ErrorAction SilentlyContinue
    if (-not $cmd) {
        return [pscustomobject]@{
            Command = $Command
            Exists = $false
            Version = $null
            Source = $null
        }
    }

    $version = & $Command --version 2>$null | Select-Object -First 1
    return [pscustomobject]@{
        Command = $Command
        Exists = $true
        Version = $version
        Source = $cmd.Source
    }
}

function Find-TlbImp {
    $roots = @(
        "C:\Program Files (x86)\Microsoft SDKs",
        "C:\Program Files\Microsoft SDKs",
        "C:\Program Files (x86)\Windows Kits",
        "C:\Program Files\Windows Kits"
    )

    $items = foreach ($root in $roots) {
        if (Test-Path -LiteralPath $root) {
            Get-ChildItem -Path $root -Recurse -Filter "TlbImp.exe" -ErrorAction SilentlyContinue
        }
    }

    $items |
        Sort-Object -Property @{
            Expression = {
                if ($_.FullName -like "*NETFX 4.8 Tools*x64*") { 0 }
                elseif ($_.FullName -like "*NETFX 4.8 Tools*") { 1 }
                elseif ($_.FullName -like "*x64*") { 2 }
                else { 3 }
            }
        }, FullName |
        Select-Object -First 1
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$webUiIndex = Join-Path $repoRoot "src\WebUI\index.html"
$webPackageJson = Join-Path $repoRoot "web\package.json"
$localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$qituRoot = Join-Path $localAppData "QiTuCDR"
$configDirectory = Join-Path $qituRoot "Config"
$logDirectory = Join-Path $qituRoot "Logs"
$settingsFile = Join-Path $configDirectory "settings.json"

$corelRoots = @(
    "C:\Program Files\Corel",
    "C:\Program Files (x86)\Corel"
)

$typeLibs = foreach ($root in $corelRoots) {
    if (Test-Path -LiteralPath $root) {
        Get-ChildItem -Path $root -Recurse -Filter "CorelDRAW.tlb" -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty FullName
    }
}

$webViewRuntime = Get-ChildItem -Path @(
    "C:\Program Files (x86)\Microsoft\EdgeWebView\Application",
    "C:\Program Files\Microsoft\EdgeWebView\Application"
) -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Select-Object -ExpandProperty FullName

$tlbImp = Find-TlbImp

$report = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    RepoRoot = $repoRoot.Path
    DotNet = Get-CommandVersion "dotnet"
    Node = Get-CommandVersion "node"
    Npm = Get-CommandVersion "npm"
    Net48TargetingPack = Test-PathItem "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
    WebPackageJson = Test-PathItem $webPackageJson
    WebUiIndex = Test-PathItem $webUiIndex
    CorelRoots = $corelRoots | ForEach-Object { Test-PathItem $_ }
    CorelDrawTypeLibs = @($typeLibs)
    WebView2RuntimePaths = @($webViewRuntime)
    TlbImp = [pscustomobject]@{
        Exists = $tlbImp -ne $null
        Path = if ($tlbImp) { $tlbImp.FullName } else { $null }
    }
    ConfigDirectory = Test-WritableDirectory $configDirectory
    LogDirectory = Test-WritableDirectory $logDirectory
    SettingsJson = Test-JsonFile $settingsFile
}

$fatalFailures = @()
if (-not $report.DotNet.Exists) { $fatalFailures += "dotnet is missing or unavailable." }
if (-not $report.Net48TargetingPack.Exists) { $fatalFailures += ".NET Framework 4.8 targeting pack is missing." }
if (-not $report.WebUiIndex.Exists) { $fatalFailures += "WebUI build output is missing: $webUiIndex" }
if ($report.WebView2RuntimePaths.Count -eq 0) { $fatalFailures += "WebView2 Runtime is missing; WPF fallback will be required." }
if (-not $report.ConfigDirectory.Writable) { $fatalFailures += "Config directory is not writable: $configDirectory" }
if (-not $report.LogDirectory.Writable) { $fatalFailures += "Log directory is not writable: $logDirectory" }
if ($report.SettingsJson.Exists -and $report.SettingsJson.ValidJson -eq $false) { $fatalFailures += "settings.json is not valid JSON: $settingsFile" }

$status = if ($fatalFailures.Count -eq 0) { "OK" } else { "FAILED" }
$report | Add-Member -NotePropertyName FatalFailures -NotePropertyValue @($fatalFailures)
$report | Add-Member -NotePropertyName Status -NotePropertyValue $status

if ($Json) {
    $report | ConvertTo-Json -Depth 6
    if ($FailOnError -and $fatalFailures.Count -gt 0) {
        exit 1
    }

    exit 0
}

Write-Host "QiTuCDR environment diagnostics" -ForegroundColor Cyan
Write-Host "Time: $($report.Timestamp)"
Write-Host "Repo: $($report.RepoRoot)"
Write-Host ""

foreach ($tool in @($report.DotNet, $report.Node, $report.Npm)) {
    $toolStatus = if ($tool.Exists) { "OK" } else { "MISSING" }
    Write-Host ("{0,-8} {1,-8} {2}" -f $tool.Command, $toolStatus, $tool.Version)
}

Write-Host ""
Write-Host (".NET Framework 4.8 targeting pack: {0}" -f $(if ($report.Net48TargetingPack.Exists) { "OK" } else { "MISSING" }))

Write-Host ""
Write-Host "Web build output:"
Write-Host ("  web/package.json: {0}" -f $(if ($report.WebPackageJson.Exists) { "OK" } else { "MISSING" }))
Write-Host ("  src/WebUI/index.html: {0}" -f $(if ($report.WebUiIndex.Exists) { "OK" } else { "MISSING" }))

Write-Host ""
Write-Host "CorelDRAW TypeLib:"
if ($report.CorelDrawTypeLibs.Count -eq 0) {
    Write-Host "  CorelDRAW.tlb was not found." -ForegroundColor Yellow
} else {
    $report.CorelDrawTypeLibs | ForEach-Object { Write-Host "  $_" }
}

Write-Host ""
Write-Host "WebView2 Runtime:"
if ($report.WebView2RuntimePaths.Count -eq 0) {
    Write-Host "  Edge WebView2 Runtime was not found." -ForegroundColor Yellow
} else {
    $report.WebView2RuntimePaths | ForEach-Object { Write-Host "  $_" }
}

Write-Host ""
Write-Host "Interop tooling:"
Write-Host ("  TlbImp.exe: {0}" -f $(if ($report.TlbImp.Exists) { $report.TlbImp.Path } else { "MISSING" }))

Write-Host ""
Write-Host "Local directories:"
Write-Host ("  Config: {0} ({1})" -f $(if ($report.ConfigDirectory.Writable) { "WRITABLE" } else { "FAILED" }), $report.ConfigDirectory.Path)
Write-Host ("  Logs:   {0} ({1})" -f $(if ($report.LogDirectory.Writable) { "WRITABLE" } else { "FAILED" }), $report.LogDirectory.Path)
Write-Host ("  settings.json: {0}" -f $(if (-not $report.SettingsJson.Exists) { "NOT_CREATED" } elseif ($report.SettingsJson.ValidJson) { "VALID" } else { "INVALID" }))
if ($report.SettingsJson.Exists -and $report.SettingsJson.ValidJson) {
    Write-Host ("  PreferTypedCorelInterop: {0}" -f $report.SettingsJson.PreferTypedCorelInterop)
    Write-Host ("  DockHostMode: {0}" -f $report.SettingsJson.DockHostMode)
}

Write-Host ""
Write-Host "Diagnostics status: $($report.Status)" -ForegroundColor $(if ($report.Status -eq "OK") { "Green" } else { "Yellow" })
if ($report.FatalFailures.Count -gt 0) {
    Write-Host "Required actions:" -ForegroundColor Yellow
    $report.FatalFailures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($FailOnError -and $fatalFailures.Count -gt 0) {
    exit 1
}
