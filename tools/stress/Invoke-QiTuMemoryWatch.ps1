param(
    [double]$DurationHours = 24,
    [int]$IntervalSeconds = 60,
    [int]$SampleCount = 0,
    [string[]]$ProcessName = @("CorelDRW", "QiTuCDR.HostHarness"),
    [double]$MaxGrowthMB = 50,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\stress"
}

if ($DurationHours -le 0) {
    throw "DurationHours must be greater than 0."
}

if ($IntervalSeconds -le 0) {
    throw "IntervalSeconds must be greater than 0."
}

if ($SampleCount -lt 0) {
    throw "SampleCount must be greater than or equal to 0."
}

if ($MaxGrowthMB -lt 0) {
    throw "MaxGrowthMB must be greater than or equal to 0."
}

function Convert-ToMegabytes {
    param([long]$Bytes)
    return [Math]::Round($Bytes / 1MB, 2)
}

function Add-Section {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title
    )

    $Lines.Add("")
    $Lines.Add("## $Title")
    $Lines.Add("")
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$csvPath = Join-Path $OutputDirectory "qitucdr-memory-watch-$timestamp.csv"
$reportPath = Join-Path $OutputDirectory "qitucdr-memory-watch-$timestamp.md"
$rows = New-Object "System.Collections.Generic.List[object]"

if ($SampleCount -gt 0) {
    $totalSamples = $SampleCount
}
else {
    $durationSeconds = [Math]::Ceiling($DurationHours * 3600)
    $totalSamples = [Math]::Max(1, [int][Math]::Ceiling($durationSeconds / $IntervalSeconds) + 1)
}

Write-Host "QiTuCDR memory watch started. Samples: $totalSamples, interval: $IntervalSeconds seconds." -ForegroundColor Cyan

for ($i = 0; $i -lt $totalSamples; $i++) {
    $sampleTime = Get-Date

    foreach ($name in $ProcessName) {
        $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
        foreach ($process in $processes) {
            $startTimeText = ""
            try {
                $startTimeText = $process.StartTime.ToString("o")
            }
            catch {
                $startTimeText = ""
            }

            $rows.Add([pscustomobject]@{
                Timestamp = $sampleTime.ToString("o")
                ProcessName = $process.ProcessName
                Id = $process.Id
                WorkingSetMB = Convert-ToMegabytes $process.WorkingSet64
                PrivateMemoryMB = Convert-ToMegabytes $process.PrivateMemorySize64
                PagedMemoryMB = Convert-ToMegabytes $process.PagedMemorySize64
                StartTime = $startTimeText
            })
        }
    }

    Write-Host ("Sample {0}/{1}: {2} row(s)" -f ($i + 1), $totalSamples, $rows.Count)

    if ($i -lt ($totalSamples - 1)) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}

$rows | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $csvPath

$lines = New-Object "System.Collections.Generic.List[string]"
$lines.Add("# QiTuCDR M8 Memory Watch Report")
$lines.Add("")
$lines.Add("- GeneratedAt: $(Get-Date -Format o)")
$lines.Add("- RepoRoot: $repoRoot")
$lines.Add("- DurationHours: $DurationHours")
$lines.Add("- IntervalSeconds: $IntervalSeconds")
$lines.Add("- SampleCount: $totalSamples")
$lines.Add("- MaxGrowthMB: $MaxGrowthMB")
$lines.Add("- ProcessName: $($ProcessName -join ', ')")
$lines.Add("- CsvPath: $csvPath")

Add-Section $lines "Sampling Result"

if ($rows.Count -eq 0) {
    $lines.Add("No target process was detected. Start CorelDRAW or HostHarness and run again.")
}
else {
    $lines.Add("| Process | PID | First Private MB | Last Private MB | Growth MB | First Working Set MB | Last Working Set MB | Result |")
    $lines.Add("|------|-----|----------------|----------------|---------|--------------|--------------|------|")

    $failed = $false
    $groups = $rows | Group-Object ProcessName, Id
    foreach ($group in $groups) {
        $ordered = $group.Group | Sort-Object Timestamp
        $first = $ordered | Select-Object -First 1
        $last = $ordered | Select-Object -Last 1
        $growth = [Math]::Round(($last.PrivateMemoryMB - $first.PrivateMemoryMB), 2)
        $result = "PASSED"

        if ($growth -gt $MaxGrowthMB) {
            $result = "FAILED"
            $failed = $true
        }

        $lines.Add("| $($first.ProcessName) | $($first.Id) | $($first.PrivateMemoryMB) | $($last.PrivateMemoryMB) | $growth | $($first.WorkingSetMB) | $($last.WorkingSetMB) | $result |")
    }

    Add-Section $lines "Result"

    if ($failed) {
        $lines.Add("FAILED: At least one target process exceeded the private-memory growth threshold.")
    }
    else {
        $lines.Add("PASSED: Target process private-memory growth stayed within the threshold.")
    }
}

Add-Section $lines "Manual Host Notes"
$lines.Add("- CorelDRAW version:")
$lines.Add("- Windows version:")
$lines.Add("- WebView2 Runtime version:")
$lines.Add("- Typed Interop enabled:")
$lines.Add("- Test document shape count:")
$lines.Add("- Crash, freeze, or error dialog:")
$lines.Add("- QiTuCDR log path:")

$lines | Out-File -Encoding UTF8 -FilePath $reportPath

Write-Host "Memory watch CSV: $csvPath" -ForegroundColor Green
Write-Host "Memory watch report: $reportPath" -ForegroundColor Green

if ($rows.Count -eq 0) {
    exit 2
}

$reportText = $lines -join "`n"
if ($reportText -match "FAILED") {
    exit 1
}
