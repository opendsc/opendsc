# Skip SQL Server tests if no SQL Server instance is available
BeforeAll {
    # Load shared SQL Server installation script
    . $PSScriptRoot/Install-SqlServer.ps1

    # Initialize SQL Server (installs if in GitHub Actions)
    Initialize-SqlServerForTests

    # Test SQL Server connectivity
    try
    {
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = "Server=$script:sqlServerInstance;Integrated Security=True"
        $conn.Open()
        $conn.Close()

        $script:sqlServerAvailable = $true
    }
    catch
    {
        Write-Warning "SQL Server not available at '$script:sqlServerInstance'. Skipping SQL Server tests. Error: $_"
    }
}

Describe 'SQL Server Login Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testLoginPrefix = 'OpenDscTestLogin_'
    }

    AfterAll {
        # Cleanup any test logins that may have been left behind
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "Server=$script:sqlServerInstance;Integrated Security=True"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "SELECT name FROM sys.server_principals WHERE name LIKE '$($script:testLoginPrefix)%'"
                $reader = $cmd.ExecuteReader()

                $loginsToDelete = @()
                while ($reader.Read())
                {
                    $loginsToDelete += $reader.GetString(0)
                }
                $reader.Close()

                foreach ($login in $loginsToDelete)
                {
                    $dropCmd = $conn.CreateCommand()
                    $dropCmd.CommandText = "DROP LOGIN [$login]"
                    try { $dropCmd.ExecuteNonQuery() | Out-Null } catch { }
                }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test logins: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/Login | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/Login'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/Login | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'test'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Login | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.loginType | Should -Not -BeNullOrEmpty
            $result.properties.password | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent login' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentLogin_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Login --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentLogin_12345_XYZ'
        }

        It 'should return properties of existing sa login' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'sa'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Login --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'sa'
            $result.actualState.loginType | Should -Be 'SqlLogin'
            $result.actualState._exist | Should -Not -Be $false
        }
    }

    Context 'Set Operation - SQL Login' -Tag 'Set' {
        It 'should create a new SQL login' {
            $loginName = "$($script:testLoginPrefix)Create1"
            $inputJson = @{
                serverInstance            = $script:sqlServerInstance
                name                      = $loginName
                loginType                 = 'SqlLogin'
                password                  = 'T3stP@ssw0rd!Secure123'
                passwordPolicyEnforced    = $true
                passwordExpirationEnabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $loginName
            $getResult.actualState.loginType | Should -Be 'SqlLogin'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            $deleteJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
        }

        It 'should update existing SQL login properties' {
            $loginName = "$($script:testLoginPrefix)Update1"

            # Create initial login
            $createJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                loginType       = 'SqlLogin'
                password        = 'T3stP@ssw0rd!Initial123'
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Update the login
            $updateJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                defaultDatabase = 'tempdb'
                disabled        = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.defaultDatabase | Should -Be 'tempdb'
            $getResult.actualState.disabled | Should -Be $true

            # Cleanup
            $deleteJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
        }
    }

    Context 'Test Operation' -Tag 'Test' {
        It 'should return inDesiredState=false for non-existent login when _exist=true' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentLogin_Test123'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Login --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return inDesiredState=true for existing login matching desired state' {
            $loginName = "$($script:testLoginPrefix)TestMatch1"

            # Create login
            $createJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                loginType       = 'SqlLogin'
                password        = 'T3stP@ssw0rd!Test123'
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Test with matching desired state
            $testJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Login --input $testJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input (@{
                    serverInstance = $script:sqlServerInstance
                    name           = $loginName
                } | ConvertTo-Json -Compress) | Out-Null
        }

        It 'should return inDesiredState=false for login with different properties' {
            $loginName = "$($script:testLoginPrefix)TestDiff1"

            # Create login with master as default database
            $createJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                loginType       = 'SqlLogin'
                password        = 'T3stP@ssw0rd!Diff123'
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Test expecting tempdb as default database
            $testJson = @{
                serverInstance  = $script:sqlServerInstance
                name            = $loginName
                defaultDatabase = 'tempdb'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Login --input $testJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input (@{
                    serverInstance = $script:sqlServerInstance
                    name           = $loginName
                } | ConvertTo-Json -Compress) | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete SQL login' {
            $loginName = "$($script:testLoginPrefix)Delete1"

            # Create a login to delete
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!Delete123'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Delete the login
            $deleteJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent login gracefully' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentLogin_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/Login --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Server Roles' -Tag 'ServerRoles' {
        It 'should assign server roles to login' {
            $loginName = "$($script:testLoginPrefix)Roles1"

            # Create login with server roles
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!Roles123'
                serverRoles    = @('dbcreator', 'securityadmin')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify roles
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.serverRoles | Should -Contain 'dbcreator'
            $getResult.actualState.serverRoles | Should -Contain 'securityadmin'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }

        It 'should update server roles' {
            $loginName = "$($script:testLoginPrefix)RolesUpdate1"

            # Create login with initial roles
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!RolesUp123'
                serverRoles    = @('dbcreator')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Update to different roles
            $updateJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
                serverRoles    = @('securityadmin', 'processadmin')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify new roles (should have only the new ones, not dbcreator)
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.serverRoles | Should -Not -Contain 'dbcreator'
            $getResult.actualState.serverRoles | Should -Contain 'securityadmin'
            $getResult.actualState.serverRoles | Should -Contain 'processadmin'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }
}
