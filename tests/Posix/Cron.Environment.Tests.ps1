if (!$IsWindows) {
    $script:isAdmin = (id -u) -eq 0
}

Describe 'POSIX Cron Environment Resource' -Tag 'Posix', 'Linux', 'macOS' -Skip:($IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list 'OpenDsc.Posix.Cron/Environment' | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Posix.Cron/Environment'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list 'OpenDsc.Posix.Cron/Environment' | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Get Operation - Non-Elevated' -Tag 'Get' {
        It 'should return _exist=false when no environment variables are set' {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -

            $inputJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should read environment variables from user crontab' {
            $existingCrontab = crontab -l 2>/dev/null | Out-String

            $envSection = @"
# OpenDsc Environment - Start
SHELL=/bin/bash
PATH=/usr/local/bin:/usr/bin:/bin
MAILTO=admin@example.com
# OpenDsc Environment - End

"@
            $newCrontab = $envSection + $existingCrontab
            $newCrontab | crontab -

            $inputJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $true
            $result.actualState.variables.SHELL | Should -Be '/bin/bash'
            $result.actualState.variables.PATH | Should -Be '/usr/local/bin:/usr/bin:/bin'
            $result.actualState.variables.MAILTO | Should -Be 'admin@example.com'

            $existingCrontab | crontab -
        }
    }

    Context 'Set Operation - User Scope' -Tag 'Set' {
        AfterEach {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -
        }

        It 'should create environment variables in user crontab' {
            $inputJson = @{
                _scope = 'User'
                variables = @{
                    SHELL = '/bin/bash'
                    PATH = '/usr/local/bin:/usr/bin'
                    MAILTO = 'test@example.com'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $verifyJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $true
            $getResult.actualState.variables.SHELL | Should -Be '/bin/bash'
            $getResult.actualState.variables.PATH | Should -Be '/usr/local/bin:/usr/bin'
            $getResult.actualState.variables.MAILTO | Should -Be 'test@example.com'
        }

        It 'should update existing environment variables' {
            $inputJson = @{
                _scope = 'User'
                variables = @{
                    PATH = '/bin:/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $updateJson = @{
                _scope = 'User'
                variables = @{
                    PATH = '/usr/local/bin:/usr/bin:/bin'
                    SHELL = '/bin/zsh'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $updateJson | Out-Null

            $verifyJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.variables.PATH | Should -Be '/usr/local/bin:/usr/bin:/bin'
            $getResult.actualState.variables.SHELL | Should -Be '/bin/zsh'
        }

        It 'should preserve order of environment variables' {
            $inputJson = @{
                _scope = 'User'
                variables = @{
                    SHELL = '/bin/bash'
                    PATH = '/usr/local/bin:/usr/bin'
                    HOME = '/home/user'
                    MAILTO = 'admin@example.com'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $crontabContent = crontab -l
            $envLines = $crontabContent -split "`n" | Where-Object { $_ -match '=' -and $_ -notmatch '^#' }

            $envLines.Count | Should -BeGreaterOrEqual 4
        }
    }

    Context 'Set Operation - System Scope' -Skip:(!$script:isAdmin) -Tag 'Set', 'Admin' {
        AfterEach {
            $cronFile = '/etc/cron.d/opendsc-test-env'
            if (Test-Path $cronFile) {
                Remove-Item $cronFile -Force
            }
        }

        It 'should create environment variables in system crontab' {
            $inputJson = @{
                _scope = 'System'
                fileName = 'opendsc-test-env'
                variables = @{
                    PATH = '/usr/local/sbin:/usr/sbin:/sbin'
                    MAILTO = 'root@localhost'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $verifyJson = @{
                _scope = 'System'
                fileName = 'opendsc-test-env'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $true
            $getResult.actualState.variables.PATH | Should -Be '/usr/local/sbin:/usr/sbin:/sbin'
            $getResult.actualState.variables.MAILTO | Should -Be 'root@localhost'
        }

        It 'should require fileName when _scope is System' {
            $inputJson = @{
                _scope = 'System'
                variables = @{
                    PATH = '/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete environment variables from user crontab' {
            $inputJson = @{
                _scope = 'User'
                variables = @{
                    SHELL = '/bin/bash'
                    PATH = '/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $deleteJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            dsc resource delete -r 'OpenDsc.Posix.Cron/Environment' --input $deleteJson | Out-Null

            $verifyJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting when no environment exists' {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -

            $inputJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            { dsc resource delete -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export' {
        BeforeAll {
            $inputJson = @{
                _scope = 'User'
                variables = @{
                    SHELL = '/bin/bash'
                    PATH = '/usr/local/bin:/usr/bin'
                }
            } | ConvertTo-Json -Compress
            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null
        }

        AfterAll {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -
        }

        It 'should export user environment variables' {
            $result = dsc resource export -r 'OpenDsc.Posix.Cron/Environment' | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty

            $userEnv = $result.resources | Where-Object { $_.properties._scope -eq 'User' -or $null -eq $_.properties._scope }
            $userEnv | Should -Not -BeNullOrEmpty
            $userEnv.properties.variables.SHELL | Should -Be '/bin/bash'
            $userEnv.properties.variables.PATH | Should -Be '/usr/local/bin:/usr/bin'
        }
    }

    Context 'Integration with Job Resource' -Tag 'Integration' {
        AfterAll {
            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc' } | crontab -
        }

        It 'should allow jobs to coexist with environment variables' {
            $envJson = @{
                _scope = 'User'
                variables = @{
                    SHELL = '/bin/bash'
                    PATH = '/usr/local/bin:/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $envJson | Out-Null

            $jobJson = @{
                name = 'IntegrationTestJob'
                schedule = '0 2 * * *'
                command = '/usr/bin/test.sh'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $jobJson | Out-Null

            $crontabContent = crontab -l

            $crontabContent | Should -Match 'OpenDsc Environment'
            $crontabContent | Should -Match 'SHELL=/bin/bash'
            $crontabContent | Should -Match 'OpenDsc Job: IntegrationTestJob'
            $crontabContent | Should -Match '0 2 \* \* \* /usr/bin/test.sh'
        }

        It 'should preserve jobs when updating environment' {
            $envJson = @{
                _scope = 'User'
                variables = @{
                    PATH = '/bin:/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $envJson | Out-Null

            $jobJson = @{
                name = 'PreserveTestJob'
                schedule = '@daily'
                command = '/bin/daily.sh'
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Job' --input $jobJson | Out-Null

            $updateEnvJson = @{
                _scope = 'User'
                variables = @{
                    PATH = '/usr/local/bin:/usr/bin'
                    SHELL = '/bin/zsh'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $updateEnvJson | Out-Null

            $verifyJobJson = @{
                name = 'PreserveTestJob'
            } | ConvertTo-Json -Compress

            $getJobResult = dsc resource get -r 'OpenDsc.Posix.Cron/Job' --input $verifyJobJson | ConvertFrom-Json
            $getJobResult.actualState._exist | Should -Be $true
            $getJobResult.actualState.schedule | Should -Be '@daily'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject empty variables dictionary' {
            $invalidInput = @{
                _scope = 'User'
                variables = @{}
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept common cron environment variables' {
            $validVars = @{
                SHELL = '/bin/bash'
                PATH = '/usr/local/bin:/usr/bin:/bin'
                MAILTO = 'admin@example.com'
                HOME = '/home/user'
                CRON_TZ = 'America/New_York'
            }

            $inputJson = @{
                _scope = 'User'
                variables = $validVars
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -
        }
    }

    Context 'Scope Handling' {
        It 'should default to user scope when not specified' {
            $inputJson = @{
                variables = @{
                    PATH = '/usr/bin'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r 'OpenDsc.Posix.Cron/Environment' --input $inputJson | Out-Null

            $verifyJson = @{
                _scope = 'User'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r 'OpenDsc.Posix.Cron/Environment' --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $true

            crontab -l 2>/dev/null | Where-Object { $_ -notmatch 'OpenDsc Environment' } | crontab -
        }
    }
}
