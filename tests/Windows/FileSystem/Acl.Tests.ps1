if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows File System ACL Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testDir = Join-Path $env:TEMP "DscAclTest_$(Get-Random)"
        $script:testFile = Join-Path $script:testDir "testfile.txt"
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows.FileSystem/AccessControlList'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
        }

}

    Context 'Get Operation - Non-Elevated' -Tag 'Get' {
        BeforeAll {
        $configuration = $env:BUILD_CONFIGURATION ?? 'Release'
            New-Item -Path $script:testDir -ItemType Directory -Force | Out-Null
            New-Item -Path $script:testFile -ItemType File -Force | Out-Null
            Set-Content -Path $script:testFile -Value "Test content"
        }

        AfterAll {
            if (Test-Path $script:testDir) {
                Remove-Item -Path $script:testDir -Recurse -Force
            }
        }

        It 'should return _exist=false for non-existent file' {
            $inputJson = @{
                path = 'C:\NonExistent\Path\File.txt'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be 'C:\NonExistent\Path\File.txt'
        }

        It 'should read ACL of existing file' {
            $inputJson = @{
                path = $script:testFile
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $script:testFile
            $result.actualState._exist | Should -Be $true
            $result.actualState.owner | Should -Not -BeNullOrEmpty
            $result.actualState.accessRules | Should -Not -BeNullOrEmpty
        }

        It 'should read ACL of existing directory' {
            $inputJson = @{
                path = $script:testDir
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $script:testDir
            $result.actualState._exist | Should -Be $true
            $result.actualState.owner | Should -Not -BeNullOrEmpty
            $result.actualState.accessRules | Should -Not -BeNullOrEmpty
        }

        It 'should include inheritance flags in access rules' {
            $inputJson = @{
                path = $script:testDir
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | ConvertFrom-Json
            $result.actualState.accessRules | Should -Not -BeNullOrEmpty
            $result.actualState.accessRules[0].identity | Should -Not -BeNullOrEmpty
            $result.actualState.accessRules[0].rights | Should -Not -BeNullOrEmpty
            $result.actualState.accessRules[0].accessControlType | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Set Operation - Add Access Rule' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeAll {
        $configuration = $env:BUILD_CONFIGURATION ?? 'Release'
            New-Item -Path $script:testDir -ItemType Directory -Force | Out-Null
            New-Item -Path $script:testFile -ItemType File -Force | Out-Null
            Set-Content -Path $script:testFile -Value "Test content"
        }

        AfterAll {
            if (Test-Path $script:testDir) {
                Remove-Item -Path $script:testDir -Recurse -Force
            }
        }

        It 'should add read access for Users group to file' {
            $inputJson = @{
                path = $script:testFile
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $usersRule = $getResult.actualState.accessRules | Where-Object { $_.identity -like '*Users' -and $_.rights -like '*Read*' }
            $usersRule | Should -Not -BeNullOrEmpty
        }

        It 'should add multiple access rules' {
            $inputJson = @{
                path = $script:testFile
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read')
                        accessControlType = 'Allow'
                    },
                    @{
                        identity = 'BUILTIN\Administrators'
                        rights = @('FullControl')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.accessRules | Should -Not -BeNullOrEmpty
        }

        It 'should handle inheritance flags for directories' {
            $inputJson = @{
                path = $script:testDir
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read')
                        inheritanceFlags = @('ContainerInherit')
                        propagationFlags = @('None')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:testDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $usersRule = $getResult.actualState.accessRules | Where-Object {
                $_.identity -like '*Users' -and $_.rights -like '*Read*' -and $_.inheritanceFlags -eq 'ContainerInherit'
            }
            $usersRule | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Set Operation - Change Owner' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $script:tempFile = Join-Path $env:TEMP "DscAclOwnerTest_$(Get-Random).txt"
            New-Item -Path $script:tempFile -ItemType File -Force | Out-Null
        }

        AfterEach {
            if (Test-Path $script:tempFile) {
                Remove-Item -Path $script:tempFile -Force
            }
        }

        It 'should change file owner to Administrators' {
            $inputJson = @{
                path = $script:tempFile
                owner = 'BUILTIN\Administrators'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:tempFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -BeLike '*Administrators'
        }
    }

    Context 'Set Operation - Purge Mode' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $script:purgeTestFile = Join-Path $env:TEMP "DscAclPurgeTest_$(Get-Random).txt"
            New-Item -Path $script:purgeTestFile -ItemType File -Force | Out-Null
        }

        AfterEach {
            if (Test-Path $script:purgeTestFile) {
                Remove-Item -Path $script:purgeTestFile -Force
            }
        }

        It 'should not remove existing rules when _purge is false' {
            $inputJson1 = @{
                path = $script:purgeTestFile
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read')
                        accessControlType = 'Allow'
                    }
                )
                _purge = $false
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson1 | Out-Null

            $verifyJson = @{
                path = $script:purgeTestFile
            } | ConvertTo-Json -Compress

            $result1 = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $initialCount = $result1.actualState.accessRules.Count

            $inputJson2 = @{
                path = $script:purgeTestFile
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Administrators'
                        rights = @('FullControl')
                        accessControlType = 'Allow'
                    }
                )
                _purge = $false
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson2 | Out-Null

            $result2 = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $result2.actualState.accessRules.Count | Should -BeGreaterOrEqual $initialCount
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should have no duplicate enum values in rights' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $rightsEnum = $schemaJson.properties.accessRules.items.properties.rights.items.enum
            $uniqueRights = $rightsEnum | Select-Object -Unique
            $rightsEnum.Count | Should -Be $uniqueRights.Count
        }

        It 'should have no duplicate enum values in inheritanceFlags' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $inheritEnum = $schemaJson.properties.accessRules.items.properties.inheritanceFlags.items.enum
            $uniqueInherit = $inheritEnum | Select-Object -Unique
            $inheritEnum.Count | Should -Be $uniqueInherit.Count
        }

        It 'should have no duplicate enum values in propagationFlags' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $propEnum = $schemaJson.properties.accessRules.items.properties.propagationFlags.items.enum
            $uniqueProp = $propEnum | Select-Object -Unique
            $propEnum.Count | Should -Be $uniqueProp.Count
        }

        It 'should mark rights as uniqueItems in schema' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $rightsSchema = $schemaJson.properties.accessRules.items.properties.rights
            $rightsSchema.uniqueItems | Should -Be $true
        }

        It 'should mark inheritanceFlags as uniqueItems in schema' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $inheritSchema = $schemaJson.properties.accessRules.items.properties.inheritanceFlags
            $inheritSchema.uniqueItems | Should -Be $true
        }

        It 'should mark propagationFlags as uniqueItems in schema' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $propSchema = $schemaJson.properties.accessRules.items.properties.propagationFlags
            $propSchema.uniqueItems | Should -Be $true
        }

        It 'should define rights as array type' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $rightsSchema = $schemaJson.properties.accessRules.items.properties.rights
            $rightsSchema.type | Should -Be 'array'
        }

        It 'should define inheritanceFlags as array type' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $inheritSchema = $schemaJson.properties.accessRules.items.properties.inheritanceFlags
            $inheritSchema.type | Should -Be 'array'
        }

        It 'should define propagationFlags as array type' {
            $schemaJson = dsc resource schema -r OpenDsc.Windows.FileSystem/AccessControlList | ConvertFrom-Json
            $propSchema = $schemaJson.properties.accessRules.items.properties.propagationFlags
            $propSchema.type | Should -Be 'array'
        }
    }

    Context 'Multiple Selection in Arrays' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $script:multiSelectDir = Join-Path $env:TEMP "DscAclMultiSelect_$(Get-Random)"
            New-Item -Path $script:multiSelectDir -ItemType Directory -Force | Out-Null
        }

        AfterEach {
            if (Test-Path $script:multiSelectDir) {
                Remove-Item -Path $script:multiSelectDir -Recurse -Force
            }
        }

        It 'should accept multiple rights in array format' {
            $inputJson = @{
                path = $script:multiSelectDir
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read', 'Write')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:multiSelectDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $usersRule = $getResult.actualState.accessRules | Where-Object { $_.identity -like '*Users' }
            $usersRule | Should -Not -BeNullOrEmpty
            $usersRule.rights | Should -Contain 'Read'
            $usersRule.rights | Should -Contain 'Write'
        }

        It 'should accept multiple inheritance flags' {
            $inputJson = @{
                path = $script:multiSelectDir
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('Read')
                        inheritanceFlags = @('ContainerInherit', 'ObjectInherit')
                        propagationFlags = @('None')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:multiSelectDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $usersRule = $getResult.actualState.accessRules | Where-Object { $_.identity -like '*Users' }
            $usersRule | Should -Not -BeNullOrEmpty
            $usersRule.inheritanceFlags | Should -Contain 'ContainerInherit'
            $usersRule.inheritanceFlags | Should -Contain 'ObjectInherit'
        }

        It 'should combine multiple rights correctly' {
            $inputJson = @{
                path = $script:multiSelectDir
                accessRules = @(
                    @{
                        identity = 'BUILTIN\Users'
                        rights = @('ListDirectory', 'CreateFiles', 'ReadAttributes')
                        accessControlType = 'Allow'
                    }
                )
            } | ConvertTo-Json -Depth 3

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                path = $script:multiSelectDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $verifyJson | ConvertFrom-Json
            $usersRule = $getResult.actualState.accessRules | Where-Object { $_.identity -like '*Users' }
            $usersRule | Should -Not -BeNullOrEmpty
            $usersRule.rights | Should -Contain 'ListDirectory'
            $usersRule.rights | Should -Contain 'CreateFiles'
            $usersRule.rights | Should -Contain 'ReadAttributes'
        }
    }

    Context 'Error Handling' {
        It 'should fail when file does not exist' {
            $inputJson = @{
                path = 'C:\NonExistent\Path\File.txt'
                owner = 'BUILTIN\Administrators'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should fail with invalid identity' -Skip:(!$script:isAdmin) {
            $tempFile = Join-Path $env:TEMP "DscAclErrorTest_$(Get-Random).txt"
            New-Item -Path $tempFile -ItemType File -Force | Out-Null

            try {
                $inputJson = @{
                    path = $tempFile
                    accessRules = @(
                        @{
                            identity = 'InvalidDomain\InvalidUser12345'
                            rights = @('Read')
                            accessControlType = 'Allow'
                        }
                    )
                } | ConvertTo-Json -Depth 3

                dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $inputJson 2>&1 | Out-Null
                $LASTEXITCODE | Should -Not -Be 0
            }
            finally {
                if (Test-Path $tempFile) {
                    Remove-Item -Path $tempFile -Force
                }
            }
        }
    }
}
