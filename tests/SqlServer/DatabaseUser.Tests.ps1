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

Describe 'SQL Server Database User Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testUserPrefix = 'OpenDscTestDbUser_'
        $script:testLoginPrefix = 'OpenDscTestLogin_'
        $script:testDatabase = 'OpenDscTestDb_DatabaseUser'

        # Create test database
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = Get-SqlServerConnectionString
        $conn.Open()

        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDatabase)') CREATE DATABASE [$($script:testDatabase)]"
        $cmd.ExecuteNonQuery() | Out-Null

        # Create test logins
        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login1') CREATE LOGIN [$($script:testLoginPrefix)Login1] WITH PASSWORD = 'P@ssw0rd123!', CHECK_POLICY = OFF"
        $cmd.ExecuteNonQuery() | Out-Null

        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login2') CREATE LOGIN [$($script:testLoginPrefix)Login2] WITH PASSWORD = 'P@ssw0rd123!', CHECK_POLICY = OFF"
        $cmd.ExecuteNonQuery() | Out-Null

        $conn.Close()
    }

    AfterAll {
        # Cleanup test database and logins
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()

                # Drop test database
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDatabase)') BEGIN ALTER DATABASE [$($script:testDatabase)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($script:testDatabase)]; END"
                $cmd.ExecuteNonQuery() | Out-Null

                # Drop test logins
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login1') DROP LOGIN [$($script:testLoginPrefix)Login1]"
                $cmd.ExecuteNonQuery() | Out-Null

                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login2') DROP LOGIN [$($script:testLoginPrefix)Login2]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test resources: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/DatabaseUser | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/DatabaseUser'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/DatabaseUser | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/DatabaseUser | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.databaseName | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.login | Should -Not -BeNullOrEmpty
            $result.properties.defaultSchema | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent user' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'NonExistentUser_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentUser_12345_XYZ'
        }

        It 'should return properties of existing system user dbo' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'dbo'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'dbo'
            $result.actualState.isSystemObject | Should -Be $true
            $result.actualState._exist | Should -Not -Be $false
        }

        It 'should return _exist=false when database does not exist' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = 'NonExistentDatabase_12345_XYZ'
                name         = 'SomeUser'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }
    }

    Context 'Set Operation - Create User' -Tag 'Set' {
        It 'should create a user mapped to a login' {
            $userName = "$($script:testUserPrefix)Create1"
            $inputJson = Get-SqlServerTestInput @{
                databaseName  = $script:testDatabase
                name          = $userName
                login         = "$($script:testLoginPrefix)Login1"
                defaultSchema = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $userName
            $getResult.actualState.login | Should -Be "$($script:testLoginPrefix)Login1"
            $getResult.actualState.defaultSchema | Should -Be 'dbo'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | Out-Null
        }

        It 'should create a user without login (NoLogin type)' {
            $userName = "$($script:testUserPrefix)NoLogin1"
            $inputJson = Get-SqlServerTestInput @{
                databaseName  = $script:testDatabase
                name          = $userName
                userType      = 'NoLogin'
                defaultSchema = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $userName
            $getResult.actualState.userType | Should -Be 'NoLogin'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | Out-Null
        }
    }

    Context 'Set Operation - Update User' -Tag 'Set' {
        It 'should update user default schema' {
            $userName = "$($script:testUserPrefix)Update1"

            # Create user with initial schema
            $createJson = Get-SqlServerTestInput @{
                databaseName  = $script:testDatabase
                name          = $userName
                login         = "$($script:testLoginPrefix)Login1"
                defaultSchema = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $createJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Create a test schema for the user to use
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'testschema') EXEC('CREATE SCHEMA testschema')"
            $cmd.ExecuteNonQuery() | Out-Null
            $conn.Close()

            # Update to new schema
            $updateJson = Get-SqlServerTestInput @{
                databaseName  = $script:testDatabase
                name          = $userName
                defaultSchema = 'testschema'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.defaultSchema | Should -Be 'testschema'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | Out-Null
        }

        It 'should be idempotent when user already in desired state' {
            $userName = "$($script:testUserPrefix)Idempotent1"

            # Create user
            $inputJson = Get-SqlServerTestInput @{
                databaseName  = $script:testDatabase
                name          = $userName
                login         = "$($script:testLoginPrefix)Login1"
                defaultSchema = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Run set again with same values
            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify state unchanged
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $userName
            $getResult.actualState.login | Should -Be "$($script:testLoginPrefix)Login1"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $verifyJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete existing user' {
            $userName = "$($script:testUserPrefix)Delete1"

            # Create user
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
                login        = "$($script:testLoginPrefix)Login1"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $createJson | Out-Null

            # Delete user
            $deleteJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $userName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input $deleteJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent user' {
            $deleteJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'NonExistentUser_ToDelete_12345'
            } | ConvertTo-Json -Compress

            # Should not throw error
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0
        }

        It 'should not allow deleting system user dbo' {
            $deleteJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'dbo'
            } | ConvertTo-Json -Compress

            # Should fail
            dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input $deleteJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        BeforeAll {
            # Create test users for export
            $conn = New-Object System.Data.SqlClient.SqlConnection
            $conn.ConnectionString = Get-SqlServerConnectionString
            $conn.Open()

            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)Export1') CREATE USER [$($script:testUserPrefix)Export1] FOR LOGIN [$($script:testLoginPrefix)Login1]"
            $cmd.ExecuteNonQuery() | Out-Null

            $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)Export2') CREATE USER [$($script:testUserPrefix)Export2] FOR LOGIN [$($script:testLoginPrefix)Login2]"
            $cmd.ExecuteNonQuery() | Out-Null

            $conn.Close()
        }

        AfterAll {
            # Cleanup export test users
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "USE [$($script:testDatabase)]; IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)Export1') DROP USER [$($script:testUserPrefix)Export1]"
                $cmd.ExecuteNonQuery() | Out-Null

                $cmd.CommandText = "USE [$($script:testDatabase)]; IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)Export2') DROP USER [$($script:testUserPrefix)Export2]"
                $cmd.ExecuteNonQuery() | Out-Null

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup export test users: $_"
            }
        }

        It 'should export database users from specific database' {
            $filterJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
            } | ConvertTo-Json -Compress
            $result = dsc resource export -r OpenDsc.SqlServer/DatabaseUser --input $filterJson | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty

            # Should find our test users
            $exportUser1 = $result.resources | Where-Object { $_.properties.name -eq "$($script:testUserPrefix)Export1" }
            $exportUser1 | Should -Not -BeNullOrEmpty

            $exportUser2 = $result.resources | Where-Object { $_.properties.name -eq "$($script:testUserPrefix)Export2" }
            $exportUser2 | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Error Handling' -Tag 'Error' {
        It 'should fail when database does not exist for set operation' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = 'NonExistentDatabase_12345'
                name         = 'SomeUser'
                login        = 'SomeLogin'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should fail when login does not exist' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = "$($script:testUserPrefix)InvalidLogin"
                login        = 'NonExistentLogin_12345_XYZ'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
