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

Describe 'SQL Server Server Permission Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
    BeforeAll {
        . $helperScript

        $script:sqlServerInstance = if ($env:SQLSERVER_INSTANCE)
        { 
            $env:SQLSERVER_INSTANCE 
        }
        elseif ($IsLinux)
        { 
            'localhost' 
        }
        else
        { 
            '.' 
        }

        # Set SQL Authentication for Linux
        if ($IsLinux -and $env:SQLSERVER_SA_PASSWORD)
        {
            $script:sqlServerUsername = 'sa'
            $script:sqlServerPassword = $env:SQLSERVER_SA_PASSWORD
        }

        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testLoginPrefix = 'OpenDscTestLogin_'
        $script:testLogin = "$($script:testLoginPrefix)ServerPerm"

        # Create test login for permission tests
        try
        {
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString
            $conn.Open()

            # Create test login with a random password
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLogin)') CREATE LOGIN [$($script:testLogin)] WITH PASSWORD = 'P@ssw0rd123!', CHECK_POLICY = OFF"
            $cmd.ExecuteNonQuery() | Out-Null

            $conn.Close()
        }
        catch
        {
            Write-Warning "Failed to create test login: $_"
        }
    }

    AfterAll {
        # Cleanup test login and any permissions
        if ($sqlServerAvailable)
        {
            try
            {
                . $helperScript
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()

                # Revoke any permissions that might have been granted during tests
                $cmd.CommandText = "REVOKE VIEW SERVER STATE FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE VIEW ANY DATABASE FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE VIEW ANY ERROR LOG FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE VIEW ANY DEFINITION FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE ALTER ANY LOGIN FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE CONNECT SQL FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                # Drop the test login
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLogin)') DROP LOGIN [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test login: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/ServerPermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/ServerPermission'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/ServerPermission | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ServerPermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.principal | Should -Not -BeNullOrEmpty
            $result.properties.permission | Should -Not -BeNullOrEmpty
            $result.properties.state | Should -Not -BeNullOrEmpty
        }

        It 'should have permission as string with pattern' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ServerPermission | ConvertFrom-Json
            $result.properties.permission.type | Should -Be 'string'
            $result.properties.permission.pattern | Should -Not -BeNullOrEmpty
        }

        It 'should have state enum with Grant, GrantWithGrant, and Deny' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ServerPermission | ConvertFrom-Json
            $stateEnum = $result.properties.state.enum
            $stateEnum | Should -Contain 'Grant'
            $stateEnum | Should -Contain 'GrantWithGrant'
            $stateEnum | Should -Contain 'Deny'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent permission' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewServerState'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.principal | Should -Be $script:testLogin
            $result.actualState.permission | Should -Be 'ViewServerState'
        }
    }

    Context 'Set Operation - Grant Permission' -Tag 'Set' {
        AfterEach {
            # Revoke any server-level permissions granted during tests
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE VIEW SERVER STATE FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE VIEW ANY DATABASE FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant VIEW SERVER STATE permission' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewServerState'
                state      = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewServerState'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }

        It 'should grant permission with GRANT option' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewServerState'
                state      = 'GrantWithGrant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted with grant option
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewServerState'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'GrantWithGrant'
        }

        It 'should change permission state from Grant to Deny' {
            # First grant the permission
            $grantJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDatabase'
                state      = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $grantJson | Out-Null

            # Now deny the permission
            $denyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDatabase'
                state      = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $denyJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission state changed to Deny
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDatabase'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'Deny'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        BeforeEach {
            # Grant a server-level permission to delete
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "GRANT VIEW ANY ERROR LOG TO [$($script:testLogin)]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch { }
        }

        It 'should revoke/delete a permission' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyErrorLog'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was revoked
            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should not fail when deleting non-existent permission' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ControlServer'
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Deny and Remove Deny' -Tag 'Set' {
        AfterEach {
            # Cleanup any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE VIEW ANY DEFINITION FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should deny a permission' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDefinition'
                state      = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was denied
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDefinition'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Deny'
        }

        It 'should remove a denied permission' {
            # First deny the permission
            $denyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDefinition'
                state      = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $denyJson | Out-Null

            # Now remove the deny by deleting
            $deleteJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ViewAnyDefinition'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was removed
            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $deleteJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'GrantWithGrant to Grant Transition' -Tag 'Set' {
        AfterEach {
            # Cleanup any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE ALTER ANY LOGIN FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should change permission from GrantWithGrant to Grant' {
            # First grant with grant option
            $grantWithGrantJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'AlterAnyLogin'
                state      = 'GrantWithGrant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $grantWithGrantJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify GrantWithGrant
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'AlterAnyLogin'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'GrantWithGrant'

            # Now change to just Grant (remove the with grant option)
            $grantJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'AlterAnyLogin'
                state      = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $grantJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it changed to Grant
            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }
    }

    Context 'Server Role Permissions' -Tag 'Set' {
        BeforeAll {
            # Create a test server role
            $script:testRole = "$($script:testLoginPrefix)Role"
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testRole)' AND type = 'R') CREATE SERVER ROLE [$($script:testRole)]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to create test server role: $_"
            }
        }

        AfterAll {
            # Cleanup test server role
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()

                # Revoke permissions first
                $cmd.CommandText = "REVOKE VIEW SERVER STATE FROM [$($script:testRole)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE ALTER ANY ENDPOINT FROM [$($script:testRole)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                # Drop the server role
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testRole)' AND type = 'R') DROP SERVER ROLE [$($script:testRole)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test server role: $_"
            }
        }

        It 'should grant permission to a server role' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testRole
                permission = 'ViewServerState'
                state      = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testRole
                permission = 'ViewServerState'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }

        It 'should deny permission to a server role' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testRole
                permission = 'AlterAnyEndpoint'
                state      = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was denied
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testRole
                permission = 'AlterAnyEndpoint'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Deny'
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        AfterEach {
            # Cleanup any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE CONNECT SQL FROM [$($script:testLogin)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should be idempotent when granting the same permission twice' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ConnectSql'
                state      = 'Grant'
            } | ConvertTo-Json -Compress

            # First application
            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second application (should not fail)
            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission is still granted
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ConnectSql'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }

        It 'should be idempotent when denying the same permission twice' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ConnectSql'
                state      = 'Deny'
            } | ConvertTo-Json -Compress

            # First application
            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second application (should not fail)
            dsc resource set -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission is still denied
            $verifyJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ConnectSql'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Deny'
        }

        It 'should be idempotent when deleting a non-existent permission twice' {
            $inputJson = Get-SqlServerTestInput @{
                principal  = $script:testLogin
                permission = 'ConnectSql'
            } | ConvertTo-Json -Compress

            # First deletion (permission doesn't exist)
            dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second deletion (still doesn't exist)
            dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission still doesn't exist
            $result = dsc resource get -r OpenDsc.SqlServer/ServerPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }
}