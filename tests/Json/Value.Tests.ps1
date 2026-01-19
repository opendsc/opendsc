Describe 'JSON Value Resource' -Tag 'Json' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testDir = Join-Path $TestDrive "json-tests"
        New-Item -ItemType Directory -Path $script:testDir -Force | Out-Null
    }

    AfterEach {
        if (Test-Path $script:testDir) {
            Get-ChildItem -Path $script:testDir -File | Remove-Item -Force
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Json/Value | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Json/Value'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Json/Value | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent file' {
            $inputJson = @{
                path = 'C:\NonExistent\file.json'
                jsonPath = '$.config.timeout'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be 'C:\NonExistent\file.json'
        }

        It 'should return _exist=false for non-existent value' {
            $jsonPath = Join-Path $script:testDir 'test-get-1.json'
            @'
{
  "config": {
    "name": "MyApp"
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.timeout'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should read primitive string value' {
            $jsonPath = Join-Path $script:testDir 'test-get-2.json'
            @'
{
  "config": {
    "name": "MyApp"
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'MyApp'
        }

        It 'should read primitive number value' {
            $jsonPath = Join-Path $script:testDir 'test-get-3.json'
            @'
{
  "config": {
    "timeout": 30
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.timeout'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 30
        }

        It 'should read object value' {
            $jsonPath = Join-Path $script:testDir 'test-get-4.json'
            @'
{
  "server": {
    "host": "localhost",
    "port": 8080
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.server'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState.value | Should -Not -BeNullOrEmpty
            $result.actualState.value.host | Should -Be 'localhost'
            $result.actualState.value.port | Should -Be 8080
        }

        It 'should read array value' {
            $jsonPath = Join-Path $script:testDir 'test-get-5.json'
            @'
{
  "items": [1, 2, 3]
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.items'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState.value.Count | Should -Be 3
            $result.actualState.value[0] | Should -Be 1
            $result.actualState.value[1] | Should -Be 2
            $result.actualState.value[2] | Should -Be 3
        }

        It 'should read array element' {
            $jsonPath = Join-Path $script:testDir 'test-get-6.json'
            @'
{
  "items": ["first", "second", "third"]
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.items[1]'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Json/Value --input $inputJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'second'
        }
    }

    Context 'Set Operation - File Not Exist' -Tag 'Set' {
        It 'should fail when file does not exist' {
            $inputJson = @{
                path = 'C:\NonExistent\file.json'
                jsonPath = '$.config.name'
                value = 'TestApp'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Set Operation - Primitive Values' -Tag 'Set' {
        It 'should create string value' {
            $jsonPath = Join-Path $script:testDir 'test-set-1.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
                value = 'MyApp'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'MyApp'
        }

        It 'should create number value' {
            $jsonPath = Join-Path $script:testDir 'test-set-2.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.timeout'
                value = '30'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.config.timeout | Should -Be 30
        }

        It 'should create boolean value' {
            $jsonPath = Join-Path $script:testDir 'test-set-3.json'
            @'
{
  "features": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.features.enabled'
                value = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.features.enabled | Should -Be $true
        }

        It 'should update existing value' {
            $jsonPath = Join-Path $script:testDir 'test-set-4.json'
            @'
{
  "config": {
    "name": "OldName"
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
                value = 'NewName'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'NewName'
        }
    }

    Context 'Set Operation - Complex Values' -Tag 'Set' {
        It 'should create object value' {
            $jsonPath = Join-Path $script:testDir 'test-set-5.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.server'
                value = @{host='localhost'; port=8080}
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.config.server.host | Should -Be 'localhost'
            $content.config.server.port | Should -Be 8080
        }

        It 'should create array value' {
            $jsonPath = Join-Path $script:testDir 'test-set-6.json'
            @'
{
  "data": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.data.items'
                value = @(1,2,3,4,5)
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.data.items.Count | Should -Be 5
            $content.data.items[2] | Should -Be 3
        }
    }

    Context 'Set Operation - Recursive Parent Creation' -Tag 'Set' {
        It 'should create nested object path recursively' {
            $jsonPath = Join-Path $script:testDir 'test-set-7.json'
            @'
{
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.server.database.connectionString'
                value = 'Server=localhost;Database=MyDb'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.server.database.connectionString'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'Server=localhost;Database=MyDb'
        }

        It 'should create array element with parent path' {
            $jsonPath = Join-Path $script:testDir 'test-set-8.json'
            @'
{
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.items[0]'
                value = 'first'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.items.Count | Should -Be 1
            $content.items[0] | Should -Be 'first'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete value from object' {
            $jsonPath = Join-Path $script:testDir 'test-delete-1.json'
            @'
{
  "config": {
    "name": "MyApp",
    "timeout": 30
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.timeout'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.config.PSObject.Properties.Name | Should -Not -Contain 'timeout'
            $content.config.name | Should -Be 'MyApp'
        }

        It 'should handle deleting non-existent value' {
            $jsonPath = Join-Path $script:testDir 'test-delete-2.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.nonexistent'
                _exist = $false
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Json/Value --input $inputJson } | Should -Not -Throw
        }

        It 'should handle deleting from non-existent file' {
            $inputJson = @{
                path = 'C:\NonExistent\file.json'
                jsonPath = '$.config.name'
                _exist = $false
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Json/Value --input $inputJson } | Should -Not -Throw
        }

        It 'should delete array element' {
            $jsonPath = Join-Path $script:testDir 'test-delete-3.json'
            @'
{
  "items": ["first", "second", "third"]
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.items[1]'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.items.Count | Should -Be 2
            $content.items[0] | Should -Be 'first'
            $content.items[1] | Should -Be 'third'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should validate JSONPath must start with $' {
            $jsonPath = Join-Path $script:testDir 'test-schema-1.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $invalidInput = @{
                path = $jsonPath
                jsonPath = 'config.name'
                value = 'Test'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid JSONPath syntax' {
            $jsonPath = Join-Path $script:testDir 'test-schema-2.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $validInput = @{
                path = $jsonPath
                jsonPath = '$.config.name'
                value = 'ValidApp'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $validInput | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }

    Context 'Formatting Preservation' -Tag 'Formatting' {
        It 'should maintain indented JSON format' {
            $jsonPath = Join-Path $script:testDir 'test-format-1.json'
            @'
{
  "config": {
    "name": "MyApp"
  }
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.timeout'
                value = 30
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw
            $content | Should -Match '\n  "config"'
            $content | Should -Match '\n    "name"'
        }
    }

    Context 'All Valid JSON Types' -Tag 'Types' {
        It 'should handle JSON null value' {
            $jsonPath = Join-Path $script:testDir 'test-null.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.value'
                value = $null
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $content = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            $content.config.PSObject.Properties.Name | Should -Contain 'value'
            $content.config.value | Should -Be $null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.value'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be $null
        }

        It 'should handle boolean true' {
            $jsonPath = Join-Path $script:testDir 'test-bool-true.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.enabled'
                value = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.enabled'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be $true
        }

        It 'should handle boolean false' {
            $jsonPath = Join-Path $script:testDir 'test-bool-false.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.disabled'
                value = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.disabled'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be $false
        }

        It 'should handle negative numbers' {
            $jsonPath = Join-Path $script:testDir 'test-negative.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.temperature'
                value = -15
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.temperature'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be -15
        }

        It 'should handle decimal numbers' {
            $jsonPath = Join-Path $script:testDir 'test-decimal.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.price'
                value = 19.99
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.price'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 19.99
        }

        It 'should handle zero' {
            $jsonPath = Join-Path $script:testDir 'test-zero.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.count'
                value = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.count'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 0
        }

        It 'should handle empty string' {
            $jsonPath = Join-Path $script:testDir 'test-empty-string.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
                value = ''
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.name'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be ''
        }

        It 'should handle empty object' {
            $jsonPath = Join-Path $script:testDir 'test-empty-object.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.metadata'
                value = @{}
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.metadata'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            # Verify empty object was created in the file (PowerShell ConvertFrom-Json may treat {} as null)
            $rawContent = Get-Content -Path $jsonPath -Raw
            $rawContent | Should -Match '"metadata"\s*:\s*\{'
            $rawContent | Should -Match '"metadata"\s*:\s*\{\s*\}'
        }

        It 'should handle empty array' {
            $jsonPath = Join-Path $script:testDir 'test-empty-array.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.items'
                value = @()
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.items'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value.Count | Should -Be 0
        }

        It 'should handle nested object with mixed types' {
            $jsonPath = Join-Path $script:testDir 'test-nested-mixed.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.server'
                value = @{
                    host = 'localhost'
                    port = 8080
                    ssl = $true
                    timeout = $null
                    retries = 3
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.server'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value.host | Should -Be 'localhost'
            $getResult.actualState.value.port | Should -Be 8080
            $getResult.actualState.value.ssl | Should -Be $true
            $getResult.actualState.value.timeout | Should -Be $null
            $getResult.actualState.value.retries | Should -Be 3
        }

        It 'should handle array with mixed types' {
            $jsonPath = Join-Path $script:testDir 'test-array-mixed.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.values'
                value = @('string', 42, $true, $null)
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.values'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value.Count | Should -Be 4
            $getResult.actualState.value[0] | Should -Be 'string'
            $getResult.actualState.value[1] | Should -Be 42
            $getResult.actualState.value[2] | Should -Be $true
            $getResult.actualState.value[3] | Should -Be $null
        }

        It 'should handle deeply nested structures' {
            $jsonPath = Join-Path $script:testDir 'test-deep-nested.json'
            @'
{
  "config": {}
}
'@ | Set-Content -Path $jsonPath

            $inputJson = @{
                path = $jsonPath
                jsonPath = '$.config.data'
                value = @{
                    level1 = @{
                        level2 = @{
                            level3 = @{
                                value = 'deep'
                                items = @(1, 2, 3)
                            }
                        }
                    }
                }
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Json/Value --input $inputJson | Out-Null

            $verifyJson = @{
                path = $jsonPath
                jsonPath = '$.config.data.level1.level2.level3.value'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Json/Value --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'deep'
        }
    }
}
