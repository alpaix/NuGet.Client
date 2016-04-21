. "$PSScriptRoot\..\..\build\common.ps1"

Properties {
    $FuncTestRoot = Join-Path $NuGetClientRoot "test\\NuGet.Core.FuncTests"
    $CleanCache = $False
}

Step 'Updating sub-modules' { Update-SubModules }

Step 'Cleaning package cache' { Clear-PackageCache } `
    -skip { return (-not $CleanCache) }

Step 'Installing NuGet.exe' { Install-NuGet }

Step 'Restoring solution packages' { Restore-SolutionPackages }

Step 'Installing dotnet CLI' { Install-DotnetCLI }

Step 'Restoring projects' { Restore-XProjects }

Step 'Running tests' {
    $xtests = Find-XProjects $FuncTestRoot
    $xtests | Test-XProject
}