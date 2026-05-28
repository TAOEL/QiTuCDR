param(
    [string]$CorelProgramsDirectory = "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64",
    [string]$SourcePath,
    [string]$AddonName = "QiTuCDR",
    [switch]$Force,
    [switch]$EnableAutoLoad,
    [switch]$Json,
    [switch]$FailOnError
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

function Assert-ChildPath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    if (-not $pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside root: $Path"
    }
}

function Get-DefaultSourcePath {
    $releaseHost = Join-Path $repoRoot "src\Host\bin\Release\net48"
    if (Test-Path -LiteralPath (Join-Path $releaseHost "QiTuCDR.Host.dll")) {
        return $releaseHost
    }

    return (Join-Path $repoRoot "src\Host\bin\Debug\net48")
}

function Get-LoadedAddonProcessIds {
    param(
        [string]$ProgramsDirectory,
        [string]$AddonDirectory
    )

    $programsPrefix = [System.IO.Path]::GetFullPath($ProgramsDirectory).TrimEnd('\') + '\'
    $addonPrefix = [System.IO.Path]::GetFullPath($AddonDirectory).TrimEnd('\') + '\'
    $ids = New-Object "System.Collections.Generic.List[int]"

    foreach ($process in @(Get-Process CorelDRW -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and ([System.IO.Path]::GetFullPath($_.Path).StartsWith($programsPrefix, [System.StringComparison]::OrdinalIgnoreCase))
    })) {
        try {
            $loaded = @($process.Modules | Where-Object {
                $_.FileName -and ([System.IO.Path]::GetFullPath($_.FileName).StartsWith($addonPrefix, [System.StringComparison]::OrdinalIgnoreCase))
            })
            if ($loaded.Count -gt 0) {
                $ids.Add([int]$process.Id) | Out-Null
            }
        }
        catch {
            $ids.Add([int]$process.Id) | Out-Null
        }
    }

    return @($ids)
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Get-DefaultSourcePath
}

$programsFull = [System.IO.Path]::GetFullPath($CorelProgramsDirectory)
$sourceFull = [System.IO.Path]::GetFullPath($SourcePath)
$addonsRoot = Join-Path $programsFull "Addons"
$target = Join-Path $addonsRoot $AddonName
$failures = New-Object "System.Collections.Generic.List[string]"

if (-not (Test-Path -LiteralPath (Join-Path $programsFull "CorelDRW.exe"))) {
    Add-Failure $failures "CorelDRW.exe was not found under CorelProgramsDirectory: $programsFull"
}

if (-not (Test-Path -LiteralPath $addonsRoot)) {
    Add-Failure $failures "Addons directory was not found: $addonsRoot"
}

foreach ($required in @(
    "QiTuCDR.Host.dll",
    "QiTuCDR.Bridge.dll",
    "QiTuCDR.Core.dll",
    "QiTuCDR.Infrastructure.dll",
    "QiTuCDR.Shared.dll",
    "WebUI\index.html"
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourceFull $required))) {
        Add-Failure $failures "Source file is missing: $required"
    }
}

if ($failures.Count -eq 0) {
    if (Test-Path -LiteralPath $target) {
        if (-not $Force) {
            Add-Failure $failures "Target addon directory already exists. Re-run with -Force to replace it: $target"
        }
        else {
            $loadedBy = @(Get-LoadedAddonProcessIds $programsFull $target)
            if ($loadedBy.Count -gt 0) {
                Add-Failure $failures "Target addon is still loaded by CorelDRAW PID(s): $($loadedBy -join ', '). Close target CorelDRAW before replacing files."
            }
        }
    }
}

if ($failures.Count -eq 0) {
    if (Test-Path -LiteralPath $target) {
        if ($Force) {
            Assert-ChildPath $addonsRoot $target
            Remove-Item -LiteralPath $target -Recurse -Force
        }
    }
}

if ($failures.Count -eq 0) {
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Get-ChildItem -LiteralPath $sourceFull -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
    }

    $markerName = if ($EnableAutoLoad) { "CorelDrw.addon" } else { "CorelDrw.addon.disabled" }
    New-Item -ItemType File -Force -Path (Join-Path $target $markerName) | Out-Null

    $itemGuid = "733cfe74-d453-42b8-8d35-cd85c9a2a3c1"
    $barGuid = "9ed84d98-2f9a-48cd-a3f4-c12772e2c424"
    $hostedType = "Addons\$AddonName\QiTuCDR.Host.dll,QiTuCDR.Host.Addons.AddonEntry"
    $appUi = @"
<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:frmwrk="Corel Framework Data">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>
  <frmwrk:uiconfig>
    <frmwrk:applicationInfo userConfiguration="true" />
  </frmwrk:uiconfig>
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="uiConfig/items">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <itemData guid="$itemGuid"
                type="wpfhost"
                hostedType="$hostedType"
                enable="true">
      </itemData>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="uiConfig/commandBars">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <commandBarData guid="$barGuid"
                      nonLocalizableName="QiTuCDR"
                      userCaption="QiTuCDR"
                      locked="false"
                      type="toolbar">
        <toolbar>
          <item guidRef="$itemGuid" dock="top"/>
        </toolbar>
      </commandBarData>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="uiConfig/containers/container[@guid='bee85f91-3ad9-dc8d-48b5-d2a87c8b2109']/container[@guid='Framework_MainFrame-layout']/dockHost[@guid='894bf987-2ec1-8f83-41d8-68f6797d0db4']/toolbar[@guidRef='c2b44f69-6dec-444e-a37e-5dbf7ff43dae']">
    <xsl:copy-of select="."/>
    <toolbar guidRef="$barGuid" dock="top" />
  </xsl:template>
</xsl:stylesheet>
"@
    $userUi = @"
<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:frmwrk="Corel Framework Data"
                exclude-result-prefixes="frmwrk">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>
  <frmwrk:uiconfig>
    <frmwrk:compositeNode xPath="/uiConfig/commandBars/commandBarData[@guid='$barGuid']"/>
  </frmwrk:uiconfig>
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
"@

    $appUi | Set-Content -LiteralPath (Join-Path $target "AppUI.xslt") -Encoding UTF8
    $userUi | Set-Content -LiteralPath (Join-Path $target "UserUI.xslt") -Encoding UTF8

    $manifest = [ordered]@{
        Product = "QiTuCDR"
        InstalledAt = (Get-Date).ToString("o")
        CorelProgramsDirectory = $programsFull
        AddonsRoot = $addonsRoot
        AddonName = $AddonName
        TargetDirectory = $target
        SourcePath = $sourceFull
        HostedType = $hostedType
        Marker = $markerName
        AutoLoadEnabled = [bool]$EnableAutoLoad
    }

    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $target "qitucdr-addon-install-manifest.json") -Encoding UTF8
}

$status = if ($failures.Count -eq 0) { "OK" } else { "FAILED" }
$result = [pscustomobject]@{
    Timestamp = (Get-Date).ToString("o")
    CorelProgramsDirectory = $programsFull
    AddonDirectory = $target
    SourcePath = $sourceFull
    AutoLoadEnabled = [bool]$EnableAutoLoad
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

Write-Host "QiTuCDR CorelDRAW addon install" -ForegroundColor Cyan
Write-Host "Status: $status" -ForegroundColor $(if ($status -eq "OK") { "Green" } else { "Yellow" })
Write-Host "CorelProgramsDirectory: $programsFull"
Write-Host "AddonDirectory: $target"
Write-Host "AutoLoadEnabled: $([bool]$EnableAutoLoad)"
if ($failures.Count -gt 0) {
    Write-Host "Failures:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Yellow
    }
}

if ($FailOnError -and $failures.Count -gt 0) {
    exit 1
}
