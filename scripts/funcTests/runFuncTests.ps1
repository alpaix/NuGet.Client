[CmdletBinding()]
param (
    [string]$DepBuildBranch="",
    [string]$DepCommitID="",
    [string]$DepBuildNumber="",
    [switch]$CleanCache
)

# For TeamCity - Incase any issue comes in this script fail the build. - Be default TeamCity returns exit code of 0 for all powershell even if it fails
trap
{
    Write-Host "Build failed: $_" -ForegroundColor Red
    Write-Host $_.Exception -ForegroundColor Red
    Write-Host ("`r`n" * 3)
    exit 1
}

$FuncScriptsRoot = Split-Path -Path $PSScriptRoot -Parent
$NuGetClientRoot = Split-Path -Path $FuncScriptsRoot -Parent

. "$NuGetClientRoot\build\common.ps1"

pushd $NuGetClientRoot

Write-Host "Dependent Build Details are as follows:"
Write-Host "Branch: $DepBuildBranch"
Write-Host "Commit ID: $DepCommitID"
Write-Host "Build Number: $DepBuildNumber"
Write-Host ""

& "$NuGetClientRoot\build\nuget.make.ps1" "$PSScriptRoot\funcTests.steps.ps1" -Opts @{
    CleanCache =$CleanCache.IsPresent
}

popd

# Return success
exit 0