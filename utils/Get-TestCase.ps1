function Get-TestCase {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $i = Import-PowerShellDataFile -Path $Path -ErrorAction Stop

    # Are we running elevated
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $elevated = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    $testCases = $i.testCases | Where-Object { -not $_.requiresElevation -or $elevated }

    return $testCases
}