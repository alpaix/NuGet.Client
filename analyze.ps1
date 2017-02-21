[CmdletBinding()]
param (
    [ValidateSet("debug", "release")]
    [Alias('c')]
    [string]$Configuration = 'Debug',
    [string]$ReleaseLabel = 'zlocal',
    [switch]$CI
)

. "$PSScriptRoot\build\common.ps1"

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

$FilesWithIssues = Select-Xml $reportPath -XPath './/Issue' | select -ExpandProperty Node | %{ Join-Path $_.Path $_.File } | Get-Unique

$LastCommit = git rev-parse --short HEAD

$ChangedFiles = git diff --name-only "$LastCommit^" | %{ Join-Path $NuGetClientRoot $_ }

$ChangedFiles | ?{ $FilesWithIssues -contains $_ }