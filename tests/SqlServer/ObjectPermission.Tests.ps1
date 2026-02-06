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

Describe 'SQL Server Object Permission Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testDbPrefix = 'OpenDscTestDb_'
        $script:testUserPrefix = 'OpenDscTestUser_'
        $script:testTablePrefix = 'OpenDscTestTable_'
        $script:testViewPrefix = 'OpenDscTestView_'
        $script:testProcPrefix = 'OpenDscTestProc_'
        $script:testFuncPrefix = 'OpenDscTestFunc_'
        $script:testSchemaPrefix = 'OpenDscTestSchema_'

        $script:testDb = "$($script:testDbPrefix)ObjPerm"
        $script:testUser = "$($script:testUserPrefix)ObjPerm"
        $script:testUser2 = "$($script:testUserPrefix)ObjPerm2"
        $script:testTable = "$($script:testTablePrefix)ObjPerm"
        $script:testView = "$($script:testViewPrefix)ObjPerm"
        $script:testProc = "$($script:testProcPrefix)ObjPerm"
        $script:testFunc = "$($script:testFuncPrefix)ObjPerm"
        $script:testSchema = "$($script:testSchemaPrefix)ObjPerm"

        # Create test database, users, and objects
        try
        {
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString
            $conn.Open()

            # Create test database
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDb)') CREATE DATABASE [$($script:testDb)]"
            $cmd.ExecuteNonQuery() | Out-Null

            $conn.Close()

            # Reconnect to test database to create objects
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
            $conn.Open()

            # Create test users
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUser)') CREATE USER [$($script:testUser)] WITHOUT LOGIN"
            $cmd.ExecuteNonQuery() | Out-Null

            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUser2)') CREATE USER [$($script:testUser2)] WITHOUT LOGIN"
            $cmd.ExecuteNonQuery() | Out-Null

            # Create test schema
            $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '$($script:testSchema)') EXEC('CREATE SCHEMA [$($script:testSchema)]')"
            $cmd.ExecuteNonQuery() | Out-Null

            # Create test table
            $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '$($script:testTable)' AND schema_id = SCHEMA_ID('dbo'))
    CREATE TABLE dbo.[$($script:testTable)] (Id INT PRIMARY KEY, Name NVARCHAR(100))
"@
            $cmd.ExecuteNonQuery() | Out-Null

            # Create test view
            $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = '$($script:testView)' AND schema_id = SCHEMA_ID('dbo'))
    EXEC('CREATE VIEW dbo.[$($script:testView)] AS SELECT Id, Name FROM dbo.[$($script:testTable)]')
"@
            $cmd.ExecuteNonQuery() | Out-Null

            # Create test stored procedure
            $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = '$($script:testProc)' AND schema_id = SCHEMA_ID('dbo'))
    EXEC('CREATE PROCEDURE dbo.[$($script:testProc)] AS SELECT 1')
"@
            $cmd.ExecuteNonQuery() | Out-Null

            # Create test function
            $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = '$($script:testFunc)' AND schema_id = SCHEMA_ID('dbo') AND type = 'FN')
    EXEC('CREATE FUNCTION dbo.[$($script:testFunc)]() RETURNS INT AS BEGIN RETURN 1 END')
"@
            $cmd.ExecuteNonQuery() | Out-Null

            $conn.Close()
        }
        catch
        {
            Write-Warning "Failed to create test database/objects: $_"
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

                $cmd = $conn.CreateCommand()
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
            $result = dsc resource list OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/ObjectPermission'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.databaseName | Should -Not -BeNullOrEmpty
            $result.properties.objectType | Should -Not -BeNullOrEmpty
            $result.properties.objectName | Should -Not -BeNullOrEmpty
            $result.properties.principal | Should -Not -BeNullOrEmpty
            $result.properties.permission | Should -Not -BeNullOrEmpty
            $result.properties.state | Should -Not -BeNullOrEmpty
        }

        It 'should have objectType enum with supported types' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $objectTypeEnum = $result.properties.objectType.enum
            $objectTypeEnum | Should -Contain 'Table'
            $objectTypeEnum | Should -Contain 'View'
            $objectTypeEnum | Should -Contain 'StoredProcedure'
            $objectTypeEnum | Should -Contain 'UserDefinedFunction'
            $objectTypeEnum | Should -Contain 'Schema'
            $objectTypeEnum | Should -Contain 'Sequence'
            $objectTypeEnum | Should -Contain 'Synonym'
        }

        It 'should have state enum with Grant, GrantWithGrant, and Deny' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $stateEnum = $result.properties.state.enum
            $stateEnum | Should -Contain 'Grant'
            $stateEnum | Should -Contain 'GrantWithGrant'
            $stateEnum | Should -Contain 'Deny'
        }

        It 'should have schemaName as optional property' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ObjectPermission | ConvertFrom-Json
            $result.required | Should -Not -Contain 'schemaName'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                schemaName   = 'dbo'
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.principal | Should -Be $script:testUser
            $result.actualState.permission | Should -Be 'Select'
            $result.actualState.objectType | Should -Be 'Table'
        }

        It 'should return _exist=false for non-existent permission on stored procedure' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'StoredProcedure'
                objectName   = $script:testProc
                principal    = $script:testUser
                permission   = 'Execute'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.objectType | Should -Be 'StoredProcedure'
        }

        It 'should default schemaName to dbo when not specified' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState.schemaName | Should -Be 'dbo'
        }

        It 'should throw error for non-existent object' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = 'NonExistentTable12345'
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Set Operation - Table Permissions' -Tag 'Set' {
        AfterEach {
            # Revoke any permissions granted during tests
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE INSERT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE UPDATE ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE DELETE ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant SELECT permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify the permission was granted
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }

        It 'should grant INSERT permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                schemaName   = 'dbo'
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Insert'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.permission | Should -Be 'Insert'
        }

        It 'should grant UPDATE permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Update'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
        }

        It 'should grant DELETE permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Delete'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
        }

        It 'should deny SELECT permission on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Deny'
        }

        It 'should grant SELECT with GRANT option on table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'GrantWithGrant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'GrantWithGrant'
        }

        It 'should change permission state from Grant to Deny' {
            # First grant
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'Grant'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Then change to deny
            $denyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $denyJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'Deny'
        }
    }

    Context 'Set Operation - View Permissions' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT ON dbo.[$($script:testView)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant SELECT permission on view' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'View'
                objectName   = $script:testView
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.objectType | Should -Be 'View'
        }
    }

    Context 'Set Operation - Stored Procedure Permissions' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE EXECUTE ON dbo.[$($script:testProc)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant EXECUTE permission on stored procedure' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'StoredProcedure'
                objectName   = $script:testProc
                principal    = $script:testUser
                permission   = 'Execute'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.objectType | Should -Be 'StoredProcedure'
            $result.actualState.permission | Should -Be 'Execute'
        }

        It 'should deny EXECUTE permission on stored procedure' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'StoredProcedure'
                objectName   = $script:testProc
                principal    = $script:testUser
                permission   = 'Execute'
                state        = 'Deny'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState.state | Should -Be 'Deny'
        }
    }

    Context 'Set Operation - Function Permissions' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE EXECUTE ON dbo.[$($script:testFunc)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant EXECUTE permission on user-defined function' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'UserDefinedFunction'
                objectName   = $script:testFunc
                principal    = $script:testUser
                permission   = 'Execute'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.objectType | Should -Be 'UserDefinedFunction'
        }
    }

    Context 'Set Operation - Schema Permissions' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT ON SCHEMA::[$($script:testSchema)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE EXECUTE ON SCHEMA::[$($script:testSchema)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant SELECT permission on schema' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Schema'
                objectName   = $script:testSchema
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.objectType | Should -Be 'Schema'
        }

        It 'should grant EXECUTE permission on schema' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Schema'
                objectName   = $script:testSchema
                principal    = $script:testUser
                permission   = 'Execute'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        BeforeEach {
            # Grant a permission to revoke
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "GRANT SELECT ON dbo.[$($script:testTable)] TO [$($script:testUser)]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch { }
        }

        It 'should revoke SELECT permission from table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                _exist       = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify revoked
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should handle revoking non-existent permission gracefully' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser2
                permission   = 'Delete'
                _exist       = $false
            } | ConvertTo-Json -Compress

            # Should not throw
            dsc resource delete -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }

    Context 'Delete Operation - Deny Permissions' -Tag 'Delete' {
        BeforeEach {
            # Deny a permission to revoke
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "DENY INSERT ON dbo.[$($script:testTable)] TO [$($script:testUser)]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch { }
        }

        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE INSERT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should revoke denied permission from table' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Insert'
                _exist       = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify revoked
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Insert'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'Multiple Permissions on Same Object' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE INSERT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $cmd.CommandText = "REVOKE UPDATE ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should grant multiple permissions on same table' {
            # Grant SELECT
            $selectJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $selectJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Grant INSERT
            $insertJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Insert'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $insertJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Grant UPDATE
            $updateJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Update'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all three
            $selectResult = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $selectJson | ConvertFrom-Json
            $selectResult.actualState._exist | Should -Not -Be $false

            $insertResult = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $insertJson | ConvertFrom-Json
            $insertResult.actualState._exist | Should -Not -Be $false

            $updateResult = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $updateJson | ConvertFrom-Json
            $updateResult.actualState._exist | Should -Not -Be $false
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        AfterEach {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString -Database $script:testDb
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "REVOKE SELECT ON dbo.[$($script:testTable)] FROM [$($script:testUser)]"
                try { $cmd.ExecuteNonQuery() | Out-Null } catch { }

                $conn.Close()
            }
            catch { }
        }

        It 'should be idempotent when permission already exists with same state' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'Select'
                state        = 'Grant'
            } | ConvertTo-Json -Compress

            # First set
            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second set (should be idempotent)
            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify still granted
            $result = dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.state | Should -Be 'Grant'
        }
    }

    Context 'Error Handling' -Tag 'Error' {
        It 'should fail for invalid permission name' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = $script:testUser
                permission   = 'InvalidPermission'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should fail for non-existent database' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = 'NonExistentDatabase12345'
                objectType   = 'Table'
                objectName   = 'SomeTable'
                principal    = 'SomeUser'
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should fail for non-existent principal' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDb
                objectType   = 'Table'
                objectName   = $script:testTable
                principal    = 'NonExistentUser12345'
                permission   = 'Select'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
