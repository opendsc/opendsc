if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Environment Variable Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/Environment | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/Environment'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/Environment | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }

}

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent variable' {
            $inputJson = @{
                name = 'NonExistentVariable_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Environment --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentVariable_12345_XYZ'
        }

        It 'should read properties of existing user variable' {
            # Create a test variable first
            [System.Environment]::SetEnvironmentVariable('TestVar_Get', 'TestValue', 'User')

            $inputJson = @{
                name = 'TestVar_Get'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Environment --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'TestVar_Get'
            $result.actualState.value | Should -Be 'TestValue'
            $result.actualState._exist | Should -Be $true

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_Get', $null, 'User')
        }

        It 'should read machine-scoped variable' {
            # Find an existing machine variable
            $machineVars = [System.Environment]::GetEnvironmentVariables('Machine')
            $testVar = $machineVars.Keys | Select-Object -First 1

            if ($testVar) {
                $inputJson = @{
                    name = $testVar
                    _scope = 'Machine'
                } | ConvertTo-Json -Compress

                $result = dsc resource get -r OpenDsc.Windows/Environment --input $inputJson | ConvertFrom-Json
                $result.actualState.name | Should -Be $testVar
                $result.actualState._exist | Should -Be $true
                $result.actualState._scope | Should -Be 'Machine'
            }
        }
    }

    Context 'Set Operation - User Scope' -Tag 'Set' {
        It 'should create a new user environment variable' {
            $inputJson = @{
                name = 'TestVar_Create'
                value = 'TestValue123'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify it was created
            $verifyJson = @{
                name = 'TestVar_Create'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Environment --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'TestValue123'
            $getResult.actualState._exist | Should -Be $true

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_Create', $null, 'User')
        }

        It 'should update existing user variable' {
            # Create initial variable
            [System.Environment]::SetEnvironmentVariable('TestVar_Update', 'OriginalValue', 'User')

            $inputJson = @{
                name = 'TestVar_Update'
                value = 'UpdatedValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify update
            $verifyJson = @{
                name = 'TestVar_Update'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Environment --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'UpdatedValue'

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_Update', $null, 'User')
        }

        It 'should handle variables with special characters' {
            $inputJson = @{
                name = 'TestVar_Special'
                value = 'C:\Path\With Spaces;D:\Another\Path'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify
            $verifyJson = @{
                name = 'TestVar_Special'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Environment --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.value | Should -Be 'C:\Path\With Spaces;D:\Another\Path'

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_Special', $null, 'User')
        }

        It 'should throw error when value is null' {
            $inputJson = @{
                name = 'TestVar_NullValue'
                value = $null
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete user environment variable' {
            # Create a variable to delete
            [System.Environment]::SetEnvironmentVariable('TestVar_Delete', 'ToBeDeleted', 'User')

            $inputJson = @{
                name = 'TestVar_Delete'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify deletion
            $verifyJson = @{
                name = 'TestVar_Delete'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Environment --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent variable' {
            $inputJson = @{
                name = 'NonExistentVar_ToDelete'
                _exist = $false
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.Windows/Environment --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        It 'should export all environment variables' {
            $result = dsc resource export -r OpenDsc.Windows/Environment | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            $firstVar = $result.resources[0].properties
            $firstVar.name | Should -Not -BeNullOrEmpty
            $firstVar.value | Should -Not -BeNullOrEmpty
        }

        It 'should export both user and machine variables' {
            $result = dsc resource export -r OpenDsc.Windows/Environment | ConvertFrom-Json

            $userVars = $result.resources | Where-Object { $_.properties._scope -eq $null -or $_.properties._scope -eq 'User' }
            $machineVars = $result.resources | Where-Object { $_.properties._scope -eq 'Machine' }

            $userVars.Count | Should -BeGreaterThan 0
            $machineVars.Count | Should -BeGreaterThan 0
        }

        It 'should export known system variables' {
            $result = dsc resource export -r OpenDsc.Windows/Environment | ConvertFrom-Json

            # Check for common Windows environment variables
            $pathVar = $result.resources | Where-Object { $_.properties.name -eq 'PATH' }
            $pathVar | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should validate variable name pattern (no equals sign)' {
            $invalidInput = @{
                name = 'Invalid=Name'
                value = 'SomeValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should validate variable name cannot start with equals' {
            $invalidInput = @{
                name = '=InvalidName'
                value = 'SomeValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid variable names' {
            $validInput = @{
                name = 'Valid_Variable-Name123'
                value = 'TestValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $validInput | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('Valid_Variable-Name123', $null, 'User')
        }
    }

    Context 'Scope Handling' {
        It 'should default to user scope when not specified' {
            $inputJson = @{
                name = 'TestVar_DefaultScope'
                value = 'TestValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify it's in user scope
            $userValue = [System.Environment]::GetEnvironmentVariable('TestVar_DefaultScope', 'User')
            $userValue | Should -Be 'TestValue'

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_DefaultScope', $null, 'User')
        }

        It 'should respect machine scope when specified' -Skip:(!$script:isAdmin) {
            $inputJson = @{
                name = 'TestVar_MachineScope'
                value = 'MachineValue'
                _scope = 'Machine'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Environment --input $inputJson | Out-Null

            # Verify it's in machine scope
            $machineValue = [System.Environment]::GetEnvironmentVariable('TestVar_MachineScope', 'Machine')
            $machineValue | Should -Be 'MachineValue'

            # Cleanup
            [System.Environment]::SetEnvironmentVariable('TestVar_MachineScope', $null, 'Machine')
        }
    }
}
