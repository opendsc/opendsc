Describe 'TestResource.Options' {
    BeforeAll {
        $configuration = if ($env:BUILD_CONFIGURATION) { $env:BUILD_CONFIGURATION } else { 'Release' }
        $publishPath = Join-Path (Split-Path $PSScriptRoot) "TestResource.Options\bin\$configuration\*\publish" |
            Resolve-Path | Select-Object -ExpandProperty ProviderPath
        $env:DSC_RESOURCE_PATH = $publishPath
        $script:tempTestFile = Join-Path $TestDrive 'test-file.txt'
    }

    Context 'Discovery' {
        BeforeAll {
            $resources = dsc resource list '*OpenDsc.Test/OptionsFile' | ConvertFrom-Json
            $script:optionsResource = $resources | Where-Object { $_.type -eq 'OpenDsc.Test/OptionsFile' }
        }

        It 'Should be found by dsc resource list' {
            $resources.Count | Should -BeGreaterThan 0
            $script:optionsResource | Should -Not -BeNullOrEmpty
        }

        It 'Should have correct resource type' {
            $script:optionsResource.type | Should -Be 'OpenDsc.Test/OptionsFile'
        }

        It 'Should have description' {
            $script:optionsResource.description | Should -Be 'Test resource using JsonSerializerOptions for file existence.'
        }

        It 'Should have tags including options' {
            $script:optionsResource.manifest.tags | Should -Contain 'options'
            $script:optionsResource.manifest.tags | Should -Contain 'test'
            $script:optionsResource.manifest.tags | Should -Contain 'file'
        }
    }

    Context 'Get Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value 'test content'
        }

        It 'Should not return _exist for existing file' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'Should return _exist=false for non-existing file' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'Test Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value 'test content'
        }

        It 'Should return inDesiredState=true when file exists' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $true
            $result.differingProperties | Should -BeNullOrEmpty
        }

        It 'Should return inDesiredState=false when file does not exist' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $false
        }

        It 'Should return differingProperties when not in desired state' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.differingProperties | Should -Contain '_exist'
        }
    }

    Context 'Set Operation' {
        It 'Should create file when it does not exist' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $true
            $result.afterState._exist | Should -BeNullOrEmpty
        }

        It 'Should report changedProperties when creating file' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            $result.changedProperties | Should -Contain '_exist'
        }

        It 'Should delete file when _exist=false' {
            Set-Content -Path $tempTestFile -Value 'test content'
            $jsonInput = @{ path = $tempTestFile; _exist = $false } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $false
            $result.afterState._exist | Should -Be $false
        }

        It 'Should not modify existing file when _exist is omitted' {
            Set-Content -Path $tempTestFile -Value 'test content'
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/OptionsFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $true
            $result.changedProperties | Should -BeNullOrEmpty
        }
    }

    Context 'Delete Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value 'test content'
        }

        It 'Should delete existing file' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            dsc resource delete -r 'OpenDsc.Test/OptionsFile' --input $jsonInput
            Test-Path $tempTestFile | Should -Be $false
        }

        It 'Should not error when deleting non-existent file' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            { dsc resource delete -r 'OpenDsc.Test/OptionsFile' --input $jsonInput } | Should -Not -Throw
        }
    }

    Context 'Export Operation' {
        BeforeAll {
            $env:TEST_EXPORT_DIR = $TestDrive
            $exportTestFile1 = Join-Path $TestDrive 'test-export1.txt'
            $exportTestFile2 = Join-Path $TestDrive 'test-export2.txt'
            Set-Content -Path $exportTestFile1 -Value 'export test 1'
            Set-Content -Path $exportTestFile2 -Value 'export test 2'
            $script:exportResult = dsc resource export -r 'OpenDsc.Test/OptionsFile' | ConvertFrom-Json
        }

        AfterAll {
            $env:TEST_EXPORT_DIR = $null
        }

        It 'Should export all matching test files' {
            $script:exportResult.resources.Count | Should -BeGreaterOrEqual 2
        }

        It 'Should return paths for exported files' {
            $script:exportResult.resources | ForEach-Object { $_.properties.path | Should -Not -BeNullOrEmpty }
        }

        It 'Should set _exist to null for exported files' {
            $script:exportResult.resources | ForEach-Object { $_.properties._exist | Should -BeNullOrEmpty }
        }
    }
}

