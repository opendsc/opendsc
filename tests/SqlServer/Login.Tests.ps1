[CmdletBinding()]
param (
    [Parameter()]
    $UtilitiesPath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'tools')
)

$script:helperScript = Join-Path $UtilitiesPath 'Install-SqlServer.ps1'

BeforeDiscovery {
    # Dot-source script
    . $helperScript

    $script:sqlServerAvailable = Initialize-SqlServerForTests
}

Describe 'SQL Server Login Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
    BeforeAll {
        . $helperScript

        $script:sqlServerInstance = if ($env:SQLSERVER_INSTANCE) { 
            $env:SQLSERVER_INSTANCE 
        } elseif ($IsLinux) { 
            'localhost' 
        } else { 
            '.' 
        }

        # Set SQL Authentication for Linux
        if ($IsLinux -and $env:SQLSERVER_SA_PASSWORD) {
            $script:sqlServerUsername = 'sa'
            $script:sqlServerPassword = $env:SQLSERVER_SA_PASSWORD
        }

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
                $conn.ConnectionString = Get-SqlServerConnectionString
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
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentLogin_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Login --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentLogin_12345_XYZ'
        }

        It 'should return properties of existing sa login' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'sa'
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
            $inputJson = Get-SqlServerTestInput @{
                name                      = $loginName
                loginType                 = 'SqlLogin'
                password                  = 'T3stP@ssw0rd!Secure123'
                passwordPolicyEnforced    = $true
                passwordExpirationEnabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $loginName
            $getResult.actualState.loginType | Should -Be 'SqlLogin'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
        }

        It 'should update existing SQL login properties' {
            $loginName = "$($script:testLoginPrefix)Update1"

            # Create initial login
            $createJson = Get-SqlServerTestInput @{
                name            = $loginName
                loginType       = 'SqlLogin'
                password        = 'T3stP@ssw0rd!Initial123'
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Update the login
            $updateJson = Get-SqlServerTestInput @{
                name            = $loginName
                defaultDatabase = 'tempdb'
                disabled        = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = Get-SqlServerTestInput @{
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.defaultDatabase | Should -Be 'tempdb'
            $getResult.actualState.disabled | Should -Be $true

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name           = $loginName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete SQL login' {
            $loginName = "$($script:testLoginPrefix)Delete1"

            # Create a login to delete
            $createJson = Get-SqlServerTestInput @{
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!Delete123'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Delete the login
            $deleteJson = Get-SqlServerTestInput @{
                name           = $loginName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/Login --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $verifyJson = Get-SqlServerTestInput @{
                name           = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent login gracefully' {
            $inputJson = Get-SqlServerTestInput @{
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
            $createJson = Get-SqlServerTestInput @{
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!Roles123'
                serverRoles    = @('dbcreator', 'securityadmin')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify roles
            $verifyJson = Get-SqlServerTestInput @{
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
            $createJson = Get-SqlServerTestInput @{
                name           = $loginName
                loginType      = 'SqlLogin'
                password       = 'T3stP@ssw0rd!RolesUp123'
                serverRoles    = @('dbcreator')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Update to different roles
            $updateJson = Get-SqlServerTestInput @{
                name           = $loginName
                serverRoles    = @('securityadmin', 'processadmin')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify new roles (should have only the new ones, not dbcreator)
            $verifyJson = Get-SqlServerTestInput @{
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

    Context 'Create Disabled Login' -Tag 'Set' {
        It 'should create a login that is disabled from the start' {
            $loginName = "$($script:testLoginPrefix)Disabled1"
            $inputJson = Get-SqlServerTestInput @{
                name      = $loginName
                loginType = 'SqlLogin'
                password  = 'T3stP@ssw0rd!Disabled123'
                disabled  = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created and is disabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $loginName
            $getResult.actualState.disabled | Should -Be $true
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }

    Context 'Password Change' -Tag 'Set' {
        It 'should change password on existing login' {
            $loginName = "$($script:testLoginPrefix)PwdChange1"

            # Create login with initial password
            $createJson = Get-SqlServerTestInput @{
                name      = $loginName
                loginType = 'SqlLogin'
                password  = 'T3stP@ssw0rd!Initial123'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Change the password
            $updateJson = Get-SqlServerTestInput @{
                name     = $loginName
                password = 'T3stP@ssw0rd!Changed456'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify login still exists (password is write-only, can't verify directly)
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $loginName
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }

    Context 'Password Policy Toggle' -Tag 'Set' {
        It 'should enable password policy on existing login' {
            $loginName = "$($script:testLoginPrefix)Policy1"

            # Create login with password policy disabled
            $createJson = Get-SqlServerTestInput @{
                name                   = $loginName
                loginType              = 'SqlLogin'
                password               = 'T3stP@ssw0rd!Policy123'
                passwordPolicyEnforced = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Enable password policy
            $updateJson = Get-SqlServerTestInput @{
                name                   = $loginName
                passwordPolicyEnforced = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify policy is enabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordPolicyEnforced | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }

        It 'should disable password policy on existing login' {
            $loginName = "$($script:testLoginPrefix)Policy2"

            # Create login with password policy enabled
            $createJson = Get-SqlServerTestInput @{
                name                   = $loginName
                loginType              = 'SqlLogin'
                password               = 'T3stP@ssw0rd!Policy456'
                passwordPolicyEnforced = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Disable password policy
            $updateJson = Get-SqlServerTestInput @{
                name                   = $loginName
                passwordPolicyEnforced = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify policy is disabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordPolicyEnforced | Should -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }

    Context 'Password Expiration Toggle' -Tag 'Set' {
        It 'should enable password expiration on existing login' {
            $loginName = "$($script:testLoginPrefix)Expiry1"

            # Create login with password expiration disabled (policy must be enabled for expiration)
            $createJson = Get-SqlServerTestInput @{
                name                      = $loginName
                loginType                 = 'SqlLogin'
                password                  = 'T3stP@ssw0rd!Expiry123'
                passwordPolicyEnforced    = $true
                passwordExpirationEnabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Enable password expiration
            $updateJson = Get-SqlServerTestInput @{
                name                      = $loginName
                passwordExpirationEnabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify expiration is enabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordExpirationEnabled | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }

        It 'should disable password expiration on existing login' {
            $loginName = "$($script:testLoginPrefix)Expiry2"

            # Create login with password expiration enabled
            $createJson = Get-SqlServerTestInput @{
                name                      = $loginName
                loginType                 = 'SqlLogin'
                password                  = 'T3stP@ssw0rd!Expiry456'
                passwordPolicyEnforced    = $true
                passwordExpirationEnabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Disable password expiration
            $updateJson = Get-SqlServerTestInput @{
                name                      = $loginName
                passwordExpirationEnabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify expiration is disabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordExpirationEnabled | Should -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }

    Context 'Enable Disabled Login' -Tag 'Set' {
        It 'should re-enable a disabled login' {
            $loginName = "$($script:testLoginPrefix)ReEnable1"

            # Create a disabled login
            $createJson = Get-SqlServerTestInput @{
                name      = $loginName
                loginType = 'SqlLogin'
                password  = 'T3stP@ssw0rd!ReEnable123'
                disabled  = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $createJson | Out-Null

            # Verify it's disabled
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.disabled | Should -Be $true

            # Re-enable the login
            $updateJson = Get-SqlServerTestInput @{
                name     = $loginName
                disabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Login --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it's now enabled
            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.disabled | Should -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        It 'should be idempotent when creating same login twice' {
            $loginName = "$($script:testLoginPrefix)Idempotent1"
            $inputJson = Get-SqlServerTestInput @{
                name            = $loginName
                loginType       = 'SqlLogin'
                password        = 'T3stP@ssw0rd!Idempotent123'
                defaultDatabase = 'master'
            } | ConvertTo-Json -Compress

            # First set
            dsc resource set -r OpenDsc.SqlServer/Login --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second set (should be idempotent)
            dsc resource set -r OpenDsc.SqlServer/Login --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the login exists with correct properties
            $verifyJson = Get-SqlServerTestInput @{
                name = $loginName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Login --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $loginName
            $getResult.actualState.defaultDatabase | Should -Be 'master'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Login --input $verifyJson | Out-Null
        }
    }
}
