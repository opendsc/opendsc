[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectName,

    [ValidateSet("Release", "Debug")]
    [string]
    $Configuration = "Debug",

    [switch]
    $Publish,

    [switch]
    $Test
)

function getNetPath {
    $dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Ignore | Select-Object -First 1).Source
    if ($null -eq $dotnet) {
        $dotnetPath = Get-ChildItem -Path (Join-Path $env:ProgramFiles 'dotnet' 'sdk' '9.0.*') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (Test-Path -Path $dotnetPath) {
            $dotnet = Join-Path $env:ProgramFiles 'dotnet' 'dotnet.exe'
        } else {
            return $false
        } 
    }

    return $dotnet
}

$outputDirectory = Join-Path $PSScriptRoot 'output'

function getProjectPath ($ProjectName) {
    $projectPath = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "$ProjectName.csproj" -File -ErrorAction Ignore | Select-Object -First 1
    if ($null -eq $projectPath) {
        Write-Error "Project file '$ProjectName.csproj' not found in the script directory or its subdirectories."
    }

    return $projectPath.FullName
}

$dotnet = getNetPath
if (-not $dotnet) {
    Write-Error "Dotnet SDK not found. Please install .NET SDK 9.0 or later."
    return
}

$projectFile = getProjectPath -ProjectName $ProjectName
$build = @(
    'build',
    $projectFile,
    '--nologo',
    '--configuration', $Configuration
)

& $dotnet @build

if ($Publish.IsPresent) {
    # TODO: Should add more parameters if needed
    $publishParams = @(
        'publish',
        $projectFile,
        '--self-contained',
        '--configuration', $Configuration,
        '--output', $outputDirectory
    )

    if ($Configuration -eq 'Release') {
        $publishParams += '/p:DebugType=None'
        $publishParams += '/p:DebugSymbols=False'
    } 

    Write-Verbose "Publishing project '$ProjectName' to '$outputDirectory'" -Verbose
    & $dotnet @publishParams

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish project '$ProjectName'. Exit code: $LASTEXITCODE"
        return
    }
}

if ($Test.IsPresent)
{
    # TODO: We should check if publish is done before running tests
    if (-not (Get-Module -Name 'Pester' -ListAvailable -ErrorAction Ignore))
    {
        $params = @{
            Name            = 'Pester'
            Scope           = 'CurrentUser'
            Repository      = 'PSGallery'
            Version         = '5.7.1'
            TrustRepository = $true
            ErrorAction     = 'Stop'
        }
        Install-PSResource @params
    }

    $testContainerData = @{
        ProjectName = $ProjectName
    }

    Invoke-Pester -Configuration @{
        Run = @{
            Container = New-PesterContainer -Path (Join-Path $PSScriptRoot 'tests' 'integration') -Data $testContainerData
        }
        Output = @{
            Verbosity = 'Detailed'
        }
    } -ErrorAction Stop
}