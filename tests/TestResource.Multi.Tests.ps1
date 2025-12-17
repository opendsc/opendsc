BeforeAll {
    $configuration = if ($env:BUILD_CONFIGURATION) { $env:BUILD_CONFIGURATION } else { 'Release' }
    $publishPath = Join-Path $PSScriptRoot "TestResource.Multi\bin\$configuration\*\*\publish" |
        Resolve-Path | Select-Object -ExpandProperty ProviderPath
    $env:DSC_RESOURCE_PATH = $publishPath
    $exeSuffix = if ($IsWindows) { '.exe' } else { '' }
    $script:resourceExe = Join-Path $publishPath "test-resource-multi$exeSuffix"
    if (-not (Test-Path $script:resourceExe)) {
        throw "Test resource executable not found at: $script:resourceExe. Run build.ps1 first."
    }
}

Describe 'Multi-Resource Manifest Tests' {
    It 'Generates multi-resource manifest with all resources' {
        $manifest = & $resourceExe manifest | ConvertFrom-Json
        $manifest.resources | Should -Not -BeNullOrEmpty
        $manifest.resources.Count | Should -Be 3

        $types = $manifest.resources.type
        $types | Should -Contain 'TestResource.Multi/File'
        $types | Should -Contain 'TestResource.Multi/User'
        $types | Should -Contain 'TestResource.Multi/Service'
    }

    It 'Each resource has correct version from attribute' {
        $manifest = & $resourceExe manifest | ConvertFrom-Json
        foreach ($resource in $manifest.resources) {
            $resource.version | Should -Be '1.0.0'
        }
    }

    It 'Multi-resource manifest includes --resource in args' {
        $manifest = & $resourceExe manifest | ConvertFrom-Json
        $fileResource = $manifest.resources | Where-Object { $_.type -eq 'TestResource.Multi/File' }
        $fileResource.get.args | Should -Contain '--resource'
        $fileResource.get.args | Should -Contain 'TestResource.Multi/File'
        $fileResource.set.args | Should -Contain '--resource'
        $fileResource.test.args | Should -Contain '--resource'
        $fileResource.delete.args | Should -Contain '--resource'
    }

    It 'Generates single resource manifest with --resource parameter' {
        $manifest = & $resourceExe manifest --resource 'TestResource.Multi/File' | ConvertFrom-Json
        $manifest.type | Should -Be 'TestResource.Multi/File'
        $manifest.version | Should -Be '1.0.0'
        $manifest.description | Should -Be 'Manages file content'
        $manifest.get | Should -Not -BeNullOrEmpty
        $manifest.get.args | Should -Contain '--resource'
        $manifest.get.args | Should -Contain 'TestResource.Multi/File'
    }

    It 'Single resource manifest includes tags' {
        $manifest = & $resourceExe manifest --resource 'TestResource.Multi/File' | ConvertFrom-Json
        $manifest.tags | Should -Contain 'file'
        $manifest.tags | Should -Contain 'content'
    }

    It 'Saves multi-resource manifest with --save' {
        $manifestPath = Join-Path $publishPath 'test-resource-multi.dsc.manifests.json'
        if (Test-Path $manifestPath) { Remove-Item $manifestPath }

        & $resourceExe manifest --save 2>&1 | Out-Null
        Test-Path $manifestPath | Should -Be $true

        $manifest = Get-Content $manifestPath | ConvertFrom-Json
        $manifest.resources.Count | Should -Be 3
    }

    It 'Saves single resource manifest with --save and --resource' {
        $manifestPath = Join-Path $publishPath 'testresource.multi.file.dsc.resource.json'
        if (Test-Path $manifestPath) { Remove-Item $manifestPath }

        & $resourceExe manifest --resource 'TestResource.Multi/File' --save 2>&1 | Out-Null
        Test-Path $manifestPath | Should -Be $true

        $manifest = Get-Content $manifestPath | ConvertFrom-Json
        $manifest.type | Should -Be 'TestResource.Multi/File'
    }
}

Describe 'Multi-Resource Operation Tests' {
    BeforeAll {
        $script:testFile = Join-Path $TestDrive 'test-file.txt'
    }

    Context 'File Resource Operations' {
        It 'Executes get with --resource parameter' {
            $inputJson = @{ path = $testFile } | ConvertTo-Json -Compress
            $result = & $resourceExe get --resource 'TestResource.Multi/File' --input $inputJson | ConvertFrom-Json
            $result.path | Should -Be $testFile
            $result._exist | Should -Be $false
        }

        It 'Executes set with --resource parameter' {
            $inputJson = @{ path = $testFile; content = 'test content'; _exist = $true } | ConvertTo-Json -Compress
            $result = & $resourceExe set --resource 'TestResource.Multi/File' --input $inputJson | ConvertFrom-Json
            $result._exist | Should -BeNullOrEmpty
            Test-Path $testFile | Should -Be $true
            (Get-Content $testFile -Raw).Trim() | Should -Be 'test content'
        }

        It 'Executes test with --resource parameter' {
            $inputJson = @{ path = $testFile; content = 'test content'; _exist = $true } | ConvertTo-Json -Compress
            $result = & $resourceExe test --resource 'TestResource.Multi/File' --input $inputJson | ConvertFrom-Json
            $result._exist | Should -BeNullOrEmpty
        }

        It 'Executes delete with --resource parameter' {
            $inputJson = @{ path = $testFile } | ConvertTo-Json -Compress
            & $resourceExe delete --resource 'TestResource.Multi/File' --input $inputJson
            Test-Path $testFile | Should -Be $false
        }
    }

    Context 'User Resource Operations' {
        It 'Executes get for user with --resource parameter' {
            $inputJson = @{ name = 'TestUser' } | ConvertTo-Json -Compress
            $result = & $resourceExe get --resource 'TestResource.Multi/User' --input $inputJson | ConvertFrom-Json
            $result.name | Should -Be 'TestUser'
            $result._exist | Should -BeNullOrEmpty
        }

        It 'Executes set for user with --resource parameter' {
            $inputJson = @{ name = 'TestUser'; fullName = 'Test User'; _exist = $true } | ConvertTo-Json -Compress
            $result = & $resourceExe set --resource 'TestResource.Multi/User' --input $inputJson | ConvertFrom-Json
            $result._exist | Should -BeNullOrEmpty
            $result.fullName | Should -Be 'Test User'
        }

        It 'Executes test for user with --resource parameter' {
            $inputJson = @{ name = 'TestUser'; fullName = 'Test User'; _exist = $true } | ConvertTo-Json -Compress
            $result = & $resourceExe test --resource 'TestResource.Multi/User' --input $inputJson | ConvertFrom-Json
            $result._exist | Should -BeNullOrEmpty
        }
    }

    Context 'Service Resource Operations' {
        It 'Executes get for service with --resource parameter' {
            $inputJson = @{ name = 'TestService1' } | ConvertTo-Json -Compress
            $result = & $resourceExe get --resource 'TestResource.Multi/Service' --input $inputJson | ConvertFrom-Json
            $result.name | Should -Be 'TestService1'
            $result._exist | Should -BeNullOrEmpty
            $result.state | Should -Be 'running'
        }

        It 'Executes test for service with --resource parameter' {
            $inputJson = @{ name = 'TestService1'; state = 'running'; _exist = $true } | ConvertTo-Json -Compress
            $result = & $resourceExe test --resource 'TestResource.Multi/Service' --input $inputJson | ConvertFrom-Json
            $result._exist | Should -BeNullOrEmpty
            $result.state | Should -Be 'running'
        }
    }
}

Describe 'Multi-Resource Error Handling' {
    It 'Fails when --resource is missing for multi-resource exe' {
        $inputJson = @{ path = 'test.txt' } | ConvertTo-Json -Compress
        $output = & $resourceExe get --input $inputJson 2>&1
        $LASTEXITCODE | Should -Not -Be 0
        ($output -join ' ') | Should -Match 'resource.*required'
    }

    It 'Lists available resources on error' {
        $inputJson = @{ path = 'test.txt' } | ConvertTo-Json -Compress
        $output = & $resourceExe get --input $inputJson 2>&1
        ($output -join ' ') | Should -Match 'resource.*required'
    }

    It 'Fails when invalid resource type is specified' {
        $inputJson = @{ path = 'test.txt' } | ConvertTo-Json -Compress
        $output = & $resourceExe get --resource 'Invalid/Resource' --input $inputJson 2>&1
        $LASTEXITCODE | Should -Not -Be 0
        $output | Should -Match 'not found'
    }
}

Describe 'Multi-Resource Schema Tests' {
    It 'Executes schema with --resource parameter' {
        $schema = & $resourceExe schema --resource 'TestResource.Multi/File' | ConvertFrom-Json
        $schema.type | Should -Be 'object'
        $schema.properties | Should -Not -BeNullOrEmpty
    }

    It 'Fails when --resource is missing for schema command' {
        $output = & $resourceExe schema 2>&1
        $LASTEXITCODE | Should -Not -Be 0
        ($output -join ' ') | Should -Match 'resource.*required'
    }
}

Describe 'Multi-Resource DSC CLI Discovery' {
    BeforeAll {
        Get-ChildItem $env:DSC_RESOURCE_PATH -Filter "*.dsc.*.json" | Remove-Item -Force -ErrorAction SilentlyContinue
        & $resourceExe manifest --save 2>&1 | Out-Null
    }

    It 'Should find all three resources with dsc resource list' {
        $resources = dsc resource list 'TestResource.Multi/*' | ConvertFrom-Json
        $resources.Count | Should -Be 3

        $types = $resources.type
        $types | Should -Contain 'TestResource.Multi/File'
        $types | Should -Contain 'TestResource.Multi/User'
        $types | Should -Contain 'TestResource.Multi/Service'
    }

    It 'File resource should have correct description' {
        $resources = dsc resource list 'TestResource.Multi/File' | ConvertFrom-Json
        $resources.Count | Should -Be 1
        $resources[0].description | Should -Be 'Manages file content'
    }

    It 'User resource should have correct description' {
        $resources = dsc resource list 'TestResource.Multi/User' | ConvertFrom-Json
        $resources.Count | Should -Be 1
        $resources[0].description | Should -Be 'Manages user accounts'
    }

    It 'Service resource should have correct description' {
        $resources = dsc resource list 'TestResource.Multi/Service' | ConvertFrom-Json
        $resources.Count | Should -Be 1
        $resources[0].description | Should -Be 'Manages service state'
    }
}

Describe 'Multi-Resource DSC CLI Schema' {
    It 'File resource should export valid schema' {
        $schema = dsc resource schema -r 'TestResource.Multi/File' | ConvertFrom-Json
        $schema.type | Should -Be 'object'
        $schema.properties | Should -Not -BeNullOrEmpty
        $schema.properties.path | Should -Not -BeNullOrEmpty
        $schema.properties.content | Should -Not -BeNullOrEmpty
    }

    It 'User resource should export valid schema' {
        $schema = dsc resource schema -r 'TestResource.Multi/User' | ConvertFrom-Json
        $schema.type | Should -Be 'object'
        $schema.properties | Should -Not -BeNullOrEmpty
        $schema.properties.name | Should -Not -BeNullOrEmpty
        $schema.properties.fullName | Should -Not -BeNullOrEmpty
    }

    It 'Service resource should export valid schema' {
        $schema = dsc resource schema -r 'TestResource.Multi/Service' | ConvertFrom-Json
        $schema.type | Should -Be 'object'
        $schema.properties | Should -Not -BeNullOrEmpty
        $schema.properties.name | Should -Not -BeNullOrEmpty
        $schema.properties.state | Should -Not -BeNullOrEmpty
    }
}

Describe 'Multi-Resource DSC CLI Operations' {
    BeforeAll {
        $script:testFile = Join-Path $TestDrive 'dsc-test-file.txt'
    }

    Context 'File Resource Operations' {
        It 'Executes get with dsc resource get' {
            $inputJson = @{ path = $testFile } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'TestResource.Multi/File' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState.path | Should -Be $testFile
            $result.actualState._exist | Should -Be $false
        }

        It 'Executes set with dsc resource set' {
            $inputJson = @{ path = $testFile; content = 'dsc test content'; _exist = $true } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'TestResource.Multi/File' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
            Test-Path $testFile | Should -Be $true
            (Get-Content $testFile -Raw).Trim() | Should -Be 'dsc test content'
        }

        It 'Executes test with dsc resource test' {
            $inputJson = @{ path = $testFile; content = 'dsc test content'; _exist = $true } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'TestResource.Multi/File' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'Executes delete with dsc resource delete' {
            $inputJson = @{ path = $testFile } | ConvertTo-Json -Compress
            dsc resource delete -r 'TestResource.Multi/File' --input $inputJson
            Test-Path $testFile | Should -Be $false
        }
    }

    Context 'User Resource Operations' {
        It 'Executes get for user with dsc resource get' {
            $inputJson = @{ name = 'TestUser' } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'TestResource.Multi/User' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState.name | Should -Be 'TestUser'
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'Executes set for user with dsc resource set' {
            $inputJson = @{ name = 'TestUser'; fullName = 'Test User'; _exist = $true } | ConvertTo-Json -Compress
            $result = dsc resource set -r 'TestResource.Multi/User' --input $inputJson --output-format json | ConvertFrom-Json
            $result.afterState.fullName | Should -Be 'Test User'
        }

        It 'Executes test for user with dsc resource test' {
            $inputJson = @{ name = 'TestUser'; fullName = 'Test User'; _exist = $true } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'TestResource.Multi/User' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }
    }

    Context 'Service Resource Operations' {
        It 'Executes get for service with dsc resource get' {
            $inputJson = @{ name = 'TestService1' } | ConvertTo-Json -Compress
            $result = dsc resource get -r 'TestResource.Multi/Service' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState.name | Should -Be 'TestService1'
            $result.actualState._exist | Should -BeNullOrEmpty
            $result.actualState.state | Should -Be 'running'
        }

        It 'Executes test for service with dsc resource test' {
            $inputJson = @{ name = 'TestService1'; state = 'Running'; _exist = $true } | ConvertTo-Json -Compress
            $result = dsc resource test -r 'TestResource.Multi/Service' --input $inputJson --output-format json | ConvertFrom-Json
            $result.actualState._exist | Should -BeNullOrEmpty
        }
    }

    Context 'Exit Code Handling' {
        It 'Should return exit code 2 for generic Exception in File resource' {
            $jsonInput = @{ path = 'trigger-generic-exception.txt' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/File' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 2
        }

        It 'Should return exit code 4 for IOException in File resource' {
            $jsonInput = @{ path = 'trigger-io-exception.txt' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/File' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 4
        }

        It 'Should return exit code 5 for DirectoryNotFoundException in File resource' {
            $jsonInput = @{ path = 'trigger-directory-not-found.txt' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/File' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 5
        }

        It 'Should return exit code 6 for UnauthorizedAccessException in File resource' {
            $jsonInput = @{ path = 'trigger-unauthorized-access.txt' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/File' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 6
        }

        It 'Should return exit code 0 for successful File resource operation' {
            $jsonInput = @{ path = $testFile } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/File' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 0
        }

        It 'Should return proper exit code for User resource' {
            $jsonInput = @{ name = 'TestUser' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/User' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 0
        }

        It 'Should return proper exit code for Service resource' {
            $jsonInput = @{ name = 'TestService1' } | ConvertTo-Json -Compress
            & $resourceExe get --resource 'TestResource.Multi/Service' --input $jsonInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }
}
