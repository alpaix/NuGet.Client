[CmdletBinding(DefaultParameterSetName='RegularBuild')]
param (
    [ValidateSet("debug", "release")]
    [string]$Configuration = 'debug',
    [ValidateSet("Release","rtm", "rc", "beta", "local")]
    [string]$ReleaseLabel = 'local',
    [int]$BuildNumber,
    [switch]$SkipRestore,
    [switch]$CleanCache,
    [string]$MSPFXPath,
    [string]$NuGetPFXPath,
    [switch]$SkipXProj,
    [switch]$SkipCSProj,
    [Parameter(ParameterSetName='RegularBuild')]
    [switch]$SkipSubModules,
    [Parameter(ParameterSetName='RegularBuild')]
    [switch]$SkipTests,
    [Parameter(ParameterSetName='RegularBuild')]
    [switch]$SkipILMerge,
    [Parameter(ParameterSetName='FastBuild')]
    [switch]$Fast
)

# For TeamCity - Incase any issue comes in this script fail the build. - Be default TeamCity returns exit code of 0 for all powershell even if it fails
trap
{
    Write-Host "Build failed: $_" -ForegroundColor Red
    Write-Host $_.Exception -ForegroundColor Red
    Write-Host ("`r`n" * 3)
    exit 1
}

. "$PSScriptRoot\build\common.ps1"

# Move to the script directory
pushd $NuGetClientRoot

& "$PSScriptRoot\build\nuget-make.ps1" -BuildNumber $BuildNumber -Opts @{
    Configuration = $Configuration
    ReleaseLabel = $ReleaseLabel
    SkipRestore = $SkipRestore.IsPresent
    CleanCache =$CleanCache.IsPresent
    MSPFXPath = $MSPFXPath
    NuGetPFXPath = $NuGetPFXPath
    SkipXProj = $SkipXProj.IsPresent
    SkipCSProj = $SkipCSProj.IsPresent
    SkipSubModules = $SkipSubModules.IsPresent
    SkipILMerge = $SkipILMerge.IsPresent
    Fast = $Fast.IsPresent
    RunTests = (-not $SkipTests) -and (-not $Fast)
}

popd