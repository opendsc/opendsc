[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectName,

    [switch]
    $Test
)

if ($Test.IsPresent)
{
    if (-not (Get-Module -Name 'Pester' -ListAvailable -ErrorAction Ignore))
    {
        $params = @{
            Name            = 'Pester'
            Scope           = 'CurrentUser'
            Repository      = 'PSGallery'
            TrustRepository = $true
            ErrorAction     = 'Stop'
        }
        Install-PSResource @params
    }

    $testFile = "$PSScriptRoot\tests\integration\$ProjectName.tests.ps1"

    if (-not (Test-Path -Path $testFile))
    {
        Write-Warning "Test file not found: $testFile"
        return
    }

    Write-Verbose "Running test file: '$testFile'" -Verbose
    Invoke-Pester -Path $testFile -Output Detailed
}