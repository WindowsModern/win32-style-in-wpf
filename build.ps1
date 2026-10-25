<#
.SYNOPSIS
    Builds PresentationFramework.Theme and runs the demo self-tests.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -SelfTest
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
            -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found) { return $found }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    throw 'MSBuild was not found.'
}

function Invoke-ThemeSelfTest {
    param([string] $Mode)

    $executable = Join-Path $root "ThemeDemo\bin\$Configuration\ThemeDemo.exe"
    $outputDirectory = Join-Path $root "artifacts\selftest-$Mode"
    if (Test-Path $outputDirectory) {
        Remove-Item $outputDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Force $outputDirectory | Out-Null

    if ($Mode -eq 'transition') {
        & $executable --transition --selftest "--out=$outputDirectory"
    }
    else {
        & $executable --selftest "--out=$outputDirectory"
    }

    $logPath = Join-Path $outputDirectory 'selftest.log'
    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-Path $logPath) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-Path $logPath)) {
        throw "ThemeDemo ($Mode) did not write $logPath."
    }

    $log = Get-Content $logPath -Encoding UTF8
    Write-Host "--- ThemeDemo ($Mode) ---"
    $log | ForEach-Object { Write-Host $_ }

    if ($log -match 'ASSERT-FAIL|ERROR:') {
        throw "ThemeDemo ($Mode) self-test reported failures."
    }
}

$msbuild = Find-MSBuild
Write-Host "MSBuild: $msbuild"

& $msbuild (Join-Path $root 'PresentationFramework.Theme.sln') /t:Rebuild `
    "/p:Configuration=$Configuration" '/p:Platform=Any CPU' /v:minimal

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Write-Host "Built: $(Join-Path $root "ThemeDemo\bin\$Configuration\ThemeDemo.exe")"

if ($SelfTest) {
    Invoke-ThemeSelfTest -Mode 'plain'
    Invoke-ThemeSelfTest -Mode 'transition'
    Write-Host 'All theme self-tests passed.'
}
