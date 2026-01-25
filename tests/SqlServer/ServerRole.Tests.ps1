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

Describe 'SQL Server Server Role Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testRolePrefix = 'OpenDscTestServerRole_'
        $script:testLoginPrefix = 'OpenDscTestLogin_'

        # Create test logins for member testing
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = Get-SqlServerConnectionString
        $conn.Open()

        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login1') CREATE LOGIN [$($script:testLoginPrefix)Login1] WITH PASSWORD = 'P@ssw0rd123!'"
        $cmd.ExecuteNonQuery() | Out-Null

        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login2') CREATE LOGIN [$($script:testLoginPrefix)Login2] WITH PASSWORD = 'P@ssw0rd123!'"
        $cmd.ExecuteNonQuery() | Out-Null

        $cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login3') CREATE LOGIN [$($script:testLoginPrefix)Login3] WITH PASSWORD = 'P@ssw0rd123!'"
        $cmd.ExecuteNonQuery() | Out-Null

        $conn.Close()
    }

    AfterAll {
        # Cleanup test logins and roles
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()

                # Drop test roles first
                $cmd.CommandText = "SELECT name FROM sys.server_principals WHERE type = 'R' AND name LIKE '$($script:testRolePrefix)%'"
                $reader = $cmd.ExecuteReader()
                $rolesToDrop = @()
                while ($reader.Read()) {
                    $rolesToDrop += $reader.GetString(0)
                }
                $reader.Close()

                foreach ($role in $rolesToDrop) {
                    $cmd.CommandText = "DROP SERVER ROLE [$role]"
                    $cmd.ExecuteNonQuery() | Out-Null
                }

                # Drop test logins
                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login1') DROP LOGIN [$($script:testLoginPrefix)Login1]"
                $cmd.ExecuteNonQuery() | Out-Null

                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login2') DROP LOGIN [$($script:testLoginPrefix)Login2]"
                $cmd.ExecuteNonQuery() | Out-Null

                $cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($script:testLoginPrefix)Login3') DROP LOGIN [$($script:testLoginPrefix)Login3]"
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
            $result = dsc resource list OpenDsc.SqlServer/ServerRole | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/ServerRole'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/ServerRole | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/ServerRole | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.members | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent role' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentServerRole_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentServerRole_12345_XYZ'
        }

        It 'should return properties of existing fixed role' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'sysadmin'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'sysadmin'
            $result.actualState.isFixedRole | Should -Be $true
            $result.actualState._exist | Should -Not -Be $false
        }
    }

    Context 'Set Operation - Create Role' -Tag 'Set' {
        It 'should create a new custom server role' {
            $roleName = "$($script:testRolePrefix)Create1"
            $inputJson = Get-SqlServerTestInput @{
                name  = $roleName
                owner = 'sa'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $roleName
            $getResult.actualState.owner | Should -Be 'sa'
            $getResult.actualState.isFixedRole | Should -Be $false
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }

        It 'should create role with members' {
            $roleName = "$($script:testRolePrefix)WithMembers1"
            $inputJson = Get-SqlServerTestInput @{
                name    = $roleName
                owner   = 'sa'
                members = @("$($script:testLoginPrefix)Login1", "$($script:testLoginPrefix)Login2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify members
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login1"
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login2"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }
    }

    Context 'Set Operation - Update Role' -Tag 'Set' {
        It 'should change role owner' {
            $roleName = "$($script:testRolePrefix)ChangeOwner1"

            # Create role
            $createJson = Get-SqlServerTestInput @{
                name  = $roleName
                owner = 'sa'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $createJson | Out-Null

            # Change owner to a test login
            $updateJson = Get-SqlServerTestInput @{
                name  = $roleName
                owner = "$($script:testLoginPrefix)Login1"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -Be "$($script:testLoginPrefix)Login1"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }
    }

    Context 'Members - Additive Mode' -Tag 'Set' {
        It 'should add members without removing existing ones (default)' {
            $roleName = "$($script:testRolePrefix)Additive1"

            # Create role with initial member
            $createJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @("$($script:testLoginPrefix)Login1")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $createJson | Out-Null

            # Add more members (default additive mode)
            $updateJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @("$($script:testLoginPrefix)Login2", "$($script:testLoginPrefix)Login3")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all members exist
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login1"
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login2"
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login3"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }
    }

    Context 'Members - Purge Mode' -Tag 'Set' {
        It 'should replace members when purge is true' {
            $roleName = "$($script:testRolePrefix)Purge1"

            # Create role with initial members
            $createJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @("$($script:testLoginPrefix)Login1", "$($script:testLoginPrefix)Login2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $createJson | Out-Null

            # Replace members with purge=true
            $updateJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @("$($script:testLoginPrefix)Login3")
                _purge  = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify only new member exists
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Not -Contain "$($script:testLoginPrefix)Login1"
            $getResult.actualState.members | Should -Not -Contain "$($script:testLoginPrefix)Login2"
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login3"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }

        It 'should remove all members when purge is true with empty list' {
            $roleName = "$($script:testRolePrefix)PurgeEmpty1"

            # Create role with members
            $createJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @("$($script:testLoginPrefix)Login1", "$($script:testLoginPrefix)Login2")
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $createJson | Out-Null

            # Remove all members
            $updateJson = Get-SqlServerTestInput @{
                name    = $roleName
                members = @()
                _purge  = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify no members
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.members | Should -BeNullOrEmpty

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete custom server role' {
            $roleName = "$($script:testRolePrefix)Delete1"

            # Create a role to delete
            $createJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $createJson | Out-Null

            # Delete the role
            $deleteJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $deleteJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent role gracefully' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentServerRole_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $inputJson } | Should -Not -Throw
        }

        It 'should fail when trying to delete fixed role' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'sysadmin'
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Idempotency' -Tag 'Set' {
        It 'should be idempotent when creating same role twice' {
            $roleName = "$($script:testRolePrefix)Idempotent1"
            $inputJson = Get-SqlServerTestInput @{
                name    = $roleName
                owner   = 'sa'
                members = @("$($script:testLoginPrefix)Login1")
            } | ConvertTo-Json -Compress

            # First set
            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Second set (should be idempotent)
            dsc resource set -r OpenDsc.SqlServer/ServerRole --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $roleName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/ServerRole --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $roleName
            $getResult.actualState.members | Should -Contain "$($script:testLoginPrefix)Login1"

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/ServerRole --input $verifyJson | Out-Null
        }
    }
}
