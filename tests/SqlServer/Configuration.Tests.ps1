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

Describe 'SQL Server Configuration Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/Configuration'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            # Configuration is a singleton - no delete or export
            $result.capabilities | Should -Not -Contain 'delete'
            $result.capabilities | Should -Not -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
        }

        It 'should have memory configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.maxServerMemory | Should -Not -BeNullOrEmpty
            $result.properties.minServerMemory | Should -Not -BeNullOrEmpty
            $result.properties.minMemoryPerQuery | Should -Not -BeNullOrEmpty
        }

        It 'should have parallelism configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.maxDegreeOfParallelism | Should -Not -BeNullOrEmpty
            $result.properties.costThresholdForParallelism | Should -Not -BeNullOrEmpty
        }

        It 'should have feature toggle properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.xpCmdShellEnabled | Should -Not -BeNullOrEmpty
            $result.properties.databaseMailEnabled | Should -Not -BeNullOrEmpty
            $result.properties.clrEnabled | Should -Not -BeNullOrEmpty
            $result.properties.agentXpsEnabled | Should -Not -BeNullOrEmpty
        }

        It 'should have backup configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.defaultBackupCompression | Should -Not -BeNullOrEmpty
            $result.properties.defaultBackupChecksum | Should -Not -BeNullOrEmpty
        }

        It 'should have network configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.networkPacketSize | Should -Not -BeNullOrEmpty
            $result.properties.remoteLoginTimeout | Should -Not -BeNullOrEmpty
            $result.properties.remoteQueryTimeout | Should -Not -BeNullOrEmpty
        }

        It 'should have trigger configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.nestedTriggers | Should -Not -BeNullOrEmpty
            $result.properties.serverTriggerRecursionEnabled | Should -Not -BeNullOrEmpty
            $result.properties.disallowResultsFromTriggers | Should -Not -BeNullOrEmpty
        }

        It 'should have security configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.crossDbOwnershipChaining | Should -Not -BeNullOrEmpty
            $result.properties.remoteDacConnectionsEnabled | Should -Not -BeNullOrEmpty
        }

        It 'should have misc configuration properties' {
            $result = dsc resource schema -r OpenDsc.SqlServer/Configuration | ConvertFrom-Json
            $result.properties.showAdvancedOptions | Should -Not -BeNullOrEmpty
            $result.properties.fillFactor | Should -Not -BeNullOrEmpty
            $result.properties.recoveryInterval | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return current server configuration' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $result.actualState | Should -Not -BeNullOrEmpty
            $result.actualState.serverInstance | Should -Not -BeNullOrEmpty
        }

        It 'should return memory configuration values' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json

            # MaxServerMemory should have a value (default is 2147483647 for unlimited)
            $result.actualState.maxServerMemory | Should -Not -BeNullOrEmpty
            $result.actualState.maxServerMemory | Should -BeOfType [long]

            # MinServerMemory should have a value
            $result.actualState.minServerMemory | Should -Not -BeNullOrEmpty
        }

        It 'should return parallelism configuration values' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json

            # MAXDOP has a value (0 = use all CPUs)
            $result.actualState.maxDegreeOfParallelism | Should -Not -BeNullOrEmpty
            $result.actualState.maxDegreeOfParallelism | Should -BeGreaterOrEqual 0

            # Cost threshold (default is 5)
            $result.actualState.costThresholdForParallelism | Should -Not -BeNullOrEmpty
        }

        It 'should return feature toggle values as booleans' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json

            $result.actualState.xpCmdShellEnabled | Should -BeOfType [bool]
            $result.actualState.databaseMailEnabled | Should -BeOfType [bool]
            $result.actualState.clrEnabled | Should -BeOfType [bool]
        }

        It 'should return backup configuration values' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json

            $result.actualState.defaultBackupCompression | Should -BeOfType [bool]
            $result.actualState.defaultBackupChecksum | Should -BeOfType [bool]
        }

        It 'should return show advanced options running value' {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json

            $result.actualState.showAdvancedOptionsRunValue | Should -BeOfType [bool]
        }
    }

    Context 'Set Operation - MaxDegreeOfParallelism' -Tag 'Set', 'Parallelism' {
        BeforeAll {
            # Get current MAXDOP to restore later
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalMaxDop = $result.actualState.maxDegreeOfParallelism
        }

        AfterAll {
            # Restore original MAXDOP
            if ($null -ne $script:originalMaxDop)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    maxDegreeOfParallelism = $script:originalMaxDop
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should set maxDegreeOfParallelism to 1' {
            $inputJson = Get-SqlServerTestInput @{
                maxDegreeOfParallelism = 1
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.maxDegreeOfParallelism | Should -Be 1
        }

        It 'should set maxDegreeOfParallelism to 0 (use all CPUs)' {
            $inputJson = Get-SqlServerTestInput @{
                maxDegreeOfParallelism = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.maxDegreeOfParallelism | Should -Be 0
        }
    }

    Context 'Set Operation - CostThresholdForParallelism' -Tag 'Set', 'Parallelism' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalCostThreshold = $result.actualState.costThresholdForParallelism
        }

        AfterAll {
            if ($null -ne $script:originalCostThreshold)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    costThresholdForParallelism = $script:originalCostThreshold
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should set costThresholdForParallelism' {
            $inputJson = Get-SqlServerTestInput @{
                costThresholdForParallelism = 50
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.costThresholdForParallelism | Should -Be 50
        }
    }

    Context 'Set Operation - Backup Configuration' -Tag 'Set', 'Backup' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalBackupCompression = $result.actualState.defaultBackupCompression
            $script:originalBackupChecksum = $result.actualState.defaultBackupChecksum
        }

        AfterAll {
            if ($null -ne $script:originalBackupCompression)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    defaultBackupCompression = $script:originalBackupCompression
                    defaultBackupChecksum    = $script:originalBackupChecksum
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should enable default backup compression' {
            $inputJson = Get-SqlServerTestInput @{
                defaultBackupCompression = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.defaultBackupCompression | Should -Be $true
        }

        It 'should disable default backup compression' {
            $inputJson = Get-SqlServerTestInput @{
                defaultBackupCompression = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.defaultBackupCompression | Should -Be $false
        }

        It 'should enable default backup checksum' {
            $inputJson = Get-SqlServerTestInput @{
                defaultBackupChecksum = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.defaultBackupChecksum | Should -Be $true
        }
    }

    Context 'Set Operation - OptimizeAdhocWorkloads' -Tag 'Set', 'Query' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalOptimizeAdhoc = $result.actualState.optimizeAdhocWorkloads
        }

        AfterAll {
            if ($null -ne $script:originalOptimizeAdhoc)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    optimizeAdhocWorkloads = $script:originalOptimizeAdhoc
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should enable optimize for ad hoc workloads' {
            $inputJson = Get-SqlServerTestInput @{
                optimizeAdhocWorkloads = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.optimizeAdhocWorkloads | Should -Be $true
        }

        It 'should disable optimize for ad hoc workloads' {
            $inputJson = Get-SqlServerTestInput @{
                optimizeAdhocWorkloads = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.optimizeAdhocWorkloads | Should -Be $false
        }
    }

    Context 'Set Operation - RemoteDacConnectionsEnabled' -Tag 'Set', 'Security' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalRemoteDac = $result.actualState.remoteDacConnectionsEnabled
        }

        AfterAll {
            if ($null -ne $script:originalRemoteDac)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    remoteDacConnectionsEnabled = $script:originalRemoteDac
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should enable remote DAC connections' {
            $inputJson = Get-SqlServerTestInput @{
                remoteDacConnectionsEnabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.remoteDacConnectionsEnabled | Should -Be $true
        }

        It 'should disable remote DAC connections' {
            $inputJson = Get-SqlServerTestInput @{
                remoteDacConnectionsEnabled = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.remoteDacConnectionsEnabled | Should -Be $false
        }
    }

    Context 'Set Operation - Multiple Properties' -Tag 'Set', 'MultipleProperties' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalMaxDop = $result.actualState.maxDegreeOfParallelism
            $script:originalCostThreshold = $result.actualState.costThresholdForParallelism
            $script:originalOptimize = $result.actualState.optimizeAdhocWorkloads
        }

        AfterAll {
            if ($null -ne $script:originalMaxDop)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    maxDegreeOfParallelism      = $script:originalMaxDop
                    costThresholdForParallelism = $script:originalCostThreshold
                    optimizeAdhocWorkloads      = $script:originalOptimize
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should set multiple configuration properties at once' {
            $inputJson = Get-SqlServerTestInput @{
                maxDegreeOfParallelism      = 2
                costThresholdForParallelism = 25
                optimizeAdhocWorkloads      = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all properties
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.maxDegreeOfParallelism | Should -Be 2
            $result.actualState.costThresholdForParallelism | Should -Be 25
            $result.actualState.optimizeAdhocWorkloads | Should -Be $true
        }
    }

    Context 'Set Operation - Trigger Configuration' -Tag 'Set', 'Triggers' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalNestedTriggers = $result.actualState.nestedTriggers
            $script:originalDisallowResults = $result.actualState.disallowResultsFromTriggers
        }

        AfterAll {
            if ($null -ne $script:originalNestedTriggers)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    nestedTriggers              = $script:originalNestedTriggers
                    disallowResultsFromTriggers = $script:originalDisallowResults
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should configure nested triggers' {
            $inputJson = Get-SqlServerTestInput @{
                nestedTriggers = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.nestedTriggers | Should -Be $true
        }

        It 'should configure disallow results from triggers' {
            $inputJson = Get-SqlServerTestInput @{
                disallowResultsFromTriggers = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.disallowResultsFromTriggers | Should -Be $true
        }
    }

    Context 'Set Operation - Network Configuration' -Tag 'Set', 'Network' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalRemoteQueryTimeout = $result.actualState.remoteQueryTimeout
        }

        AfterAll {
            if ($null -ne $script:originalRemoteQueryTimeout)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    remoteQueryTimeout = $script:originalRemoteQueryTimeout
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should set remote query timeout' {
            $inputJson = Get-SqlServerTestInput @{
                remoteQueryTimeout = 300
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.remoteQueryTimeout | Should -Be 300
        }

        It 'should disable remote query timeout with 0' {
            $inputJson = Get-SqlServerTestInput @{
                remoteQueryTimeout = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.remoteQueryTimeout | Should -Be 0
        }
    }

    Context 'Set Operation - BlockedProcessThreshold' -Tag 'Set', 'Diagnostics' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalBlockedProcess = $result.actualState.blockedProcessThreshold
        }

        AfterAll {
            if ($null -ne $script:originalBlockedProcess)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    blockedProcessThreshold = $script:originalBlockedProcess
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should enable blocked process threshold' {
            $inputJson = Get-SqlServerTestInput @{
                blockedProcessThreshold = 10
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.blockedProcessThreshold | Should -Be 10
        }

        It 'should disable blocked process threshold with 0' {
            $inputJson = Get-SqlServerTestInput @{
                blockedProcessThreshold = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.blockedProcessThreshold | Should -Be 0
        }
    }

    Context 'Idempotency' -Tag 'Idempotency' {
        BeforeAll {
            $inputJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $inputJson |
            ConvertFrom-Json
            $script:originalMaxDop = $result.actualState.maxDegreeOfParallelism
        }

        AfterAll {
            if ($null -ne $script:originalMaxDop)
            {
                $restoreJson = Get-SqlServerTestInput @{
                    maxDegreeOfParallelism = $script:originalMaxDop
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.SqlServer/Configuration --input $restoreJson | Out-Null
            }
        }

        It 'should be idempotent when setting same value twice' {
            # Set initial value
            $inputJson = Get-SqlServerTestInput @{
                maxDegreeOfParallelism = 4
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Set same value again
            dsc resource set -r OpenDsc.SqlServer/Configuration --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify still correct
            $verifyJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.SqlServer/Configuration --input $verifyJson |
            ConvertFrom-Json
            $result.actualState.maxDegreeOfParallelism | Should -Be 4
        }
    }
}
