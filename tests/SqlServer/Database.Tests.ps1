# Skip SQL Server tests if no SQL Server instance is available
BeforeDiscovery {
    # Load shared SQL Server installation script
    . $PSScriptRoot/Install-SqlServer.ps1

    # Initialize SQL Server (installs if in GitHub Actions)
    $script:sqlServerAvailable = Initialize-SqlServerForTests
}

Describe 'SQL Server Database Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
    BeforeAll {
        $script:sqlServerInstance = if ($env:SQLSERVER_INSTANCE) { $env:SQLSERVER_INSTANCE } else { '.' }

        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testDbPrefix = 'OpenDscTestDb_'
    }

    AfterAll {
        # Cleanup any test databases that may have been left behind
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "Server=$script:sqlServerInstance;Integrated Security=True"
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "SELECT name FROM sys.databases WHERE name LIKE '$($script:testDbPrefix)%'"
                $reader = $cmd.ExecuteReader()

                $dbsToDelete = @()
                while ($reader.Read())
                {
                    $dbsToDelete += $reader.GetString(0)
                }
                $reader.Close()

                foreach ($db in $dbsToDelete)
                {
                    $dropCmd = $conn.CreateCommand()
                    $dropCmd.CommandText = "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]"
                    try { $dropCmd.ExecuteNonQuery() | Out-Null } catch { }
                }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test databases: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/Database | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/Database'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/Database | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'test'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Database | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.collation | Should -Not -BeNullOrEmpty
            $result.properties.recoveryModel | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent database' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentDatabase_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Database --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentDatabase_12345_XYZ'
        }

        It 'should return properties of existing master database' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'master'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Database --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be 'master'
            $result.actualState.isSystemObject | Should -Be $true
            $result.actualState._exist | Should -Not -Be $false
            $result.actualState.collation | Should -Not -BeNullOrEmpty
            $result.actualState.recoveryModel | Should -Not -BeNullOrEmpty
        }

        It 'should return read-only metadata properties' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'tempdb'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Database --input $inputJson | ConvertFrom-Json
            $result.actualState.id | Should -BeGreaterThan 0
            $result.actualState.createDate | Should -Not -BeNullOrEmpty
            $result.actualState.size | Should -BeGreaterThan 0
            $result.actualState.isAccessible | Should -Be $true
        }
    }

    Context 'Set Operation - Create Database' -Tag 'Set' {
        It 'should create a new database with default settings' {
            $dbName = "$($script:testDbPrefix)Create1"
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Database --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $dbName
            $getResult.actualState._exist | Should -Not -Be $false
            $getResult.actualState.isSystemObject | Should -Be $false

            # Cleanup
            $deleteJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/Database --input $deleteJson | Out-Null
        }

        It 'should create database with specified recovery model' {
            $dbName = "$($script:testDbPrefix)Create2"
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Simple'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify recovery model
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Database --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.recoveryModel | Should -Be 'Simple'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input $verifyJson | Out-Null
        }

        It 'should update existing database properties' {
            $dbName = "$($script:testDbPrefix)Update1"

            # Create initial database
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Full'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $createJson | Out-Null

            # Update the database
            $updateJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Simple'
                autoShrink     = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Database --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.recoveryModel | Should -Be 'Simple'
            $getResult.actualState.autoShrink | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input $verifyJson | Out-Null
        }
    }

    Context 'Set Operation - ANSI Options' -Tag 'Set', 'AnsiOptions' {
        It 'should set ANSI options on database' {
            $dbName = "$($script:testDbPrefix)Ansi1"

            $inputJson = @{
                serverInstance         = $script:sqlServerInstance
                name                   = $dbName
                ansiNullDefault        = $true
                ansiNullsEnabled       = $true
                ansiPaddingEnabled     = $true
                quotedIdentifiersEnabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify ANSI settings
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Database --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.ansiNullDefault | Should -Be $true
            $getResult.actualState.ansiNullsEnabled | Should -Be $true
            $getResult.actualState.ansiPaddingEnabled | Should -Be $true
            $getResult.actualState.quotedIdentifiersEnabled | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input $verifyJson | Out-Null
        }
    }

    Context 'Test Operation' -Tag 'Test' {
        It 'should return inDesiredState=false for non-existent database when _exist=true' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentDatabase_Test123'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Database --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return inDesiredState=true for existing database matching desired state' {
            $dbName = "$($script:testDbPrefix)TestMatch1"

            # Create database
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Simple'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $createJson | Out-Null

            # Test with matching desired state
            $testJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Simple'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Database --input $testJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input (@{
                    serverInstance = $script:sqlServerInstance
                    name           = $dbName
                } | ConvertTo-Json -Compress) | Out-Null
        }

        It 'should return inDesiredState=false for database with different properties' {
            $dbName = "$($script:testDbPrefix)TestDiff1"

            # Create database with Simple recovery model
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Simple'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $createJson | Out-Null

            # Test expecting Full recovery model
            $testJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
                recoveryModel  = 'Full'
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Database --input $testJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input (@{
                    serverInstance = $script:sqlServerInstance
                    name           = $dbName
                } | ConvertTo-Json -Compress) | Out-Null
        }

        It 'should return inDesiredState=true when database should not exist and does not exist' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentDatabase_TestNotExist'
                _exist         = $false
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.SqlServer/Database --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete database' {
            $dbName = "$($script:testDbPrefix)Delete1"

            # Create a database to delete
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $createJson | Out-Null

            # Delete the database
            $deleteJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/Database --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $verifyJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/Database --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent database gracefully' {
            $inputJson = @{
                serverInstance = $script:sqlServerInstance
                name           = 'NonExistentDatabase_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/Database --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        It 'should export all user databases' {
            $dbName = "$($script:testDbPrefix)Export1"

            # Create a test database
            $createJson = @{
                serverInstance = $script:sqlServerInstance
                name           = $dbName
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Database --input $createJson | Out-Null

            # Export databases
            $result = dsc resource export -r OpenDsc.SqlServer/Database | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            # Verify our test database is in the export (system databases should be excluded)
            $testDbExport = $result.resources | Where-Object { $_.properties.name -eq $dbName }
            $testDbExport | Should -Not -BeNullOrEmpty

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/Database --input (@{
                    serverInstance = $script:sqlServerInstance
                    name           = $dbName
                } | ConvertTo-Json -Compress) | Out-Null
        }
    }
}
