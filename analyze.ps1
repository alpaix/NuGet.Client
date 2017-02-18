. "$PSScriptRoot\build\common.ps1"

$Configuration = 'Debug'
$ReleaseLabel = 'zlocal'
$BuildNumber = (Get-BuildNumber)
$ToolsetVersion = 15

$solutionPath = Join-Path $NuGetClientRoot NuGet.Clients.sln -Resolve
$projectPath = Join-Path $NuGetClientRoot src\NuGet.Clients\NuGet.SolutionRestoreManager\NuGet.SolutionRestoreManager.csproj -Resolve
$ruleSetPath = Join-Path $NuGetClientRoot NuGet.ruleset -Resolve
$reportPath = Join-Path $Artifacts codeAnalysis.xml

Test-BuildEnvironment

Invoke-BuildStep 'Running Code Analysis - VS15 Toolset' {
    Build-ClientsProjectHelper `
        -SolutionOrProject $projectPath `
        -Configuration $Configuration `
        -ReleaseLabel $ReleaseLabel `
        -BuildNumber $BuildNumber `
        -Parameters @{
            'RunCodeAnalysis'='true'
            'CodeAnalysisRuleSet'=$ruleSetPath
            'CodeAnalysisLogFile'=$reportPath
        } `
        -ToolsetVersion 15
}