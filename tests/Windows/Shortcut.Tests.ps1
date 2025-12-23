Describe 'Windows Shortcut Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $notepadPath = 'C:\Windows\System32\notepad.exe'
        $cmdPath = 'C:\Windows\System32\cmd.exe'
        $calcPath = 'C:\Windows\System32\calc.exe'
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/Shortcut | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/Shortcut'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/Shortcut | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }

}

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent shortcut' {
            $shortcutPath = Join-Path $TestDrive 'nonexistent.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = ''
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be $shortcutPath
        }

        It 'should read properties of existing shortcut' {
            $shortcutPath = Join-Path $TestDrive 'test-get.lnk'

            $createInput = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = 'Test Description'
                arguments = '/test'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $createInput | Out-Null

            $getInput = @{
                path = $shortcutPath
                targetPath = ''
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.path | Should -Be $shortcutPath
            $result.actualState.targetPath | Should -Be $notepadPath
            $result.actualState.description | Should -Be 'Test Description'
            $result.actualState.arguments | Should -Be '/test'

            Remove-Item $shortcutPath -Force
        }
    }

    Context 'Set Operation - Basic Properties' -Tag 'Set' {
        It 'should create a basic shortcut with required properties only' {
            $shortcutPath = Join-Path $TestDrive 'basic.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | ConvertFrom-Json

            $result.beforeState._exist | Should -Be $false
            $result.afterState.targetPath | Should -Be $notepadPath
            $result.changedProperties | Should -Contain 'targetPath'
            $result.changedProperties | Should -Contain '_exist'

            Test-Path $shortcutPath | Should -Be $true

            Remove-Item $shortcutPath -Force
        }

        It 'should set description property' {
            $shortcutPath = Join-Path $TestDrive 'with-description.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = 'My Custom Description'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.description | Should -Be 'My Custom Description'

            Remove-Item $shortcutPath -Force
        }

        It 'should set arguments property' {
            $shortcutPath = Join-Path $TestDrive 'with-arguments.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $cmdPath
                arguments = '/k echo Hello World'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.arguments | Should -Be '/k echo Hello World'

            Remove-Item $shortcutPath -Force
        }

        It 'should set workingDirectory property' {
            $shortcutPath = Join-Path $TestDrive 'with-workdir.lnk'
            $workDir = 'C:\Windows'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                workingDirectory = $workDir
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.workingDirectory | Should -Be $workDir

            Remove-Item $shortcutPath -Force
        }

        It 'should set iconLocation property' {
            $shortcutPath = Join-Path $TestDrive 'with-icon.lnk'
            $iconLocation = "$($cmdPath),0"
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                iconLocation = $iconLocation
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.iconLocation | Should -Be $iconLocation

            Remove-Item $shortcutPath -Force
        }

        It 'should set hotkey property' {
            $shortcutPath = Join-Path $TestDrive 'with-hotkey.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                hotkey = 'CTRL+ALT+N'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.hotkey | Should -Not -BeNullOrEmpty

            Remove-Item $shortcutPath -Force
        }
    }

    Context 'Set Operation - Window Styles' -Tag 'Set' {
        It 'should set windowStyle to Normal' {
            $shortcutPath = Join-Path $TestDrive 'window-normal.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                windowStyle = 'Normal'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            ($result.actualState.windowStyle -eq 'Normal' -or $null -eq $result.actualState.windowStyle) | Should -Be $true

            Remove-Item $shortcutPath -Force
        }

        It 'should set windowStyle to Minimized' {
            $shortcutPath = Join-Path $TestDrive 'window-minimized.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                windowStyle = 'Minimized'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.windowStyle | Should -Be 'Minimized'

            Remove-Item $shortcutPath -Force
        }

        It 'should set windowStyle to Maximized' {
            $shortcutPath = Join-Path $TestDrive 'window-maximized.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                windowStyle = 'Maximized'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.windowStyle | Should -Be 'Maximized'

            Remove-Item $shortcutPath -Force
        }
    }

    Context 'Set Operation - Complex Scenarios' -Tag 'Set' {
        It 'should create shortcut with all properties set' {
            $shortcutPath = Join-Path $TestDrive 'all-properties.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $cmdPath
                arguments = '/k dir'
                workingDirectory = 'C:\Windows'
                description = 'Full Featured Shortcut'
                iconLocation = "$($cmdPath),0"
                windowStyle = 'Maximized'
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | ConvertFrom-Json

            $result.afterState.targetPath | Should -Be $cmdPath
            $result.afterState.arguments | Should -Be '/k dir'
            $result.afterState.workingDirectory | Should -Be 'C:\Windows'
            $result.afterState.description | Should -Be 'Full Featured Shortcut'
            $result.afterState.windowStyle | Should -Be 'Maximized'

            Remove-Item $shortcutPath -Force
        }

        It 'should update existing shortcut properties' {
            $shortcutPath = Join-Path $TestDrive 'update-test.lnk'

            $input1 = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = 'Original'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $input1 | Out-Null

            $input2 = @{
                path = $shortcutPath
                targetPath = $cmdPath
                description = 'Updated'
                arguments = '/k'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $input2 | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.targetPath | Should -Be $cmdPath
            $result.actualState.description | Should -Be 'Updated'
            $result.actualState.arguments | Should -Be '/k'

            Remove-Item $shortcutPath -Force
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete an existing shortcut' {
            $shortcutPath = Join-Path $TestDrive 'to-delete.lnk'

            $createInput = @{
                path = $shortcutPath
                targetPath = $notepadPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $createInput | Out-Null
            Test-Path $shortcutPath | Should -Be $true

            $deleteInput = @{
                path = $shortcutPath
                targetPath = ''
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/Shortcut --input $deleteInput
            Test-Path $shortcutPath | Should -Be $false
        }

        It 'should not fail when deleting non-existent shortcut' {
            $shortcutPath = Join-Path $TestDrive 'never-existed.lnk'

            $deleteInput = @{
                path = $shortcutPath
                targetPath = ''
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Windows/Shortcut --input $deleteInput } | Should -Not -Throw
            Test-Path $shortcutPath | Should -Be $false
        }
    }

    Context 'Error Handling' {
        It 'should fail when directory does not exist' {
            $shortcutPath = 'C:\NonExistentDirectory\shortcut.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson 2>&1
            $LASTEXITCODE | Should -Not -Be 0
            $result | Should -Match '(directory|Directory not found)'
        }
    }

    Context 'Edge Cases' {
        It 'should allow creating shortcut without targetPath' {
            $shortcutPath = Join-Path $TestDrive 'no-target.lnk'
            $inputJson = @{
                path = $shortcutPath
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | ConvertFrom-Json
            $result.afterState.path | Should -Be $shortcutPath
            $result.changedProperties | Should -Contain '_exist'
            Test-Path $shortcutPath | Should -Be $true

            Remove-Item $shortcutPath -Force
        }

        It 'should handle paths with spaces' {
            $shortcutPath = Join-Path $TestDrive 'path with spaces.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = 'Path with spaces'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null
            Test-Path $shortcutPath | Should -Be $true

            Remove-Item $shortcutPath -Force
        }

        It 'should handle special characters in description' {
            $shortcutPath = Join-Path $TestDrive 'special-chars.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = "Test & 'Special' `"Chars`""
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null

            $getInput = @{ path = $shortcutPath; targetPath = '' } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Shortcut --input $getInput | ConvertFrom-Json
            $result.actualState.description | Should -Be "Test & 'Special' `"Chars`""

            Remove-Item $shortcutPath -Force
        }

        It 'should handle empty strings for optional properties' {
            $shortcutPath = Join-Path $TestDrive 'empty-optionals.lnk'
            $inputJson = @{
                path = $shortcutPath
                targetPath = $notepadPath
                description = ''
                arguments = ''
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Shortcut --input $inputJson | Out-Null
            Test-Path $shortcutPath | Should -Be $true

            Remove-Item $shortcutPath -Force
        }
    }
}
