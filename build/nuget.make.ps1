[CmdletBinding(SupportsShouldProcess=$True)]
param(
    [Parameter(Position=0,Mandatory=$False)]
    [string]$MakeFile = '.\make.steps.ps1',
    [int]$BuildNumber,
    [Alias('opts')]
    [hashtable]$MakeOptions = @{}
)

. "$PSScriptRoot\common.ps1"

Function New-NuGetMakeProperties {
    [Alias("Properties")]
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=$True)]
        [scriptblock]$Properties
    )
    $script:NuGetMakeProperties += $Properties
}

Function New-BuildStep {
    [Alias("Step")]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$True)]
        [string]$Description,
        [Parameter(Mandatory=$True)]
        [scriptblock]$Expression,
        [Parameter(Mandatory=$False)]
        [Alias('args')]
        [object[]]$Arguments,
        [Alias('skip')]
        [scriptblock]$SkipExpression
    )

    $newStep = @{
        Description = $Description
        Expression = $Expression
        Arguments = $Arguments
        SkipExpression = $SkipExpression
    }

    $script:NuGetBuildSteps += $newStep
}

Function Invoke-BuildStep {
    [CmdletBinding(SupportsShouldProcess=$True)]
    param(
        [Parameter(Mandatory=$True)]
        [string]$BuildStep,
        [Parameter(Mandatory=$True)]
        [scriptblock]$Expression,
        [Alias('args')]
        [object[]]$Arguments,
        [Alias('skip')]
        [scriptblock]$SkipExpression
    )
    $SkipExecution = $SkipExpression -and (& $SkipExpression)

    if (-not $SkipExecution) {
        Trace-Log "[BEGIN] $BuildStep"
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $completed = $False
        try {
            if (-not $WhatIf) {
                Invoke-Command $Expression -ArgumentList $Arguments -ErrorVariable err
            }
            $completed = $true
        }
        finally {
            $sw.Stop()
            Reset-Colors
            if ($completed) {
                Trace-Log "[DONE +$(Format-ElapsedTime $sw.Elapsed)] $BuildStep"
            }
            else {
                if (-not $err) {
                    Trace-Log "[STOPPED +$(Format-ElapsedTime $sw.Elapsed)] $BuildStep"
                }
                else {
                    Error-Log "[FAILED +$(Format-ElapsedTime $sw.Elapsed)] $BuildStep"
                }
            }
        }
    }
    else {
        Warning-Log "[SKIP] $BuildStep"
    }
}

Write-Host ("`r`n" * 3)
Trace-Log ('=' * 60)

$startTime = [DateTime]::UtcNow
if (-not $BuildNumber) {
    $BuildNumber = Get-BuildNumber
}
Trace-Log "Build #$BuildNumber started at $startTime in '$NuGetClientRoot'"

$script:NuGetBuildSteps = @()
$script:NuGetMakeProperties = @()
$NuGetBuildErrors = @()

. $MakeFile

$script:NuGetMakeProperties | %{ . $_  }

$MakeOptions.Keys | %{
    if (test-path "variable:\$_") {
        set-item -path "variable:\$_" -value $MakeOptions.$_ | Out-Null
    } else {
        new-item -path "variable:\$_" -value $MakeOptions.$_ | Out-Null
    }
}

$script:NuGetBuildSteps | %{
    Invoke-BuildStep $_.Description $_.Expression $_.Arguments -skip $_.SkipExpression -ev +NuGetBuildErrors
}

Trace-Log ('-' * 60)

## Calculating Build time
$endTime = [DateTime]::UtcNow
Trace-Log "Build #$BuildNumber ended at $endTime"
Trace-Log "Time elapsed $(Format-ElapsedTime ($endTime - $startTime))"

if ($NuGetBuildErrors) {
    Trace-Log "Build's completed with following errors:"
    $NuGetBuildErrors | Out-Default
}

Trace-Log ('=' * 60)

if ($NuGetBuildErrors) {
    Throw $NuGetBuildErrors.Count
}

Write-Host ("`r`n" * 3)