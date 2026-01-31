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

    Write-Verbose -Message "SQL server is not available: $($script:sqlServerAvailable)" -Verbose
}

Describe 'SQL Server Database Permission Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testDbPrefix = 'OpenDscTestDb_'
        $script:testUserPrefix = 'OpenDscTestUser_'
        $script:testDb = "$($script:testDbPrefix)Permission"
        $script:testUser = "$($script:testUserPrefix)Permission"

        # Create test database and user for permission tests
        try
        {
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString
            $conn.Open()

            # Create test database
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDb)') CREATE DATABASE [$($script:testDb)]"
            $cmd.ExecuteNonQuery() | Out-Null

            # Switch to test database and create a test user
            $cmd.CommandText = "USE [$($script:testDb)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUser)') CREATE USER [$($script:testUser)] WITHOUT LOGIN"
            $cmd.ExecuteNonQuery() | Out-Null

            $conn.Close()
        }
        catch
        {
            Write-Warning "Failed to create test database/user: $_"
        }
    }

    AfterAll {
        # Cleanup test database
        if ($sqlServerAvailable)
        {
            try
            {
                . $helperScript

                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                # Revoke any permissions we might have granted (do this before dropping the database)
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "USE [$($script:testDb)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE VIEW DEFINITION FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE SHOWPLAN FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE BACKUP DATABASE FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "USE [master]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDb)') BEGIN ALTER DATABASE [$($script:testDb)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($script:testDb)]; END"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test database: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/DatabasePermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/DatabasePermission'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/DatabasePermission | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/DatabasePermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.databaseName | Should -Not -BeNullOrEmpty
            $result.properties.principal | Should -Not -BeNullOrEmpty
            $result.properties.permission | Should -Not -BeNullOrEmpty
            $result.properties.state | Should -Not -BeNullOrEmpty
        }

        It 'should have permission as string with pattern' {
            $result = dsc resource schema -r OpenDsc.SqlServer/DatabasePermission | ConvertFrom-Json
            $result.properties.permission.type | Should -Be 'string'
            $result.properties.permission.pattern | Should -Not -BeNullOrEmpty
        }

        It 'should have state enum with Grant, GrantWithGrant, and Deny' {
            $result = dsc resource schema -r OpenDsc.SqlServer/DatabasePermission | ConvertFrom-Json
            $stateEnum = $result.properties.state.enum
            $stateEnum | Should -Contain 'Grant'
            $stateEnum | Should -Contain 'GrantWithGrant'
            $stateEnum | Should -Contain 'Deny'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent permission' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.principal | Should -Be $script:testUser
            $result.actualState.permission | Should -Be 'Select'
        }
    }

    Context 'Set Operation - Grant Permission' -Tag 'Set' {
        AfterEach {
            # Revoke any database-level permissions granted during tests
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE VIEW DEFINITION FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE SHOWPLAN FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant VIEW DEFINITION permission' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'ViewDefinition'
                state          = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'ViewDefinition'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }

        It 'should grant permission with GRANT option' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'ViewDefinition'
                state          = 'GrantWithGrant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted with grant option
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'ViewDefinition'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'GrantWithGrant'
        }

        It 'should change permission state from Grant to Deny' {
            # First grant the permission (use database-level permission)
            $grantJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Showplan'
                state          = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $grantJson | Out-Null

            # Now deny the permission
            $denyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Showplan'
                state          = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $denyJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission state changed to Deny
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Showplan'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'Deny'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        BeforeEach {
            # Grant a database-level permission to delete
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "GRANT BACKUP DATABASE TO [$($script:testUser)]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch { }
        }

        It 'should revoke/delete a permission' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'BackupDatabase'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was revoked
            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should not fail when deleting non-existent permission' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Control'
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.SqlServer/DatabasePermission --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Deny and Remove Deny' -Tag 'Set' {
        AfterEach {
            # Clean up any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE ALTER FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should deny permission' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Alter'
                state          = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was denied
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Alter'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Deny'
        }

        It 'should remove deny by deleting permission' {
            # First deny the permission
            $denyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Alter'
                state          = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $denyJson | Out-Null

            # Now delete (revoke) the permission
            $deleteJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Alter'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/DatabasePermission --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission no longer exists
            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $deleteJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'GrantWithGrant to Grant Transition' -Tag 'Set' {
        AfterEach {
            # Clean up any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE REFERENCES FROM [$($script:testUser)] CASCADE"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should transition from GrantWithGrant to Grant' {
            # First grant with grant option
            $grantWithGrantJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'References'
                state          = 'GrantWithGrant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $grantWithGrantJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify GrantWithGrant state
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'References'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'GrantWithGrant'

            # Now change to just Grant (should revoke the grant option)
            $grantJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'References'
                state          = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $grantJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify state is now Grant (not GrantWithGrant)
            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'Grant'
        }
    }

    Context 'Database Role Permissions' -Tag 'Set' {
        BeforeAll {
            # Create a test database role
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'OpenDscTestRole' AND type = 'R') CREATE ROLE [OpenDscTestRole]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to create test role: $_"
            }
        }

        AfterAll {
            # Drop the test role
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT FROM [OpenDscTestRole]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "DROP ROLE IF EXISTS [OpenDscTestRole]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant permission to database role' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = 'OpenDscTestRole'
                permission     = 'Select'
                state          = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted to the role
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = 'OpenDscTestRole'
                permission     = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        AfterEach {
            # Clean up any permissions
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$(Get-SqlServerConnectionString);Database=$($script:testDb)"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE EXECUTE FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should be idempotent when granting same permission twice' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Execute'
                state          = 'Grant'
            } | ConvertTo-Json -Compress

            # First set
            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second set (should be idempotent)
            dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission is still granted
            $verifyJson = Get-SqlServerTestInput @{
                databaseName   = $script:testDb
                principal      = $script:testUser
                permission     = 'Execute'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }
    }
}
