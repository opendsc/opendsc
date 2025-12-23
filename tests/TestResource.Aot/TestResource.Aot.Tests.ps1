Describe 'TestResource.Aot' {
    BeforeAll {
        $configuration = if ($env:BUILD_CONFIGURATION) { $env:BUILD_CONFIGURATION } else { 'Release' }
        $publishPath = Join-Path $PSScriptRoot "..\..\artifacts\TestResource.Aot" | Resolve-Path | Select-Object -ExpandProperty ProviderPath
        $env:DSC_RESOURCE_PATH = $publishPath
        $script:tempTestFile = Join-Path $TestDrive 'test-file.txt'
        $exeSuffix = if ($IsWindows) { '.exe' } else { '' }
        $script:resourceExe = Join-Path $publishPath "test-resource-aot$exeSuffix"
    }

    Context 'Discovery' {
        BeforeAll {
            $resources = dsc resource list '*OpenDsc.Test/AotFile' | ConvertFrom-Json
            $script:aotResource = $resources | Where-Object { $_.type -eq 'OpenDsc.Test/AotFile' }
        }

        It 'Should be found by dsc resource list' {
            $resources.Count | Should -BeGreaterThan 0
            $script:aotResource | Should -Not -BeNullOrEmpty
        }

        It 'Should have correct resource type' {
            $script:aotResource.type | Should -Be 'OpenDsc.Test/AotFile'
        }

        It 'Should have description' {
            $script:aotResource.description | Should -Be 'AOT test resource for file existence.'
        }

        It 'Should have tags including aot' {
            $script:aotResource.manifest.tags | Should -Contain 'aot'
            $script:aotResource.manifest.tags | Should -Contain 'test'
            $script:aotResource.manifest.tags | Should -Contain 'file'
        }
    }

    Context 'Schema Command' {
        BeforeAll {
            $script:schema = dsc resource schema -r 'OpenDsc.Test/AotFile' | ConvertFrom-Json
        }

        It 'Should return valid JSON schema' {
            $script:schema | Should -Not -BeNullOrEmpty
            $script:schema.type | Should -Be 'object'
        }

        It 'Should have path property in schema' {
            $script:schema.properties.path | Should -Not -BeNullOrEmpty
        }

        It 'Should have _exist property in schema' {
            $script:schema.properties._exist | Should -Not -BeNullOrEmpty
        }

        It 'Should mark path as required' {
            $script:schema.required | Should -Contain 'path'
        }
    }

    Context 'Manifest from Resource List' {
        BeforeAll {
            $resources = dsc resource list '*OpenDsc.Test/AotFile' | ConvertFrom-Json
            $script:aotResourceManifest = $resources | Where-Object { $_.type -eq 'OpenDsc.Test/AotFile' }
        }

        It 'Should return valid manifest JSON' {
            $script:aotResourceManifest.manifest | Should -Not -BeNullOrEmpty
        }

        It 'Should include get in manifest' {
            $script:aotResourceManifest.manifest.get | Should -Not -BeNullOrEmpty
        }

        It 'Should include set in manifest' {
            $script:aotResourceManifest.manifest.set | Should -Not -BeNullOrEmpty
        }

        It 'Should include test in manifest' {
            $script:aotResourceManifest.manifest.test | Should -Not -BeNullOrEmpty
        }

        It 'Should include delete in manifest' {
            $script:aotResourceManifest.manifest.delete | Should -Not -BeNullOrEmpty
        }

        It 'Should include export in manifest' {
            $script:aotResourceManifest.manifest.export | Should -Not -BeNullOrEmpty
        }

        It 'Should have manifest version' {
            $script:aotResourceManifest.manifest.version | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value 'test content'
        }

        It 'Should not return _exist for existing file' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'Should return _exist=false for non-existing file' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'Test Operation' {
        BeforeEach {
            Set-Content -Path $tempTestFile -Value 'test content'
        }

        It 'Should return inDesiredState=true when file exists' {
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $true
            $result.differingProperties | Should -Be @()
        }

        It 'Should return inDesiredState=false when file does not exist' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.inDesiredState | Should -Be $false
        }

        It 'Should return differingProperties when not in desired state' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.differingProperties | Should -Contain '_exist'
        }
    }

    Context 'Set Operation' {
        It 'Should create file when it does not exist' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $true
            $result.afterState._exist | Should -BeNullOrEmpty
        }

        It 'Should report changedProperties when creating file' {
            Remove-Item $tempTestFile -Force -ErrorAction SilentlyContinue
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            $result.changedProperties | Should -Contain '_exist'
        }

        It 'Should delete file when _exist=false' {
            Set-Content -Path $tempTestFile -Value 'test content'
            $jsonInput = @{ path = $tempTestFile; _exist = $false } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
            Test-Path $tempTestFile | Should -Be $false
            $result.afterState._exist | Should -Be $false
        }

        It 'Should not modify existing file when _exist is omitted' {
            Set-Content -Path $tempTestFile -Value 'test content'
            $jsonInput = @{ path = $tempTestFile } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'OpenDsc.Test/AotFile' --input $jsonInput | ConvertFrom-Json
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
            dsc resource delete -r 'OpenDsc.Test/AotFile' --input $jsonInput
            Test-Path $tempTestFile | Should -Be $false
        }

        It 'Should not error when deleting non-existent file' {
            $nonExistentPath = Join-Path $TestDrive 'nonexistent.txt'
            $jsonInput = @{ path = $nonExistentPath } | ConvertTo-Json -Compress
            { dsc resource delete -r 'OpenDsc.Test/AotFile' --input $jsonInput } | Should -Not -Throw
        }
    }

    Context 'Export Operation' {
        BeforeAll {
            $env:TEST_EXPORT_DIR = $TestDrive
            $exportTestFile1 = Join-Path $TestDrive 'test-export1.txt'
            $exportTestFile2 = Join-Path $TestDrive 'test-export2.txt'
            Set-Content -Path $exportTestFile1 -Value 'export test 1'
            Set-Content -Path $exportTestFile2 -Value 'export test 2'
            $script:exportResult = dsc resource export -r 'OpenDsc.Test/AotFile' | ConvertFrom-Json
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

    Context 'Error Handling' {
        It 'Should handle invalid JSON input gracefully' {
            dsc resource get -r 'OpenDsc.Test/AotFile' --input 'invalid json' 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'Should return error on missing required property' {
            $jsonInput = @{} | ConvertTo-Json -Compress
            dsc resource get -r 'OpenDsc.Test/AotFile' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Exit Code Handling' {
        It 'Should return exit code 2 for JsonException' {
            $jsonInput = @{ path = 'trigger-json-exception.txt' } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 2
        }

        It 'Should return exit code 1 for generic Exception' {
            $jsonInput = @{ path = 'trigger-generic-exception.txt' } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 1
        }

        It 'Should return exit code 3 for IOException' {
            $jsonInput = @{ path = 'trigger-io-exception.txt' } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 3
        }

        It 'Should return exit code 4 for DirectoryNotFoundException' {
            $jsonInput = @{ path = 'trigger-directory-not-found.txt' } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 4
        }

        It 'Should return exit code 5 for UnauthorizedAccessException' {
            $jsonInput = @{ path = 'trigger-unauthorized-access.txt' } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 5
        }

        It 'Should return exit code 0 for success' {
            $jsonInput = @{ path = $script:tempTestFile } | ConvertTo-Json -Compress
            & $script:resourceExe get --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }
}
