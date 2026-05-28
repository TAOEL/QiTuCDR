param(
    [string]$InstalledAssemblyPath,
    [string]$OutputDirectory,
    [string[]]$TargetCorelVersions = @("23", "24", "25", "26", "27"),
    [switch]$Json
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\registration"
}

function Find-CorelDrawTypeLibs {
    $roots = @(
        "C:\Program Files\Corel",
        "C:\Program Files (x86)\Corel"
    )

    foreach ($root in $roots) {
        if (Test-Path -LiteralPath $root) {
            Get-ChildItem -Path $root -Recurse -Filter "CorelDRAW.tlb" -ErrorAction SilentlyContinue
        }
    }
}

function Get-CorelVersionFromPath {
    param([string]$Path)

    $match = [regex]::Match($Path, "\\CorelDRAW Graphics Suite\\(?<version>\d+)\\")
    if ($match.Success) {
        return $match.Groups["version"].Value
    }

    return $null
}

function Get-CorelProgramsDirectory {
    param([string]$TypeLibPath)

    $directory = Split-Path -Parent $TypeLibPath
    while (-not [string]::IsNullOrWhiteSpace($directory)) {
        if ((Split-Path -Leaf $directory) -like "Programs*") {
            return $directory
        }

        $parent = Split-Path -Parent $directory
        if ($parent -eq $directory) {
            break
        }

        $directory = $parent
    }

    return $null
}

function Get-RegistryValueMap {
    param([Microsoft.Win32.RegistryKey]$Key)

    $values = @{}
    foreach ($name in $Key.GetValueNames()) {
        $valueName = if ([string]::IsNullOrWhiteSpace($name)) { "(Default)" } else { $name }
        $value = $Key.GetValue($name)
        $values[$valueName] = if ($value -eq $null) { $null } else { [string]$value }
    }

    return $values
}

function Convert-RegistryProviderPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    return $Path.
        Replace("Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER", "HKCU:").
        Replace("Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE", "HKLM:")
}

function Get-CorelVersionHintFromRegistryPath {
    param([string]$Path)

    $match = [regex]::Match($Path, "\\CorelDRAW\\(?<version>\d+)\.0\\?")
    if ($match.Success) {
        return $match.Groups["version"].Value
    }

    return ""
}

function Get-RegistryCandidateAssessment {
    param(
        [string]$Path,
        [hashtable]$Values
    )

    $score = 0
    $signals = New-Object "System.Collections.Generic.List[string]"
    $text = $Path

    foreach ($entry in $Values.GetEnumerator()) {
        $text += " $($entry.Key) $($entry.Value)"
    }

    if ($text -match "(?i)add[\s-]?in|addon|plugin") {
        $score += 40
        $signals.Add("add-in/plugin keyword") | Out-Null
    }

    if ($text -match "(?i)docker") {
        $score += 35
        $signals.Add("docker keyword") | Out-Null
    }

    if ($text -match "(?i)vsta|macro|gms|automation") {
        $score += 25
        $signals.Add("automation/macro keyword") | Out-Null
    }

    if ($text -match "(?i)assembly|manifest|load|dll|path") {
        $score += 20
        $signals.Add("assembly/path/load keyword") | Out-Null
    }

    if ($text -match "(?i)workspace") {
        $score += 5
        $signals.Add("workspace keyword") | Out-Null
    }

    if ($signals.Count -eq 0) {
        $signals.Add("weak match") | Out-Null
    }

    return [pscustomobject]@{
        Score = $score
        Reason = ($signals.ToArray() -join ", ")
    }
}

function Search-RegistryTree {
    param(
        [string]$RootPath,
        [int]$MaxDepth = 5
    )

    $results = New-Object "System.Collections.Generic.List[object]"

    if (-not (Test-Path -Path $RootPath)) {
        return @()
    }

    function Visit-Key {
        param(
            [string]$Path,
            [int]$Depth
        )

        if ($Depth -gt $MaxDepth) {
            return
        }

        try {
            $item = Get-Item -Path $Path -ErrorAction Stop
            $leaf = Split-Path -Leaf $Path
            $interestingName = $leaf -match "(?i)(addin|add-in|addon|plugin|vsta|docker|workspace|macro|gms|automation)"
            $valueMap = Get-RegistryValueMap $item
            $interestingValue = $false

            foreach ($entry in $valueMap.GetEnumerator()) {
                if (($entry.Key -match "(?i)(addin|assembly|path|load|manifest|docker|vsta|plugin)") -or ($entry.Value -match "(?i)(addin|assembly|dll|vsta|plugin|docker|qitucdr)")) {
                    $interestingValue = $true
                    break
                }
            }

            if ($interestingName -or $interestingValue) {
                $displayPath = Convert-RegistryProviderPath $Path
                $assessment = Get-RegistryCandidateAssessment $displayPath $valueMap
                $results.Add([pscustomobject]@{
                    Path = $displayPath
                    ProviderPath = $Path
                    Depth = $Depth
                    VersionHint = Get-CorelVersionHintFromRegistryPath $displayPath
                    Score = $assessment.Score
                    Reason = $assessment.Reason
                    Values = [pscustomobject]$valueMap
                }) | Out-Null
            }

            Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | ForEach-Object {
                Visit-Key $_.PSPath ($Depth + 1)
            }
        }
        catch {
        }
    }

    Visit-Key $RootPath 0
    return $results.ToArray()
}

function New-MarkdownReport {
    param(
        [object]$Report,
        [string]$Path
    )

    $lines = New-Object "System.Collections.Generic.List[string]"
    $lines.Add("# QiTuCDR CorelDRAW Registration Plan")
    $lines.Add("")
    $lines.Add("- GeneratedAt: $($Report.GeneratedAt)")
    $lines.Add("- InstalledAssemblyPath: $($Report.InstalledAssemblyPath)")
    $lines.Add("- Status: $($Report.Status)")
    $lines.Add("- EvidenceLevel: $($Report.EvidenceSummary.EvidenceLevel)")
    $lines.Add("- TypeLibCount: $($Report.EvidenceSummary.TypeLibCount)")
    $lines.Add("- RegistryCandidateCount: $($Report.EvidenceSummary.RegistryCandidateCount)")
    $lines.Add("")
    $lines.Add("## Target Version Coverage")
    $lines.Add("")

    if ($Report.TargetVersions.Count -eq 0) {
        $lines.Add("No target CorelDRAW versions were configured.")
    }
    else {
        $lines.Add("| TargetVersion | Found | TypeLibCount |")
        $lines.Add("|---------------|-------|--------------|")
        foreach ($item in $Report.TargetVersions) {
            $lines.Add("| $($item.Version) | $($item.Found) | $($item.TypeLibCount) |")
        }
    }

    $lines.Add("")
    $lines.Add("## CorelDRAW TypeLibs")
    $lines.Add("")

    if ($Report.TypeLibs.Count -eq 0) {
        $lines.Add("No CorelDRAW TypeLib was found.")
    }
    else {
        $lines.Add("| Version | TypeLib | ProgramsDirectory |")
        $lines.Add("|---------|---------|-------------------|")
        foreach ($item in $Report.TypeLibs) {
            $lines.Add("| $($item.Version) | $($item.Path) | $($item.ProgramsDirectory) |")
        }
    }

    $lines.Add("")
    $lines.Add("## Registry Candidates")
    $lines.Add("")
    $lines.Add("These candidates are evidence for manual review only. They are not approved registration paths.")
    $lines.Add("")

    if ($Report.RegistryCandidates.Count -eq 0) {
        $lines.Add("No existing CorelDRAW add-in related registry key was found.")
    }
    else {
        $lines.Add("| Score | VersionHint | Reason | Path |")
        $lines.Add("|-------|-------------|--------|------|")
        foreach ($item in $Report.RegistryCandidates) {
            $lines.Add("| $($item.Score) | $($item.VersionHint) | $($item.Reason) | $($item.Path) |")
        }
    }

    $lines.Add("")
    $lines.Add("## Manifest Confirmation Checklist")
    $lines.Add("")
    $lines.Add("Before writing a CONFIRMED manifest, fill these fields from official SDK documentation or target-machine evidence:")
    $lines.Add("")
    foreach ($item in $Report.ManifestFieldChecklist) {
        $lines.Add("- $item")
    }

    $lines.Add("")
    $lines.Add("## Registration Guidance")
    $lines.Add("")
    $lines.Add("- This report is read-only and does not prove the final add-in registration path.")
    $lines.Add("- Confirm the official CorelDRAW add-in registration mechanism for each target version before enabling installer registry writes.")
    $lines.Add("- After confirmation, write the verified paths into a CONFIRMED registration manifest and call installer\\Install-QiTuCDR.ps1 with -CorelDrawRegistrationManifestPath.")
    $lines.Add("- Do not write guessed CorelDRAW registry paths on designer workstations.")
    $lines.Add("")
    $lines.Add("## Next Actions")
    $lines.Add("")
    foreach ($item in $Report.NextActions) {
        $lines.Add("- $item")
    }

    $lines | Set-Content -LiteralPath $Path -Encoding UTF8
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$typeLibs = @(Find-CorelDrawTypeLibs | ForEach-Object {
    [pscustomobject]@{
        Version = Get-CorelVersionFromPath $_.FullName
        Path = $_.FullName
        ProgramsDirectory = Get-CorelProgramsDirectory $_.FullName
    }
})

$registryRoots = @(
    "HKCU:\Software\Corel",
    "HKLM:\Software\Corel",
    "HKCU:\Software\WOW6432Node\Corel",
    "HKLM:\Software\WOW6432Node\Corel"
)

$registryCandidates = foreach ($root in $registryRoots) {
    Search-RegistryTree $root 5
}

$registryCandidates = @($registryCandidates | Sort-Object Score, Path -Descending)

$normalizedTargetVersions = @(
    $TargetCorelVersions |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { [string]$_ } |
        Select-Object -Unique
)

$targetVersions = foreach ($version in $normalizedTargetVersions) {
    $matches = @($typeLibs | Where-Object { $_.Version -eq $version })
    [pscustomobject]@{
        Version = $version
        Found = $matches.Count -gt 0
        TypeLibCount = $matches.Count
    }
}

$missingTargetVersions = @($targetVersions | Where-Object { -not $_.Found } | ForEach-Object { $_.Version })

if ([string]::IsNullOrWhiteSpace($InstalledAssemblyPath)) {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $InstalledAssemblyPath = Join-Path $localAppData "QiTuCDR\App\QiTuCDR.Host.dll"
}

$status = if ($typeLibs.Count -gt 0) { "NEEDS_CONFIRMATION" } else { "CORELDRAW_NOT_FOUND" }
$nextActions = New-Object "System.Collections.Generic.List[string]"

if ($typeLibs.Count -eq 0) {
    $nextActions.Add("Install a supported CorelDRAW version or run the report on a designer workstation with CorelDRAW installed.") | Out-Null
}
else {
    $nextActions.Add("Validate the official AddIn and Docker registration mechanism for every CorelDRAW version that will be supported.") | Out-Null
    $nextActions.Add("Create a CONFIRMED registration manifest and enable only verified CorelDRAW target versions.") | Out-Null
}

if ($missingTargetVersions.Count -gt 0) {
    $nextActions.Add("Run this report on machines that cover missing target version identifiers: $($missingTargetVersions -join ', ').") | Out-Null
}

$nextActions.Add("Keep registry writes disabled until a verified CorelDRAW SDK or official registration reference confirms the exact path.") | Out-Null

$manifestFieldChecklist = @(
    "CorelVersionIdentifier: target CorelDRAW version identifier, only 23/24/25/26/27.",
    "ProductLabel: visible product name, normally QiTuCDR.",
    "RegistrationKind: AddIn or Docker, based on the confirmed CorelDRAW registration mechanism.",
    "RegistryPath: exact HKCU/HKLM Software Corel path confirmed by official SDK docs or target-machine evidence.",
    "ConfirmationSource: path to the registration confirmation record or official SDK reference.",
    "ConfirmedBy: person who confirmed the registration evidence.",
    "ConfirmedAt: ISO date/time when confirmation was completed."
)

$evidenceLevel = if ($typeLibs.Count -gt 0 -and @($registryCandidates).Count -gt 0) {
    "TYPELIB_AND_REGISTRY_CANDIDATES"
}
elseif ($typeLibs.Count -gt 0) {
    "TYPELIB_ONLY"
}
else {
    "NO_CORELDRAW_EVIDENCE"
}

$report = [pscustomobject]@{
    GeneratedAt = (Get-Date).ToString("o")
    InstalledAssemblyPath = $InstalledAssemblyPath
    EvidenceSummary = [pscustomobject]@{
        EvidenceLevel = $evidenceLevel
        TypeLibCount = $typeLibs.Count
        RegistryCandidateCount = @($registryCandidates).Count
        ConfirmedRegistrationPathCount = 0
    }
    TargetVersions = @($targetVersions)
    MissingTargetVersions = @($missingTargetVersions)
    TypeLibs = @($typeLibs)
    RegistryRoots = $registryRoots
    RegistryCandidates = @($registryCandidates)
    ManifestFieldChecklist = @($manifestFieldChecklist)
    NextActions = @($nextActions.ToArray())
    Status = $status
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonPath = Join-Path $OutputDirectory "qitucdr-coreldraw-registration-plan-$timestamp.json"
$markdownPath = Join-Path $OutputDirectory "qitucdr-coreldraw-registration-plan-$timestamp.md"

$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
New-MarkdownReport $report $markdownPath

if ($Json) {
    $report | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host "QiTuCDR CorelDRAW registration plan" -ForegroundColor Cyan
Write-Host "Status: $status"
Write-Host "TypeLibCount: $($typeLibs.Count)"
Write-Host "RegistryCandidateCount: $(@($registryCandidates).Count)"
Write-Host "Json: $jsonPath"
Write-Host "Markdown: $markdownPath"
