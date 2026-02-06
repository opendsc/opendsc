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

Describe 'SQL Server Agent Job Resource' -Tag 'SqlServer' -Skip:(!$script:sqlServerAvailable) {
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

        $script:testJobPrefix = 'OpenDscTestJob_'
    }

    AfterAll {
        # Cleanup any test jobs that may have been left behind
        if ($script:sqlServerAvailable)
        {
            try
            {
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = Get-SqlServerConnectionString
                $conn.Open()

                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "SELECT name FROM msdb.dbo.sysjobs WHERE name LIKE '$($script:testJobPrefix)%'"
                $reader = $cmd.ExecuteReader()

                $jobsToDelete = @()
                while ($reader.Read())
                {
                    $jobsToDelete += $reader.GetString(0)
                }
                $reader.Close()

                foreach ($job in $jobsToDelete)
                {
                    $dropCmd = $conn.CreateCommand()
                    $dropCmd.CommandText = "EXEC msdb.dbo.sp_delete_job @job_name = N'$job'"
                    try { $dropCmd.ExecuteNonQuery() | Out-Null } catch { }
                }

                $conn.Close()
            }
            catch
            {
                Write-Warning "Failed to cleanup test jobs: $_"
            }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.SqlServer/AgentJob | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.SqlServer/AgentJob'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.SqlServer/AgentJob | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should return valid JSON schema' {
            $result = dsc resource schema -r OpenDsc.SqlServer/AgentJob | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $result.properties.serverInstance | Should -Not -BeNullOrEmpty
            $result.properties.name | Should -Not -BeNullOrEmpty
            $result.properties.isEnabled | Should -Not -BeNullOrEmpty
            $result.properties.description | Should -Not -BeNullOrEmpty
        }

        It 'should have completion action enums for notification levels' {
            $result = dsc resource schema -r OpenDsc.SqlServer/AgentJob | ConvertFrom-Json
            $result.properties.emailLevel | Should -Not -BeNullOrEmpty
            $result.properties.pageLevel | Should -Not -BeNullOrEmpty
            $result.properties.netSendLevel | Should -Not -BeNullOrEmpty
            $result.properties.eventLogLevel | Should -Not -BeNullOrEmpty
            $result.properties.deleteLevel | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent job' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentJob_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentJob_12345_XYZ'
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should create a new agent job' {
            $jobName = "$($script:testJobPrefix)Create1"
            $inputJson = Get-SqlServerTestInput @{
                name        = $jobName
                description = 'Test job created by OpenDsc'
                isEnabled   = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify it was created
            $verifyJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.name | Should -Be $jobName
            $getResult.actualState.description | Should -Be 'Test job created by OpenDsc'
            $getResult.actualState.isEnabled | Should -Be $false
            $getResult.actualState._exist | Should -Not -Be $false

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $deleteJson | Out-Null
        }

        It 'should update existing agent job properties' {
            $jobName = "$($script:testJobPrefix)Update1"

            # Create initial job
            $createJson = Get-SqlServerTestInput @{
                name        = $jobName
                description = 'Initial description'
                isEnabled   = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $createJson | Out-Null

            # Update the job
            $updateJson = Get-SqlServerTestInput @{
                name        = $jobName
                description = 'Updated description'
                isEnabled   = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $updateJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify update
            $verifyJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.description | Should -Be 'Updated description'
            $getResult.actualState.isEnabled | Should -Be $true

            # Cleanup
            $deleteJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $deleteJson | Out-Null
        }

        It 'should set event log level' {
            $jobName = "$($script:testJobPrefix)EventLog1"

            $inputJson = Get-SqlServerTestInput @{
                name          = $jobName
                description   = 'Job with event log level'
                isEnabled     = $false
                eventLogLevel = 'Always'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.eventLogLevel | Should -Be 'Always'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $verifyJson | Out-Null
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete agent job' {
            $jobName = "$($script:testJobPrefix)Delete1"

            # Create a job to delete
            $createJson = Get-SqlServerTestInput @{
                name        = $jobName
                description = 'Job to delete'
                isEnabled   = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $createJson | Out-Null

            # Delete the job
            $deleteJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $deleteJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify deletion
            $verifyJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent job gracefully' {
            $inputJson = Get-SqlServerTestInput @{
                name = 'NonExistentJob_ToDelete_XYZ'
            } | ConvertTo-Json -Compress

            # Should not throw error
            { dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        BeforeAll {
            # Create a test job for export
            $script:exportTestJobName = "$($script:testJobPrefix)Export1"

            $createJson = Get-SqlServerTestInput @{
                name        = $script:exportTestJobName
                description = 'Job for export test'
                isEnabled   = $false
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $createJson | Out-Null
        }

        AfterAll {
            # Cleanup export test job
            $deleteJson = Get-SqlServerTestInput @{
                name = $script:exportTestJobName
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $deleteJson | Out-Null
        }

        It 'should export all agent jobs' {
            $filterJson = Get-SqlServerTestInput @{} | ConvertTo-Json -Compress
            $result = dsc resource export -r OpenDsc.SqlServer/AgentJob --input $filterJson | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            # Find our test job
            $testJob = $result.resources | Where-Object { $_.properties.name -eq $script:exportTestJobName }
            $testJob | Should -Not -BeNullOrEmpty
            $testJob.properties.description | Should -Be 'Job for export test'
        }
    }

    Context 'Category Assignment' -Tag 'Category' {
        It 'should assign category to job' {
            $jobName = "$($script:testJobPrefix)Category1"

            # Use a default category that exists
            $inputJson = Get-SqlServerTestInput @{
                name        = $jobName
                description = 'Job with category'
                isEnabled   = $false
                category    = '[Uncategorized (Local)]'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.SqlServer/AgentJob --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = Get-SqlServerTestInput @{
                name = $jobName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.SqlServer/AgentJob --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.category | Should -Be '[Uncategorized (Local)]'

            # Cleanup
            dsc resource delete -r OpenDsc.SqlServer/AgentJob --input $verifyJson | Out-Null
        }
    }
}
