param(
    [string]$OutputDirectory,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipDiagnostics
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\stress"
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = ($output | Out-String).Trim()
    }
}

function Add-Section {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title,
        [string]$Body
    )

    $Lines.Add("")
    $Lines.Add("## $Title")
    $Lines.Add("")
    $Lines.Add($Body)
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $OutputDirectory "qitucdr-stress-baseline-$timestamp.md"
$lines = New-Object "System.Collections.Generic.List[string]"

$lines.Add("# QiTuCDR M8 Baseline Report")
$lines.Add("")
$lines.Add("- GeneratedAt: $(Get-Date -Format o)")
$lines.Add("- RepoRoot: $repoRoot")
$lines.Add("- Machine: $env:COMPUTERNAME")
$lines.Add("- User: $env:USERNAME")

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        $build = Invoke-CheckedCommand powershell -NoProfile -ExecutionPolicy Bypass -File "build\scripts\Invoke-QiTuBuild.ps1" -SkipTests:$SkipTests
        Add-Section $lines "Build Verification" "ExitCode: $($build.ExitCode)`n`n``````text`n$($build.Output)`n``````"
        if ($build.ExitCode -ne 0) {
            throw "Build verification failed with exit code $($build.ExitCode)."
        }
    }

    if (-not $SkipDiagnostics) {
        $diagnostics = Invoke-CheckedCommand powershell -NoProfile -ExecutionPolicy Bypass -File "tools\diagnostics\Test-QiTuEnvironment.ps1"
        Add-Section $lines "Environment Diagnostics" "ExitCode: $($diagnostics.ExitCode)`n`n``````text`n$($diagnostics.Output)`n``````"
    }

    $webAssets = Get-ChildItem -Path "src\WebUI\assets" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
    Add-Section $lines "WebUI Assets" (($webAssets | Out-String).Trim())

    $processes = Get-Process | Where-Object {
        $_.ProcessName -like "*Corel*" -or $_.ProcessName -like "*QiTuCDR*"
    } | Select-Object ProcessName, Id, WorkingSet64, PrivateMemorySize64, StartTime -ErrorAction SilentlyContinue

    if ($processes) {
        Add-Section $lines "Runtime Process Snapshot" (($processes | Format-Table -AutoSize | Out-String).Trim())
    }
    else {
        Add-Section $lines "Runtime Process Snapshot" "No CorelDRAW or QiTuCDR process was detected."
    }

    Add-Section $lines "Manual CorelDRAW Stress Checklist" @"
- [ ] 5000+ Shape batch convert completes without host crash.
- [ ] Locked and hidden shapes are skipped according to tool options.
- [ ] Panel open/close 100 times does not create extra WebView2 instances.
- [ ] WebView2 crash enters WPF fallback and returns to Ready after recovery.
- [ ] Closing document during task cancels current task and returns standard response.
- [ ] 24-hour idle run memory growth is less than 50 MB.
- [ ] Undo/Redo command groups remain understandable in CorelDRAW.
"@

    $lines | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Host "Stress baseline report: $reportPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
