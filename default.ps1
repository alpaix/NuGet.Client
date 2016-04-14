Properties {
    $Configuration = "debug"
    $SkipSubModules = $true
    $Fast = $true
    $SkipXProj = $false
    $SkipRestore = $false
}

. "$PSScriptRoot\build\common.ps1"

Task default -Depends Clean, UpdateSubmodules, InstallDotnetCLI, RestoreSolutionPackages

Task Clean {
    Trace-Log "Running clean step"
    Error-Log "Clean failed"
    "Clean"
}

Task UpdateSubmodules `
    -Description 'Updating sub-modules' `
    -Precondition { return ($SkipSubModules -or $Fast) } `
{
    Update-SubModules
}

Task InstallNuGet `
    -Description 'Installing NuGet.exe' `
{
    Install-NuGet
}

Task InstallDotnetCLI `
    -Description 'Installing dotnet CLI' `
{
    Install-DotnetCLI
}

Task RestoreSolutionPackages `
    -Depends InstallNuGet `
    -Description 'Restoring solution packages' `
    -Precondition { return -not $SkipRestore }
{
    Restore-SolutionPackages
}