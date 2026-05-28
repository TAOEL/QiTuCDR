param(
    [int]$Iterations = 3,
    [int]$DelayMs = 50,
    [switch]$NoBuild,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$harnessScript = Join-Path $repoRoot "tools\harness\Start-QiTuHostHarness.ps1"
$harnessProject = Join-Path $repoRoot "tests\HostHarness\QiTuCDR.HostHarness\QiTuCDR.HostHarness.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\stress"
}

if ($Iterations -le 0) {
    throw "Iterations must be greater than 0."
}

if ($DelayMs -lt 0) {
    throw "DelayMs must be greater than or equal to 0."
}

$running = Get-Process -Name "QiTuCDR.HostHarness" -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited }

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportPath = Join-Path $OutputDirectory "qitucdr-document-close-stress-$timestamp.md"

    if ($running -and -not $NoBuild) {
        Write-Host "Existing HostHarness process detected; using isolated temporary output." -ForegroundColor Yellow
        $tempOut = Join-Path $env:TEMP "qitucdr-hostharness-document-close-out-$timestamp"
        New-Item -ItemType Directory -Force -Path $tempOut | Out-Null

        dotnet build $harnessProject /p:OutDir="$tempOut\"
        if ($LASTEXITCODE -ne 0) {
            throw "HostHarness isolated build failed with exit code $LASTEXITCODE."
        }

        $exe = Join-Path $tempOut "QiTuCDR.HostHarness.exe"
        $process = Start-Process -FilePath $exe -ArgumentList @("--document-close-stress", $Iterations.ToString(), "--delay-ms", $DelayMs.ToString(), "--report", $reportPath) -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Document close stress failed with exit code $($process.ExitCode)."
        }
    }
    else {
        $arguments = @(
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            $harnessScript,
            "-DocumentCloseStress",
            $Iterations.ToString(),
            "-DelayMs",
            $DelayMs.ToString(),
            "-ReportPath",
            $reportPath
        )

        if ($NoBuild) {
            $arguments += "-NoBuild"
        }

        powershell @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Document close stress failed with exit code $LASTEXITCODE."
        }
    }

    if (-not (Test-Path $reportPath)) {
        throw "Document close stress did not create report: $reportPath"
    }

    Write-Host "Document close stress report: $reportPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
