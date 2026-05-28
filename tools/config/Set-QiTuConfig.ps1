param(
    [string]$SettingsPath,
    [switch]$EnableTypedInterop,
    [switch]$DisableTypedInterop,
    [switch]$AllowOfficialCorelDockerAdapter,
    [switch]$DisableOfficialCorelDockerAdapter,
    [int]$WebViewPreheatDelayMs = -1,
    [int]$BatchSize = -1,
    [int]$TaskTimeoutMs = -1,
    [string]$DockHostMode,
    [switch]$Json
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

function Get-DefaultSettingsPath {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return Join-Path $localAppData "QiTuCDR\Config\settings.json"
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

function Get-PropertyValue {
    param(
        [object]$Source,
        [string[]]$Names,
        [object]$DefaultValue
    )

    foreach ($name in $Names) {
        $property = $Source.PSObject.Properties[$name]
        if ($property) {
            return $property.Value
        }
    }

    return $DefaultValue
}

function Read-Config {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return New-DefaultConfig
    }

    try {
        $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $source = $raw | ConvertFrom-Json

        $nativePanel = Get-PropertyValue $source @("NativePanel", "nativePanel") $null
        if ($null -eq $nativePanel) {
            $nativePanel = (New-DefaultConfig).NativePanel
        }

        return [ordered]@{
            WebViewPreheatDelayMs = [int](Get-PropertyValue $source @("WebViewPreheatDelayMs", "webViewPreheatDelayMs") 4000)
            BatchSize = [int](Get-PropertyValue $source @("BatchSize", "batchSize") 50)
            TaskTimeoutMs = [int](Get-PropertyValue $source @("TaskTimeoutMs", "taskTimeoutMs") 120000)
            PreferTypedCorelInterop = [bool](Get-PropertyValue $source @("PreferTypedCorelInterop", "preferTypedCorelInterop") $false)
            AllowOfficialCorelDockerAdapter = [bool](Get-PropertyValue $source @("AllowOfficialCorelDockerAdapter", "allowOfficialCorelDockerAdapter") $false)
            DockHostMode = [string](Get-PropertyValue $source @("DockHostMode", "dockHostMode") "Debug")
            NativePanel = [ordered]@{
                WindowTopmost = [bool](Get-PropertyValue $nativePanel @("WindowTopmost", "windowTopmost") $false)
                SaveWindowPosition = [bool](Get-PropertyValue $nativePanel @("SaveWindowPosition", "saveWindowPosition") $true)
                SaveToolSettings = [bool](Get-PropertyValue $nativePanel @("SaveToolSettings", "saveToolSettings") $true)
                AutoBackupOriginalFile = [bool](Get-PropertyValue $nativePanel @("AutoBackupOriginalFile", "autoBackupOriginalFile") $false)
                ShowTaskCompletedToast = [bool](Get-PropertyValue $nativePanel @("ShowTaskCompletedToast", "showTaskCompletedToast") $true)
                ToolWindowPositions = Get-PropertyValue $nativePanel @("ToolWindowPositions", "toolWindowPositions") @{}
                PopupWindowPositions = Get-PropertyValue $nativePanel @("PopupWindowPositions", "popupWindowPositions") @{}
            }
        }
    }
    catch {
        $backupPath = $Path + ".bad." + (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
        Copy-Item -LiteralPath $Path -Destination $backupPath -Force
        Write-Warning "Invalid settings JSON was backed up to: $backupPath"
        return New-DefaultConfig
    }
}

function Save-Config {
    param(
        [string]$Path,
        [object]$Config
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $Config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}

if ($EnableTypedInterop -and $DisableTypedInterop) {
    throw "Use only one of -EnableTypedInterop or -DisableTypedInterop."
}

if ($AllowOfficialCorelDockerAdapter -and $DisableOfficialCorelDockerAdapter) {
    throw "Use only one of -AllowOfficialCorelDockerAdapter or -DisableOfficialCorelDockerAdapter."
}

if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
    $SettingsPath = Get-DefaultSettingsPath
}

$config = Read-Config $SettingsPath
$existsBeforeSave = Test-Path -LiteralPath $SettingsPath
$changed = $false

if ($EnableTypedInterop) {
    $config.PreferTypedCorelInterop = $true
    $changed = $true
}

if ($DisableTypedInterop) {
    $config.PreferTypedCorelInterop = $false
    $changed = $true
}

if ($AllowOfficialCorelDockerAdapter) {
    $config.AllowOfficialCorelDockerAdapter = $true
    $changed = $true
}

if ($DisableOfficialCorelDockerAdapter) {
    $config.AllowOfficialCorelDockerAdapter = $false
    $changed = $true
}

if ($WebViewPreheatDelayMs -ge 0) {
    $config.WebViewPreheatDelayMs = $WebViewPreheatDelayMs
    $changed = $true
}

if ($BatchSize -ge 0) {
    $config.BatchSize = $BatchSize
    $changed = $true
}

if ($TaskTimeoutMs -ge 0) {
    $config.TaskTimeoutMs = $TaskTimeoutMs
    $changed = $true
}

if (-not [string]::IsNullOrWhiteSpace($DockHostMode)) {
    $normalizedDockHostMode = $DockHostMode.Trim()
    if ($normalizedDockHostMode -ne "Debug" -and $normalizedDockHostMode -ne "CorelDocker") {
        throw "DockHostMode must be Debug or CorelDocker."
    }

    $config.DockHostMode = $normalizedDockHostMode
    $changed = $true
}

$saved = $false
if ($changed -or -not $existsBeforeSave) {
    Save-Config $SettingsPath $config
    $saved = $true
}

$result = [pscustomobject]@{
    SettingsPath = $SettingsPath
    Changed = $changed
    Saved = $saved
    Created = -not $existsBeforeSave
    Config = [pscustomobject]$config
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
    exit 0
}

Write-Host "QiTuCDR config" -ForegroundColor Cyan
Write-Host "Path: $SettingsPath"
Write-Host ("Changed: {0}" -f $changed)
Write-Host ("Saved: {0}" -f $saved)
Write-Host ("Created: {0}" -f (-not $existsBeforeSave))
Write-Host ""
Write-Host ("WebViewPreheatDelayMs: {0}" -f $config.WebViewPreheatDelayMs)
Write-Host ("BatchSize: {0}" -f $config.BatchSize)
Write-Host ("TaskTimeoutMs: {0}" -f $config.TaskTimeoutMs)
Write-Host ("PreferTypedCorelInterop: {0}" -f $config.PreferTypedCorelInterop)
Write-Host ("AllowOfficialCorelDockerAdapter: {0}" -f $config.AllowOfficialCorelDockerAdapter)
Write-Host ("DockHostMode: {0}" -f $config.DockHostMode)
Write-Host ("NativePanel.WindowTopmost: {0}" -f $config.NativePanel.WindowTopmost)
Write-Host ("NativePanel.SaveWindowPosition: {0}" -f $config.NativePanel.SaveWindowPosition)
Write-Host ("NativePanel.SaveToolSettings: {0}" -f $config.NativePanel.SaveToolSettings)
if ($config.AllowOfficialCorelDockerAdapter) {
    Write-Warning "Official CorelDRAW Docker adapter is an unvalidated API shell. Keep this disabled outside controlled diagnostics."
}
