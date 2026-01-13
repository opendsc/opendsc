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
                execute = 'powershell.exe'
                arguments = '-NoProfile -Command "Write-Host Test"'
                triggerType = 'Daily'
                startTime = '14:00'
                daysInterval = 1
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $createJson | Out-Null

            $getJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $getJson | ConvertFrom-Json
            $result.actualState.taskName | Should -Be $taskName
            $result.actualState.execute | Should -Be 'powershell.exe'
            $result.actualState.arguments | Should -BeLike '*Write-Host Test*'
            $result.actualState.triggerType | Should -Be 'Daily'
            $result.actualState.daysInterval | Should -Be 1

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
                execute = 'notepad.exe'
                triggerType = 'Daily'
                startTime = '09:30'
                daysInterval = 1
                description = 'Test task created by DSC'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.execute | Should -Be 'notepad.exe'
            $getResult.actualState.triggerType | Should -Be 'Daily'
            $getResult.actualState.startTime | Should -Be '09:30'
        }

        It 'should create task with weekly trigger' {
            $taskName = 'TestTask_Weekly_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                execute = 'cmd.exe'
                arguments = '/c echo test'
                triggerType = 'Weekly'
                startTime = '10:00'
                daysOfWeek = @('Monday', 'Wednesday', 'Friday')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggerType | Should -Be 'Weekly'
            $getResult.actualState.daysOfWeek | Should -Be @('Monday', 'Wednesday', 'Friday')
        }

        It 'should create task with AtStartup trigger' {
            $taskName = 'TestTask_Startup_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                execute = 'powershell.exe'
                arguments = '-Command "Write-Host Startup"'
                triggerType = 'AtStartup'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $inputJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.triggerType | Should -Be 'AtStartup'
        }

        It 'should create task in custom folder path' {
            $taskName = 'TestTask_CustomPath_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $inputJson = @{
                taskName = $taskName
                taskPath = '\OpenDsc\Tests\'
                execute = 'notepad.exe'
                triggerType = 'Daily'
                startTime = '12:00'
                daysInterval = 1
            } | ConvertTo-Json -Compress

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
                execute = 'notepad.exe'
                triggerType = 'Daily'
                startTime = '08:00'
                daysInterval = 1
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $createJson | Out-Null

            $updateJson = @{
                taskName = $taskName
                execute = 'cmd.exe'
                arguments = '/c dir'
                triggerType = 'Daily'
                startTime = '09:00'
                daysInterval = 2
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $updateJson | Out-Null

            $verifyJson = @{
                taskName = $taskName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/ScheduledTask --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.execute | Should -Be 'cmd.exe'
            $getResult.actualState.startTime | Should -Be '09:00'
            $getResult.actualState.daysInterval | Should -Be 2
        }
    }

    Context 'Delete Operation' -Skip:(!$script:isAdmin) -Tag 'Delete' {
        It 'should delete scheduled task' {
            $taskName = 'TestTask_Delete_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

            $createJson = @{
                taskName = $taskName
                execute = 'notepad.exe'
                triggerType = 'Daily'
                startTime = '10:00'
                daysInterval = 1
            } | ConvertTo-Json -Compress

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

        It 'should export tasks with different trigger types' {
            $result = dsc resource export -r OpenDsc.Windows/ScheduledTask | ConvertFrom-Json

            $tasks = $result.resources | Where-Object { $_.properties.triggerType -ne $null }
            $tasks.Count | Should -BeGreaterThan 0
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should validate task name pattern (no invalid characters)' {
            $invalidInput = @{
                taskName = 'Invalid:Task*Name'
                execute = 'notepad.exe'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/ScheduledTask --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid task names' {
            $validInput = @{
                taskName = 'Valid-Task_Name123'
                execute = 'notepad.exe'
                triggerType = 'Daily'
                startTime = '10:00'
                daysInterval = 1
            } | ConvertTo-Json -Compress

            if ($script:isAdmin) {
                dsc resource set -r OpenDsc.Windows/ScheduledTask --input $validInput | Out-Null
                $LASTEXITCODE | Should -Be 0

                schtasks /delete /tn 'Valid-Task_Name123' /f 2>&1 | Out-Null
            }
        }
    }
}
