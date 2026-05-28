param(
    [switch]$NoBuild,
    [int]$PanelStress = 0,
    [int]$RecoveryStress = 0,
    [int]$DocumentCloseStress = 0,
    [int]$HostEventStress = 0,
    [int]$DelayMs = 10,
    [string]$DockHostMode,
    [string]$ReportPath
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$projectPath = Join-Path $repoRoot "tests\HostHarness\QiTuCDR.HostHarness\QiTuCDR.HostHarness.csproj"

Push-Location $repoRoot
try {
    $hostHarnessRunning = Get-Process -Name "QiTuCDR.HostHarness" -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited }
    if ($hostHarnessRunning -and -not $NoBuild) {
        Write-Host "==> HostHarness is already running; skipping build to avoid locked output files." -ForegroundColor Yellow
        $NoBuild = $true
    }

    if (-not $NoBuild) {
        Write-Host "==> dotnet build HostHarness" -ForegroundColor Cyan
        dotnet build $projectPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build HostHarness failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "==> dotnet run HostHarness" -ForegroundColor Cyan
    $runArgs = @("run", "--project", $projectPath, "--no-build")
    if ($PanelStress -gt 0) {
        $runArgs += "--"
        $runArgs += "--panel-stress"
        $runArgs += $PanelStress.ToString()
    }
    elseif ($RecoveryStress -gt 0) {
        $runArgs += "--"
        $runArgs += "--recovery-stress"
        $runArgs += $RecoveryStress.ToString()
    }
    elseif ($DocumentCloseStress -gt 0) {
        $runArgs += "--"
        $runArgs += "--document-close-stress"
        $runArgs += $DocumentCloseStress.ToString()
    }
    elseif ($HostEventStress -gt 0) {
        $runArgs += "--"
        $runArgs += "--host-event-stress"
        $runArgs += $HostEventStress.ToString()
    }

    if ($PanelStress -gt 0 -or $RecoveryStress -gt 0 -or $DocumentCloseStress -gt 0 -or $HostEventStress -gt 0) {
        $runArgs += "--delay-ms"
        $runArgs += $DelayMs.ToString()
        if (-not [string]::IsNullOrWhiteSpace($DockHostMode)) {
            if ($DockHostMode -ne "Debug" -and $DockHostMode -ne "CorelDocker") {
                throw "DockHostMode must be Debug or CorelDocker."
            }

            $runArgs += "--dock-host-mode"
            $runArgs += $DockHostMode
        }

        if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
            $runArgs += "--report"
            $runArgs += $ReportPath
        }
    }

    dotnet @runArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run HostHarness failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
