param(
    [string]$Configuration = "Debug",
    [switch]$SkipWeb,
    [switch]$SkipTests,
    [switch]$EnableCorelDrawInterop,
    [string]$CorelDrawInteropDirectory
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

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

Push-Location $repoRoot
try {
    $msbuildProperties = @()
    if ($EnableCorelDrawInterop) {
        $msbuildProperties += "/p:EnableCorelDrawInterop=true"
    }

    if (-not [string]::IsNullOrWhiteSpace($CorelDrawInteropDirectory)) {
        $msbuildProperties += "/p:CorelDrawInteropDirectory=$CorelDrawInteropDirectory"
    }

    $hostHarnessRunning = Get-Process -Name "QiTuCDR.HostHarness" -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited }
    if ($hostHarnessRunning) {
        Write-Host "==> HostHarness is running; solution build will skip HostHarness output copy." -ForegroundColor Yellow
    }

    if (-not $SkipWeb) {
        Write-Host "==> npm install/build" -ForegroundColor Cyan
        Push-Location (Join-Path $repoRoot "web")
        try {
            Invoke-CheckedCommand npm install
            Invoke-CheckedCommand npm run build
        }
        finally {
            Pop-Location
        }
    }

    Write-Host "==> dotnet restore" -ForegroundColor Cyan
    Invoke-CheckedCommand dotnet restore QiTuCDR.sln @msbuildProperties

    Write-Host "==> dotnet build ($Configuration)" -ForegroundColor Cyan
    if ($hostHarnessRunning) {
        Invoke-CheckedCommand dotnet build "src\Host\QiTuCDR.Host.csproj" --configuration $Configuration --no-restore @msbuildProperties
        Invoke-CheckedCommand dotnet build "tests\Unit\QiTuCDR.Tests\QiTuCDR.Tests.csproj" --configuration $Configuration --no-restore @msbuildProperties
    }
    else {
        Invoke-CheckedCommand dotnet build QiTuCDR.sln --configuration $Configuration --no-restore @msbuildProperties
    }

    if (-not $SkipTests) {
        Write-Host "==> dotnet test ($Configuration)" -ForegroundColor Cyan
        if ($hostHarnessRunning) {
            Invoke-CheckedCommand dotnet test "tests\Unit\QiTuCDR.Tests\QiTuCDR.Tests.csproj" --configuration $Configuration --no-build
        }
        else {
            Invoke-CheckedCommand dotnet test QiTuCDR.sln --configuration $Configuration --no-build
        }
    }

    Write-Host "QiTuCDR build verification completed." -ForegroundColor Green
}
finally {
    Pop-Location
}
