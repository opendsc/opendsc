if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
} else {
    $script:isAdmin = $true
}

Describe 'SymbolicLink Resource' -Tag 'Windows', 'Linux', 'macOS' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot '..\..\artifacts\publish'
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.FileSystem/SymbolicLink | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.FileSystem/SymbolicLink'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.FileSystem/SymbolicLink | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent symlink' {
            $testLink = Join-Path $TestDrive 'NonExistentLink'
            $inputJson = @{
                path   = $testLink
                target = '/some/target'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be $testLink
        }

        It 'should read properties of existing file symlink' -Skip:(!$script:isAdmin) {
            $testTarget = Join-Path $TestDrive 'TargetFile.txt'
            $testLink = Join-Path $TestDrive 'FileLink'
            $testContent = 'Target file content'
            Set-Content -Path $testTarget -Value $testContent -NoNewline

            if ($IsWindows) {
                cmd /c mklink $testLink $testTarget | Out-Null
            } else {
                ln -s $testTarget $testLink
            }

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $testLink
            $result.actualState.target | Should -Be $testTarget
            $result.actualState.type | Should -Be 'file'
            $result.actualState._exist | Should -BeNullOrEmpty

            # Cleanup
            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget -Force -ErrorAction SilentlyContinue
        }

        It 'should read properties of existing directory symlink' -Skip:(!$script:isAdmin) {
            $testTarget = Join-Path $TestDrive 'TargetDir'
            $testLink = Join-Path $TestDrive 'DirLink'
            New-Item -ItemType Directory -Path $testTarget -Force | Out-Null

            if ($IsWindows) {
                cmd /c mklink /D $testLink $testTarget | Out-Null
            } else {
                ln -s $testTarget $testLink
            }

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $testLink
            $result.actualState.target | Should -Be $testTarget
            $result.actualState.type | Should -Be 'directory'
            $result.actualState._exist | Should -BeNullOrEmpty

            # Cleanup
            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'Set Operation' -Tag 'Set' -Skip:(!$script:isAdmin) {
        It 'should create a new file symlink' {
            $testTarget = Join-Path $TestDrive 'TargetFile.txt'
            $testLink = Join-Path $TestDrive 'NewFileLink'
            $testContent = 'Target content'
            Set-Content -Path $testTarget -Value $testContent -NoNewline

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | Out-Null

            $verifyJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.target | Should -Be $testTarget
            $getResult.actualState.type | Should -Be 'file'
            $getResult.actualState._exist | Should -BeNullOrEmpty

            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget -Force -ErrorAction SilentlyContinue
        }

        It 'should create a new directory symlink' {
            $testTarget = Join-Path $TestDrive 'TargetDir'
            $testLink = Join-Path $TestDrive 'NewDirLink'
            New-Item -ItemType Directory -Path $testTarget -Force | Out-Null

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | Out-Null

            $verifyJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.target | Should -Be $testTarget
            $getResult.actualState.type | Should -Be 'directory'
            $getResult.actualState._exist | Should -BeNullOrEmpty

            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget -Force -ErrorAction SilentlyContinue
        }

        It 'should fail when target does not exist' {
            $testTarget = Join-Path $TestDrive 'NonExistentTarget'
            $testLink = Join-Path $TestDrive 'LinkToNonExistent'

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should update existing symlink target' {
            $testTarget1 = Join-Path $TestDrive 'Target1.txt'
            $testTarget2 = Join-Path $TestDrive 'Target2.txt'
            $testLink = Join-Path $TestDrive 'UpdateLink'
            Set-Content -Path $testTarget1 -Value 'Target 1' -NoNewline
            Set-Content -Path $testTarget2 -Value 'Target 2' -NoNewline

            $inputJson = @{
                path   = $testLink
                target = $testTarget1
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | Out-Null

            $updateJson = @{
                path   = $testLink
                target = $testTarget2
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $updateJson | Out-Null

            $verifyJson = @{
                path   = $testLink
                target = $testTarget2
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.target | Should -Be $testTarget2
            $getResult.actualState.type | Should -Be 'file'

            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget1 -Force -ErrorAction SilentlyContinue
            Remove-Item $testTarget2 -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'Delete Operation' -Tag 'Delete' -Skip:(!$script:isAdmin) {
        It 'should delete an existing symlink' {
            $testTarget = Join-Path $TestDrive 'TargetToDelete.txt'
            $testLink = Join-Path $TestDrive 'LinkToDelete'
            Set-Content -Path $testTarget -Value 'Target content' -NoNewline

            $inputJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $inputJson | Out-Null

            $deleteJson = @{
                path   = $testLink
                target = $testTarget
                _exist = $false
            } | ConvertTo-Json -Compress
            dsc resource delete -r OpenDsc.FileSystem/SymbolicLink --input $deleteJson | Out-Null

            $verifyJson = @{
                path   = $testLink
                target = $testTarget
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false

            Remove-Item $testTarget -Force -ErrorAction SilentlyContinue
        }

        It 'should not error when deleting non-existent symlink' {
            $testLink = Join-Path $TestDrive 'NonExistentLinkToDelete'

            $inputJson = @{
                path   = $testLink
                target = '/some/target'
                _exist = $false
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.FileSystem/SymbolicLink --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject when path is missing' {
            $invalidInput = @{
                target = '/some/target'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should reject when target is missing' {
            $invalidInput = @{
                path = '/some/path'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid symlink configuration' {
            $testLink = Join-Path $TestDrive 'ValidLink'
            $testTarget = Join-Path $TestDrive 'ValidTarget'
            $validInput = @{
                path   = $testLink
                target = $testTarget
                type   = 'File'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input $validInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 0

            Remove-Item $testLink -Force -ErrorAction SilentlyContinue
        }

        It 'should retrieve schema successfully' {
            $schema = dsc resource schema -r OpenDsc.FileSystem/SymbolicLink | ConvertFrom-Json
            $schema | Should -Not -BeNullOrEmpty
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.path | Should -Not -BeNullOrEmpty
            $schema.properties.target | Should -Not -BeNullOrEmpty
            $schema.required | Should -Contain 'path'
            $schema.required | Should -Contain 'target'
        }
    }
}
