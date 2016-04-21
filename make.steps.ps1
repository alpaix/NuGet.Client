. "$PSScriptRoot\build\common.ps1"

Properties {
    $Configuration = 'debug'
    $ReleaseLabel = 'local'
    $SkipRestore = $False
    $CleanCache = $False
    $MSPFXPath = ''
    $NuGetPFXPath = ''
    $SkipXProj = $False
    $SkipCSProj = $False
    $SkipSubModules = $False
    $SkipILMerge = $False
    $Fast = $False
    $RunTests = $False

    $CLIRoot = $PSScriptRoot
    $env:DOTNET_INSTALL_DIR = $CLIRoot
}

Step 'Updating sub-modules' { Update-SubModules } `
    -skip { return ($SkipSubModules -or $Fast) }

Step 'Cleaning artifacts' { Clear-Artifacts } `
    -skip { return $SkipXProj }

Step 'Cleaning nupkgs' { Clear-Nupkgs } `
    -skip { return $SkipXProj }

Step 'Cleaning package cache' { Clear-PackageCache } `
    -skip { return (-not $CleanCache) }

Step 'Installing NuGet.exe' { Install-NuGet }

Step 'Installing dotnet CLI' { Install-DotnetCLI }

# Restoring tools required for build
Step 'Restoring solution packages' { Restore-SolutionPackages } `
    -skip { return $SkipRestore }

Step 'Enabling delayed signing' {
        Enable-DelaySigning $MSPFXPath $NuGetPFXPath
    } `
    -skip { return ((-not $MSPFXPath) -and (-not $NuGetPFXPath)) }

Step 'Building NuGet.Core projects' {
        Build-CoreProjects $Configuration $ReleaseLabel $BuildNumber -SkipRestore:$SkipRestore -Fast:$Fast
    } `
    -skip { return $SkipXProj }

## Building the Tooling solution
Step 'Building NuGet.Clients projects' {
        Build-ClientsProjects $Configuration $ReleaseLabel $BuildNumber -SkipRestore:$SkipRestore -Fast:$Fast
    } `
    -skip { return $SkipCSproj }

Step 'Running NuGet.Core tests' {
        Test-CoreProjects -SkipRestore:$SkipRestore -Fast:$Fast -Configuration $Configuration
    } `
    -skip { return (-not $RunTests) }

Step 'Running NuGet.Clients tests' {
        Test-ClientsProjects $Configuration
    } `
    -skip { return (-not $RunTests) }

Step 'Merging NuGet.exe' {
        Invoke-ILMerge $Configuration
    } `
    -skip { return ($SkipILMerge -or $SkipCSProj -or $Fast) }
