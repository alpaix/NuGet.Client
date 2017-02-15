[CmdletBinding()]
param (
    [ValidateSet("debug", "release")]
    [Alias('c')]
    [string]$Configuration = 'Debug',
    [Alias('l')]
    [string]$ReleaseLabel = 'zlocal'
)

. "$PSScriptRoot\build\common.ps1"

Write-Host ("`r`n" * 3)
Trace-Log ('=' * 60)

$BuildNumber = (Get-BuildNumber)
$ToolsetVersion = 15

$solutionPath = Join-Path $NuGetClientRoot NuGet.Clients.sln -Resolve
$ruleSetPath = Join-Path $NuGetClientRoot NuGet.ruleset -Resolve
$reportPath = Join-Path $Artifacts codeAnalysis.xml

Test-BuildEnvironment

Invoke-BuildStep 'Running Code Analysis - VS15 Toolset' {
    Build-ClientsProjectHelper `
        -SolutionOrProject $solutionPath `
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

Trace-Log ('-' * 60)

$FilesWithIssues = Select-Xml $reportPath -XPath './/Issue' | select -ExpandProperty Node | %{ Join-Path $_.Path $_.File } | Get-Unique

if ($FilesWithIssues) {
    Trace-Log "Detected code analysis issues in $($FilesWithIssues.Count) file(s)."

    # Get list of staged and changed files
    $ChangedFiles = & git diff --name-only HEAD
    if (-not $ChangedFiles) {
        # Otherwise get list of files in a recent commit
        $ChangedFiles = & git diff --name-only HEAD^
    }

    $ChangedFiles = $ChangedFiles | %{ Join-Path $NuGetClientRoot $_ }

    Trace-Log "Found total $($ChangedFiles.Count) recently changed file(s)."

    $FixItPlease = $ChangedFiles | ?{ $FilesWithIssues -contains $_ }
    if ($FixItPlease) {
        Error-Log "Detected code analysis issues in recently changed files. Full report is at '$reportPath'." -Fatal
    }
    else {
        Trace-Log "No issues detected in changed files. Full report is at '$reportPath'."
    }
}
else {
    Trace-Log "No issues detected."
}

Trace-Log ('=' * 60)
Write-Host ("`r`n" * 3)