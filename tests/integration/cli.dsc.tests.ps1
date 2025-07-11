[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectName
)

$script:root = Split-Path (Split-Path -Path $PSScriptRoot -Parent)
$script:outputDirectory = Join-Path $root 'output'

Write-Verbose "Running tests for project '$ProjectName' in directory '$root'" -Verbose

# Import helper function 
. (Join-Path $root 'utils' 'Get-TestCase.ps1')

BeforeDiscovery {
    if (-not (Get-Variable -Name 'DSC_RESOURCE_PATH' -ErrorAction Ignore)) {
        $env:DSC_RESOURCE_PATH = $outputDirectory
    } else {
        $env:DSC_RESOURCE_PATH += [System.IO.Path]::PathSeparator + $outputDirectory
    }

    $testData = Join-Path $root 'src' $ProjectName 'data' 'testData.psd1'

    $skip = $false
    if (-not (Test-Path -Path $testData)) {
        Write-Warning "Test data file '$testData' not found. Skipping tests."
        $skip = $true
    } else {
        $testCases = Get-TestCase -Path $testData
    }
}

Describe 'Discovery tests' {
    It 'Can be discovered by dsc.exe' {
        # Find the project file and extract the assmebly name + resource name
        $projectFile = Get-ChildItem -Path $root -Recurse -Filter "$ProjectName.csproj" -File -ErrorAction Ignore | Select-Object -First 1
        [xml]$projectContent = Get-Content -Path $projectFile.FullName -ErrorAction Stop
        $assemblyName = $projectContent.Project.PropertyGroup.AssemblyName
        $manifestFile = Join-Path $outputDirectory "$assemblyName.dsc.resource.json"
        # For other tests
        $script:resourceName = Get-Content -Path $manifestFile -ErrorAction Stop | ConvertFrom-Json | Select-Object -ExpandProperty type 

        $out = dsc resource list | ConvertFrom-Json
        $out.type | Should -Contain $resourceName
    }
}

Describe 'Dynamic tests' {
    It 'Should execute <operation> successfully' -TestCases $testCases -Skip:$skip {
        param ($operation, $testData)

        foreach ($d in $testData) {
            $jsonInput = $d | ConvertTo-Json -Depth 5
            $out = dsc resource $operation --resource $resourceName --input $jsonInput | ConvertFrom-Json
            $LASTEXITCODE | Should -Be 0
            if ($operation -ne 'delete') {
                $out | Should -Not -BeNullOrEmpty    
            } else {
                $out | Should -BeNullOrEmpty
            }
            
        }
    }
}

AfterAll {
    Remove-Item -Path env:\DSC_RESOURCE_PATH -ErrorAction Ignore
}