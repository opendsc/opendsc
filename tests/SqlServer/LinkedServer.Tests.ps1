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

Describe 'SQL Server Linked Server Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testLinkedServerPrefix = 'OpenDscTestLS_'
    }

    AfterAll {
        # Cleanup any test linked servers that may have been left behind
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = @"
SELECT name FROM sys.servers
WHERE is_linked = 1 AND name LIKE '$($script:testLinkedServerPrefix)%'
"@
                $reader = $cmd.ExecuteReader()

                $linkedServersToDelete = @()
                while ($reader.Read())
                {
                    $linkedServersToDelete += $reader.GetString(0)
                }
                $reader.Close()

                foreach ($ls in $linkedServersToDelete)
                {
                    $dropCmd = $conn.CreateCommand()
                    $dropCmd.CommandText = "EXEC sp_dropserver @server = N'$ls'"
                    try { $dropCmd.ExecuteNonQuery() | Out-Null }
                    catch { }
                }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test linked servers: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/LinkedServer | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/LinkedServer'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/LinkedServer | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/LinkedServer | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.productName | Should -Not -BeNullOrEmpty
            $result.properties.providerName | Should -Not -BeNullOrEmpty
        }

        It 'should have connection properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/LinkedServer | ConvertFrom-Json
            $result.properties.dataSource | Should -Not -BeNullOrEmpty
            $result.properties.catalog | Should -Not -BeNullOrEmpty
            $result.properties.providerString | Should -Not -BeNullOrEmpty
        }

        It 'should have RPC properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/LinkedServer | ConvertFrom-Json
            $result.properties.rpc | Should -Not -BeNullOrEmpty
            $result.properties.rpcOut | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent linked server' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentLinkedServer_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $inputJson |
            ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentLinkedServer_12345_XYZ'
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should create a new linked server to SQL Server' {
            $lsName = "$($script:testLinkedServerPrefix)Create1"
            $inputJson = Get-SqlServerTestInput @{
                name         = $lsName
                productName  = 'SQL Server'
                providerName = 'SQLNCLI11'
                dataSource   = $script:sqlServerInstance
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $verifyJson |
            ConvertFrom-Json
            $getResult.actualState.name | Should -Be $lsName
            $getResult.actualState.productName | Should -Be 'SQL Server'
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $deleteJson | Out-Null
        }

        It 'should update linked server properties' {
            $lsName = "$($script:testLinkedServerPrefix)Update1"

            # Create initial linked server
            $createJson = Get-SqlServerTestInput @{
                name         = $lsName
                productName  = 'SQL Server'
                providerName = 'SQLNCLI11'
                dataSource   = $script:sqlServerInstance
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $createJson | Out-Null

            # Update the linked server
            $updateJson = Get-SqlServerTestInput @{
                name       = $lsName
                rpcOut     = $true
                dataAccess = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $verifyJson |
            ConvertFrom-Json
            $getResult.actualState.rpcOut | Should -Be $true
            $getResult.actualState.dataAccess | Should -Be $true

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $deleteJson | Out-Null
        }

        It 'should set timeout properties' {
            $lsName = "$($script:testLinkedServerPrefix)Timeout1"

            $inputJson = Get-SqlServerTestInput @{
                name           = $lsName
                productName    = 'SQL Server'
                providerName   = 'SQLNCLI11'
                dataSource     = $script:sqlServerInstance
                connectTimeout = 30
                queryTimeout   = 60
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $verifyJson |
            ConvertFrom-Json
            $getResult.actualState.connectTimeout | Should -Be 30
            $getResult.actualState.queryTimeout | Should -Be 60

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $verifyJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete linked server' {
            $lsName = "$($script:testLinkedServerPrefix)Delete1"

            # Create a linked server to delete
            $createJson = Get-SqlServerTestInput @{
                name         = $lsName
                productName  = 'SQL Server'
                providerName = 'SQLNCLI11'
                dataSource   = $script:sqlServerInstance
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $createJson | Out-Null

            # Delete the linked server
            $deleteJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $verifyJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $verifyJson |
            ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent linked server gracefully' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentLinkedServer_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $inputJson } |
            Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        BeforeAll {
            # Create a test linked server for export
            $script:exportTestLsName = "$($script:testLinkedServerPrefix)Export1"

            $createJson = Get-SqlServerTestInput @{
                name         = $script:exportTestLsName
                productName  = 'SQL Server'
                providerName = 'SQLNCLI11'
                dataSource   = $script:sqlServerInstance
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $createJson | Out-Null
        }

        AfterAll {
            # Cleanup export test linked server
            $deleteJson = Get-SqlServerTestInput @{
                name = $script:exportTestLsName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $deleteJson | Out-Null
        }

        It 'should export all linked servers' {
            $filterJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource export -r OpenDsc.SqlServer/LinkedServer --input $filterJson | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty

            # Find our test linked server
            $testLs = $result.resources |
            Where-Object { $_.properties.name -eq $script:exportTestLsName }
            $testLs | Should -Not -BeNullOrEmpty
            $testLs.properties.productName | Should -Be 'SQL Server'
        }
    }

    Context 'RPC Configuration' -Tag 'RPC' {
        It 'should enable RPC and RPC Out' {
            $lsName = "$($script:testLinkedServerPrefix)RPC1"

            $inputJson = Get-SqlServerTestInput @{
                name         = $lsName
                productName  = 'SQL Server'
                providerName = 'SQLNCLI11'
                dataSource   = $script:sqlServerInstance
                rpc          = $true
                rpcOut       = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/LinkedServer --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $lsName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/LinkedServer --input $verifyJson |
            ConvertFrom-Json
            $getResult.actualState.rpc | Should -Be $true
            $getResult.actualState.rpcOut | Should -Be $true

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/LinkedServer --input $verifyJson | Out-Null
        }
    }
}
