Describe 'TestResource.NonAot' {
    BeforeAll {
        $publishPath = Join-Path $PSScriptRoot 'TestResource.NonAot\bin\Release\net9.0\publish'
        $env:DSC_RESOURCE_PATH = $publishPath
        $script:tempTestFile = Join-Path $TestDrive "test-file.txt"
    }

    Context 'Discovery' {
        BeforeAll {
            $resources = dsc resource list '*OpenDsc.Test/NonAotFile' | ConvertFrom-Json
            $script:nonAotResource = $resources | Where-Object { $_.type -eq 'OpenDsc.Test/NonAotFile' }
        }

        It 'Should be found by dsc resource list' {
            $resources.Count | Should -BeGreaterThan 0
            $script:nonAotResource | Should -Not -BeNullOrEmpty
        }

        It 'Should have correct resource type' {
            $script:nonAotResource.type | Should -Be 'OpenDsc.Test/NonAotFile'
        }

        It 'Should have description' {
            $script:nonAotResource.description | Should -Be 'Non-AOT test resource for file existence.'
        }

        It 'Should have tags including non-aot' {
            $script:nonAotResource.manifest.tags | Should -Contain 'non-aot'
            $script:nonAotResource.manifest.tags | Should -Contain 'test'
            $script:nonAotResource.manifest.tags | Should -Contain 'file'
        }
    }

    Context 'Get Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value "test content"
        }

        It 'Should not return _exist for existing file' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'Should return _exist=false for non-existing file' {
            $nonExistentPath = Join-Path $TestDrive "nonexistent.txt"
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'Test Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value "test content"
        }

        It 'Should return inDesiredState=true when file exists' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $true
            $result.differingProperties | Should -BeNullOrEmpty
        }

        It 'Should return inDesiredState=false when file does not exist' {
            $nonExistentPath = Join-Path $TestDrive "nonexistent.txt"
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $false
            $result.differingProperties | Should -Contain '_exist'
        }
    }

    Context 'Set Operation' {
        It 'Should create file when it does not exist' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $true
            $result.afterState._exist | Should -BeNullOrEmpty
        }

        It 'Should report changedProperties when creating file' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            $result.changedProperties | Should -Contain '_exist'
        }

        It 'Should delete file when _exist=false' {
            Set-Content -Path $tempTestFile -Value "test content"
            $jsonInput = @{ path = $tempTestFile; _exist = $false } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $false
            $result.afterState._exist | Should -Be $false
        }

        It 'Should not modify existing file when _exist is omitted' {
            Set-Content -Path $tempTestFile -Value "test content"
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/NonAotFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $true
            $result.changedProperties | Should -BeNullOrEmpty
        }
    }

    Context 'Delete Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value "test content"
        }

        It 'Should delete existing file' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            dsc resource delete -r 'OpenDsc.Test/NonAotFile' --input $jsonInput
            Test-Path $tempTestFile | Should -Be $false
        }

        It 'Should not error when deleting non-existent file' {
            $nonExistentPath = Join-Path $TestDrive "nonexistent.txt"
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            { dsc resource delete -r 'OpenDsc.Test/NonAotFile' --input $jsonInput } | Should -Not -Throw
        }
    }

    Context 'Export Operation' {
        BeforeAll {
            $exportTestFile1 = Join-Path $TestDrive "test-export1.txt"
            $exportTestFile2 = Join-Path $TestDrive "test-export2.txt"
            Set-Content -Path $exportTestFile1 -Value "export test 1"
            Set-Content -Path $exportTestFile2 -Value "export test 2"
            $script:exportResult = dsc resource export -r 'OpenDsc.Test/NonAotFile' | ConvertFrom-Json
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

