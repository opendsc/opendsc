if (!$IsWindows) {
    $script:isAdmin = (id -u) -eq 0
}

Describe 'POSIX Cron Job Resource' -Tag 'Posix', 'Linux', 'macOS' -Skip:($IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list 'OpenDsc.Posix.Cron/Job' | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Posix.Cron/Job'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list 'OpenDsc.Posix.Cron/Job' | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Get Operation - Non-Elevated' -Tag 'Get' {
        It 'should return _exist=false for non-existent job' {
            $inputJson = @{
                name = 'NonExistentJob_12345_XYZ'
                schedule = '0 2 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.name | Should -Be 'NonExistentJob_12345_XYZ'
        }

        It 'should read properties of existing user job' {
            $jobName = 'TestJob_Get'
            crontab -l 2>/dev/null | { process { $_ } } | Out-String | Set-Variable existingCrontab

            $newCrontab = $existingCrontab + "`n# OpenDsc Job: $jobName`n0 3 * * * /usr/bin/echo test`n"
            $newCrontab | crontab -

            $inputJson = @{
                name = $jobName
                schedule = '0 3 * * *'
                command = '/usr/bin/echo test'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | ConvertFrom-Json
            $result.actualState.name | Should -Be $jobName
            $result.actualState.schedule | Should -Be '0 3 * * *'
            $result.actualState.command | Should -Be '/usr/bin/echo test'
            $result.actualState._exist | Should -Be $true

            $existingCrontab | crontab -
        }
    }

    Context 'Set Operation - User Scope' -Tag 'Set' {
        AfterEach {
            $jobName = 'TestJob_Create'
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch "OpenDsc Job: $jobName" } | crontab -
        }

        It 'should create a new user cron job' {
            $inputJson = @{
                name = 'TestJob_Create'
                schedule = '0 2 * * *'
                command = '/usr/bin/echo hello'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'TestJob_Create'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.schedule | Should -Be '0 2 * * *'
            $getResult.actualState.command | Should -Be '/usr/bin/echo hello'
            $getResult.actualState._exist | Should -Be $true
        }

        It 'should update existing user job' {
            $inputJson = @{
                name = 'TestJob_Create'
                schedule = '0 2 * * *'
                command = '/usr/bin/echo original'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $updateJson = @{
                name = 'TestJob_Create'
                schedule = '0 3 * * *'
                command = '/usr/bin/echo updated'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $updateJson | Out-Null

            $verifyJson = @{
                name = 'TestJob_Create'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.schedule | Should -Be '0 3 * * *'
            $getResult.actualState.command | Should -Be '/usr/bin/echo updated'
        }

        It 'should handle jobs with special schedule strings' {
            $inputJson = @{
                name = 'TestJob_Create'
                schedule = '@daily'
                command = '/usr/bin/daily-task.sh'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'TestJob_Create'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.schedule | Should -Be '@daily'
        }

        It 'should handle jobs with comments' {
            $inputJson = @{
                name = 'TestJob_Create'
                schedule = '0 2 * * *'
                command = '/usr/bin/backup.sh'
                comment = 'Daily backup job'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'TestJob_Create'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.comment | Should -Be 'Daily backup job'
        }
    }

    Context 'Set Operation - System Scope' -Skip:(!$script:isAdmin) -Tag 'Set', 'Admin' {
        AfterEach {
            $cronFile = '/etc/cron.d/opendsc-test'
            if (Test-Path $cronFile) {
                Remove-Item $cronFile -Force
            }
        }

        It 'should create system cron job' {
            $inputJson = @{
                name = 'system-test-job'
                _scope = 'System'
                fileName = 'opendsc-test'
                schedule = '0 2 * * *'
                command = '/usr/bin/system-task.sh'
                runAsUser = 'root'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'system-test-job'
                _scope = 'System'
                fileName = 'opendsc-test'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.schedule | Should -Be '0 2 * * *'
            $getResult.actualState.command | Should -Be '/usr/bin/system-task.sh'
            $getResult.actualState.runAsUser | Should -Be 'root'
        }

        It 'should default runAsUser to root for system scope' {
            $inputJson = @{
                name = 'system-default-user'
                _scope = 'System'
                fileName = 'opendsc-test'
                schedule = '0 3 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'system-default-user'
                _scope = 'System'
                fileName = 'opendsc-test'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.runAsUser | Should -Be 'root'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete user cron job' {
            $inputJson = @{
                name = 'TestJob_Delete'
                schedule = '0 2 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $deleteJson = @{
                name = 'TestJob_Delete'
            } | ConvertTo-Json -Compress

            dsc resource delete -r 'OpenDsc.Posix.Cron/Job' --input $deleteJson | Out-Null

            $verifyJson = @{
                name = 'TestJob_Delete'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent job' {
            $inputJson = @{
                name = 'NonExistentJob_ToDelete'
            } | ConvertTo-Json -Compress

            { dsc resource delete -r 'OpenDsc.Posix.Cron/Job' --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        BeforeAll {
            $inputJson = @{
                name = 'TestJob_Export1'
                schedule = '0 2 * * *'
                command = '/usr/bin/task1.sh'
            } | ConvertTo-Json -Compress
            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $inputJson2 = @{
                name = 'TestJob_Export2'
                schedule = '@daily'
                command = '/usr/bin/task2.sh'
            } | ConvertTo-Json -Compress
            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson2 | Out-Null
        }

        AfterAll {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Job: TestJob_Export' } | crontab -
        }

        It 'should export all user cron jobs' {
            $result = dsc resource export -r 'OpenDsc.Posix.Cron/Job' | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty

            $exportedJobs = $result.resources | Where-Object { $_.properties.name -match 'TestJob_Export' }
            $exportedJobs.Count | Should -BeGreaterOrEqual 2

            $job1 = $exportedJobs | Where-Object { $_.properties.name -eq 'TestJob_Export1' }
            $job1 | Should -Not -BeNullOrEmpty
            $job1.properties.schedule | Should -Be '0 2 * * *'
            $job1.properties.command | Should -Be '/usr/bin/task1.sh'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject invalid job name pattern' {
            $invalidInput = @{
                name = 'Invalid Name With Spaces'
                schedule = '0 2 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should reject invalid schedule pattern' {
            $invalidInput = @{
                name = 'InvalidSchedule'
                schedule = 'invalid schedule'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid schedule patterns' {
            $validSchedules = @(
                '0 2 * * *'
                '*/5 * * * *'
                '@daily'
                '@hourly'
                '@reboot'
            )

            foreach ($schedule in $validSchedules) {
                $inputJson = @{
                    name = "ValidSchedule_$(Get-Random)"
                    schedule = $schedule
                    command = '/bin/true'
                } | ConvertTo-Json -Compress

                dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null
                $LASTEXITCODE | Should -Be 0
            }

            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Job: ValidSchedule_' } | crontab -
        }
    }

    Context 'Scope Handling' {
        It 'should default to user scope when not specified' {
            $inputJson = @{
                name = 'DefaultScopeJob'
                schedule = '0 2 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson | Out-Null

            $verifyJson = @{
                name = 'DefaultScopeJob'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $true

            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Job: DefaultScopeJob' } | crontab -
        }

        It 'should require fileName when _scope is System' -Skip:(!$script:isAdmin) {
            $inputJson = @{
                name = 'MissingFileName'
                _scope = 'System'
                schedule = '0 2 * * *'
                command = '/bin/true'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
