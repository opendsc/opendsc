if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Scheduled Task Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/ScheduledTask | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/ScheduledTask'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/ScheduledTask | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent task' {
            $inputJson = @{
                taskName = 'NonExistentTask_12345_XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.taskName | Should -Be 'NonExistentTask_12345_XYZ'
        }

        It 'should read properties of existing task' -Skip:(!$script:isAdmin) {
            $taskName = 'TestTask_Get_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $createJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT14:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'powershell.exe'
                        arguments = '-NoProfile -Command "Write-Host Test"'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $createJson | Out-Null

            $getJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $getJson | ConvertFrom-Json
            $result.actualState.taskName | Should -Be $taskName
            $result.actualState.actions[0].path | Should -Be 'powershell.exe'
            $result.actualState.actions[0].arguments | Should -BeLike '*Write-Host Test*'
            $result.actualState.triggers[0].daily | Should -Not -BeNullOrEmpty
            $result.actualState.triggers[0].daily.daysInterval | Should -Be 1

            schtasks /delete /tn $taskName /f 2>&1 | Out-Null
        }
    }

    Context 'Set Operation' -Skip:(!$script:isAdmin) -Tag 'Set' {
        AfterEach {
            if ($taskName) {
                schtasks /delete /tn $taskName /f 2>&1 | Out-Null
                $taskName = $null
            }
        }

        It 'should create a new scheduled task with daily trigger' {
            $taskName = 'TestTask_Create_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT09:30:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
                description = 'Test task created by DSC'
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.actions[0].path | Should -Be 'notepad.exe'
            $getResult.actualState.triggers[0].daily | Should -Not -BeNullOrEmpty
        }

        It 'should create task with weekly trigger' {
            $taskName = 'TestTask_weekly_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        weekly = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT10:00:00')
                            daysOfWeek = @('Monday', 'Wednesday', 'Friday')
                            weeksInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'cmd.exe'
                        arguments = '/c echo test'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggers[0].weekly | Should -Not -BeNullOrEmpty
            $getResult.actualState.triggers[0].weekly.daysOfWeek | Should -Be @('Monday', 'Wednesday', 'Friday')
        }

        It 'should create task with boot trigger' {
            $taskName = 'TestTask_Startup_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        boot = @{
                            enabled = $true
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'powershell.exe'
                        arguments = '-Command "Write-Host Startup"'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggers[0].boot | Should -Not -BeNullOrEmpty
        }

        It 'should create task with multiple triggers' {
            $taskName = 'TestTask_MultiTrigger_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT08:00:00')
                            daysInterval = 1
                        }
                    }
                    @{
                        boot = @{
                            enabled = $true
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggers.Count | Should -Be 2
            $getResult.actualState.triggers[0].daily | Should -Not -BeNullOrEmpty
            $getResult.actualState.triggers[1].boot | Should -Not -BeNullOrEmpty
        }

        It 'should create task with multiple actions' {
            $taskName = 'TestTask_MultiAction_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT08:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'cmd.exe'
                        arguments = '/c echo First'
                    }
                    @{
                        path = 'cmd.exe'
                        arguments = '/c echo Second'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.actions.Count | Should -Be 2
            $getResult.actualState.actions[0].arguments | Should -BeLike '*First*'
            $getResult.actualState.actions[1].arguments | Should -BeLike '*Second*'
        }

        It 'should create task in custom folder path' {
            $taskName = 'TestTask_CustomPath_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                taskPath = '\OpenDsc\Tests\'
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT12:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
                taskPath = '\OpenDsc\Tests\'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.taskPath | Should -Be '\OpenDsc\Tests\'

            schtasks /delete /tn "\OpenDsc\Tests\$taskName" /f 2>&1 | Out-Null
            $taskName = $null
        }

        It 'should update existing task' {
            $taskName = 'TestTask_Update_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $createJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT08:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $createJson | Out-Null

            $updateJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT09:00:00')
                            daysInterval = 2
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'cmd.exe'
                        arguments = '/c dir'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $updateJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.actions[0].path | Should -Be 'cmd.exe'
            $getResult.actualState.triggers[0].daily.daysInterval | Should -Be 2
        }

        It 'should create task with trigger repetition' {
            $taskName = 'TestTask_Repetition_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT08:00:00')
                            daysInterval = 1
                            repetitionInterval = '01:00:00'
                            repetitionDuration = '08:00:00'
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggers[0].daily.repetitionInterval | Should -Be '01:00:00'
            $getResult.actualState.triggers[0].daily.repetitionDuration | Should -Be '08:00:00'
        }

        It 'should create task with per-trigger enabled flag' {
            $taskName = 'TestTask_TriggerEnabled_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT08:00:00')
                            daysInterval = 1
                            enabled = $false
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
                enabled = $true
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.enabled | Should -Be $true
            $getResult.actualState.triggers[0].daily.enabled | Should -Be $false
        }
    }

    Context 'Delete Operation' -Skip:(!$script:isAdmin) -Tag 'Delete' {
        It 'should delete scheduled task' {
            $taskName = 'TestTask_Delete_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $createJson = @{
                taskName = $taskName
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT10:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $createJson | Out-Null

            $deleteJson = @{
                taskName = $taskName
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/ScheduledTask --input $deleteJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should handle deleting non-existent task' {
            $inputJson = @{
                taskName = 'NonExistentTask_ToDelete_12345'
                _exist = $false
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Windows/ScheduledTask --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Skip:(!$script:isAdmin) -Tag 'Export' {
        It 'should export scheduled tasks' {
            $result = dsc resource export -r OpenDsc.Windows/ScheduledTask | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            $firstTask = $result.resources[0].properties
            $firstTask.taskName | Should -Not -BeNullOrEmpty
        }

        It 'should export tasks with triggers and actions arrays' {
            $result = dsc resource export -r OpenDsc.Windows/ScheduledTask | ConvertFrom-Json

            $tasksWithTriggers = $result.resources | Where-Object { $_.properties.triggers -ne $null }
            $tasksWithTriggers.Count | Should -BeGreaterThan 0

            $tasksWithActions = $result.resources | Where-Object { $_.properties.actions -ne $null }
            $tasksWithActions.Count | Should -BeGreaterThan 0
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should validate task name pattern (no invalid characters)' {
            $invalidInput = @{
                taskName = 'Invalid:Task*Name'
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT10:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid task names' {
            $validInput = @{
                taskName = 'Valid-Task_Name123'
                triggers = @(
                    @{
                        daily = @{
                            startBoundary = (Get-Date).AddDays(1).ToString('yyyy-MM-ddT10:00:00')
                            daysInterval = 1
                        }
                    }
                )
                actions = @(
                    @{
                        path = 'notepad.exe'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            if ($script:isAdmin) {
                dsc resource set -r OpenDsc.Windows/ScheduledTask --input $validInput | Out-Null
                $LASTEXITCODE | Should -Be 0

                schtasks /delete /tn 'Valid-Task_Name123' /f 2>&1 | Out-Null
            }
        }
    }
}
