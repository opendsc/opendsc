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

Describe 'SQL Server Database Role Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testRolePrefix = 'OpenDscTestRole_'
        $script:testUserPrefix = 'OpenDscTestUser_'
        $script:testDatabase = 'OpenDscTestDb_DatabaseRole'

        # Create test database
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = Get-SqlServerConnectionString
        $conn.Open()

        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDatabase)') CREATE DATABASE [$($script:testDatabase)]"
        $cmd.ExecuteNonQuery() | Out-Null

        # Create test users in the database
        $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)User1') CREATE USER [$($script:testUserPrefix)User1] WITHOUT LOGIN"
        $cmd.ExecuteNonQuery() | Out-Null

        $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)User2') CREATE USER [$($script:testUserPrefix)User2] WITHOUT LOGIN"
        $cmd.ExecuteNonQuery() | Out-Null

        $cmd.CommandText = "USE [$($script:testDatabase)]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($script:testUserPrefix)User3') CREATE USER [$($script:testUserPrefix)User3] WITHOUT LOGIN"
        $cmd.ExecuteNonQuery() | Out-Null

        $conn.Close()
    }

    AfterAll {
        # Cleanup test database
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$($script:testDatabase)') BEGIN ALTER DATABASE [$($script:testDatabase)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($script:testDatabase)]; END"
                $cmd.ExecuteNonQuery() | Out-Null

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
            $result = dsc resource list OpenDsc.SqlServer/DatabaseRole | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/DatabaseRole'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/DatabaseRole | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/DatabaseRole | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.databaseName | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.members | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent role' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'NonExistentRole_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentRole_12345_XYZ'
        }

        It 'should return properties of existing fixed role' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'db_datareader'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'db_datareader'
            $result.actualState.isFixedRole | Should -Be $true
            $result.actualState._exist | Should -Not -Be $false
        }
    }

    Context 'Set Operation - Create Role' -Tag 'Set' {
        It 'should create a new custom database role' {
            $roleName = "$($script:testRolePrefix)Create1"
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                owner        = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $roleName
            $getResult.actualState.owner | Should -Be 'dbo'
            $getResult.actualState.isFixedRole | Should -Be $false
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }

        It 'should create role with members' {
            $roleName = "$($script:testRolePrefix)WithMembers1"
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                owner        = 'dbo'
                members      = @("$($script:testUserPrefix)User1", "$($script:testUserPrefix)User2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify members
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User1"
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User2"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }
    }

    Context 'Set Operation - Update Role' -Tag 'Set' {
        It 'should change role owner' {
            $roleName = "$($script:testRolePrefix)ChangeOwner1"

            # Create role
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                owner        = 'dbo'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $createJson | Out-Null

            # Change owner to a test user
            $updateJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                owner        = "$($script:testUserPrefix)User1"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -Be "$($script:testUserPrefix)User1"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }
    }

    Context 'Members - Additive Mode' -Tag 'Set' {
        It 'should add members without removing existing ones (default)' {
            $roleName = "$($script:testRolePrefix)Additive1"

            # Create role with initial member
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @("$($script:testUserPrefix)User1")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $createJson | Out-Null

            # Add more members (default additive mode)
            $updateJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @("$($script:testUserPrefix)User2", "$($script:testUserPrefix)User3")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all members exist
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User1"
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User2"
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User3"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }
    }

    Context 'Members - Purge Mode' -Tag 'Set' {
        It 'should replace members when purge is true' {
            $roleName = "$($script:testRolePrefix)Purge1"

            # Create role with initial members
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @("$($script:testUserPrefix)User1", "$($script:testUserPrefix)User2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $createJson | Out-Null

            # Replace members with purge=true
            $updateJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @("$($script:testUserPrefix)User3")
                _purge       = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify only new member exists
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Not -Contain "$($script:testUserPrefix)User1"
            $getResult.actualState.members | Should -Not -Contain "$($script:testUserPrefix)User2"
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User3"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }

        It 'should remove all members when purge is true with empty list' {
            $roleName = "$($script:testRolePrefix)PurgeEmpty1"

            # Create role with members
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @("$($script:testUserPrefix)User1", "$($script:testUserPrefix)User2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $createJson | Out-Null

            # Remove all members
            $updateJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                members      = @()
                _purge       = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify no members
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -BeNullOrEmpty

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }
    }

    Context 'Fixed Role Membership' -Tag 'Set' {
        It 'should add members to fixed database role' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'db_datareader'
                members      = @("$($script:testUserPrefix)User1")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify member added
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'db_datareader'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User1"

            # Cleanup - remove member from fixed role
            $cleanupJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'db_datareader'
                members      = @()
                _purge       = $true
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $cleanupJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete custom database role' {
            $roleName = "$($script:testRolePrefix)Delete1"

            # Create a role to delete
            $createJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $createJson | Out-Null

            # Delete the role
            $deleteJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $deleteJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent role gracefully' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'NonExistentRole_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $inputJson } | Should -Not -Throw
        }

        It 'should fail when trying to delete fixed role' {
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = 'db_datareader'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        It 'should be idempotent when creating same role twice' {
            $roleName = "$($script:testRolePrefix)Idempotent1"
            $inputJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
                owner        = 'dbo'
                members      = @("$($script:testUserPrefix)User1")
            } | ConvertTo-Json -Compress

            # First set
            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second set (should be idempotent)
            dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                databaseName = $script:testDatabase
                name         = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $roleName
            $getResult.actualState.members | Should -Contain "$($script:testUserPrefix)User1"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input $verifyJson | Out-Null
        }
    }
}
