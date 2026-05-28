param(
    [string]$TypeLibPath,
    [string]$TlbImpPath,
    [string]$OutputDirectory,
    [string]$AssemblyName,
    [switch]$Force,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Find-CorelTypeLib {
    $roots = @(
        "C:\Program Files\Corel",
        "C:\Program Files (x86)\Corel"
    )

    $items = foreach ($root in $roots) {
        if (Test-Path -LiteralPath $root) {
            Get-ChildItem -Path $root -Recurse -Filter "CorelDRAW.tlb" -ErrorAction SilentlyContinue
        }
    }

    $items |
        Sort-Object -Property FullName -Descending |
        Select-Object -First 1
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

function Get-CorelMajorVersion {
    param([string]$Path)

    $match = [regex]::Match($Path, "\\CorelDRAW Graphics Suite\\(?<version>\d+)\\")
    if ($match.Success) {
        return $match.Groups["version"].Value
    }

    return "Unknown"
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($TypeLibPath)) {
    $typeLib = Find-CorelTypeLib
    if (-not $typeLib) {
        throw "CorelDRAW.tlb was not found. Install CorelDRAW or pass -TypeLibPath."
    }

    $TypeLibPath = $typeLib.FullName
}

if (-not (Test-Path -LiteralPath $TypeLibPath)) {
    throw "TypeLib path does not exist: $TypeLibPath"
}

if ([string]::IsNullOrWhiteSpace($TlbImpPath)) {
    $tlbImp = Find-TlbImp
    if (-not $tlbImp) {
        throw "TlbImp.exe was not found. Install the .NET Framework SDK or pass -TlbImpPath."
    }

    $TlbImpPath = $tlbImp.FullName
}

if (-not (Test-Path -LiteralPath $TlbImpPath)) {
    throw "TlbImp.exe path does not exist: $TlbImpPath"
}

$corelVersion = Get-CorelMajorVersion $TypeLibPath
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\coreldraw-interop\v$corelVersion"
}

if ([string]::IsNullOrWhiteSpace($AssemblyName)) {
    $AssemblyName = "CorelDRAW$corelVersion.Interop.dll"
}

$outputPath = Join-Path $OutputDirectory $AssemblyName

$result = [pscustomobject]@{
    TypeLibPath = $TypeLibPath
    TlbImpPath = $TlbImpPath
    OutputDirectory = $OutputDirectory
    OutputPath = $outputPath
    CorelMajorVersion = $corelVersion
    GeneratedFiles = @()
}

Write-Host "CorelDRAW Interop generation plan" -ForegroundColor Cyan
Write-Host "TypeLib: $TypeLibPath"
Write-Host "TlbImp:  $TlbImpPath"
Write-Host "Output:  $outputPath"

if ($WhatIf) {
    $result
    exit 0
}

if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "Output already exists. Pass -Force to overwrite: $outputPath"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
if ((Test-Path -LiteralPath $outputPath) -and $Force) {
    Remove-Item -LiteralPath $outputPath -Force
}

Push-Location $OutputDirectory
try {
    Invoke-CheckedCommand $TlbImpPath `
        $TypeLibPath `
        "/out:$outputPath" `
        "/namespace:CorelDRAW.Interop" `
        "/machine:X64" `
        "/silent"
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $outputPath)) {
    throw "TlbImp completed but output was not found: $outputPath"
}

$generatedFiles = Get-ChildItem -Path $OutputDirectory -File | Select-Object -ExpandProperty FullName
$result.GeneratedFiles = @($generatedFiles)

Write-Host "Generated: $outputPath" -ForegroundColor Green
$result
