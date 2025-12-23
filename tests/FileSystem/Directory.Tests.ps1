Describe 'Directory Resource' -Tag 'Windows', 'Linux', 'macOS' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testTempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.FileSystem/Directory | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.FileSystem/Directory'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.FileSystem/Directory | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'test'
        }
}

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent directory' {
            $testDir = Join-Path $TestDrive 'NonExistentDir'
            $inputJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/Directory --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be $testDir
        }

        It 'should return _exist=true for existing directory' {
            $testDir = Join-Path $TestDrive 'ExistingDir'
            New-Item -ItemType Directory -Path $testDir | Out-Null

            $inputJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/Directory --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $testDir
            $result.actualState._exist | Should -BeNullOrEmpty
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should create a new directory' {
            $testDir = Join-Path $TestDrive 'NewDir'

            $inputJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/Directory --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/Directory --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -BeNullOrEmpty
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete an existing directory' {
            $testDir = Join-Path $TestDrive 'DirToDelete'
            New-Item -ItemType Directory -Path $testDir | Out-Null

            $inputJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.FileSystem/Directory --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/Directory --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }
    }

    Context 'Test Operation' {
        It 'should create directory and copy contents from source' {
            $sourceDir = Join-Path $TestDrive 'SourceDir'
            $targetDir = Join-Path $TestDrive 'TargetDir'

            New-Item -ItemType Directory -Path $sourceDir | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $sourceDir 'SubDir') | Out-Null
            'test content' | Out-File (Join-Path $sourceDir 'file1.txt')
            'sub content' | Out-File (Join-Path $sourceDir 'SubDir\file2.txt')

            $inputJson = @{
                path = $targetDir
                sourcePath = $sourceDir
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/Directory --input $inputJson | Out-Null

            Test-Path $targetDir | Should -Be $true
            Test-Path (Join-Path $targetDir 'file1.txt') | Should -Be $true
            Test-Path (Join-Path $targetDir 'SubDir') | Should -Be $true
            Test-Path (Join-Path $targetDir 'SubDir\file2.txt') | Should -Be $true
            Get-Content (Join-Path $targetDir 'file1.txt') | Should -Be 'test content'

            $testResult = dsc resource test -r OpenDsc.FileSystem/Directory --input $inputJson | ConvertFrom-Json
            $testResult.inDesiredState | Should -Be $true
        }

        It 'should return not in desired state if contents differ' {
            $sourceDir = Join-Path $TestDrive 'SourceDir2'
            $targetDir = Join-Path $TestDrive 'TargetDir2'

            New-Item -ItemType Directory -Path $sourceDir | Out-Null
            'original' | Out-File (Join-Path $sourceDir 'file.txt')

            New-Item -ItemType Directory -Path $targetDir | Out-Null
            'modified' | Out-File (Join-Path $targetDir 'file.txt')

            $inputJson = @{
                path = $targetDir
                sourcePath = $sourceDir
            } | ConvertTo-Json -Compress

            $testResult = dsc resource test -r OpenDsc.FileSystem/Directory --input $inputJson | ConvertFrom-Json
            $testResult.inDesiredState | Should -Be $false
        }

        It 'should return in desired state if target has extra files but source files match' {
            $sourceDir = Join-Path $TestDrive 'SourceDir3'
            $targetDir = Join-Path $TestDrive 'TargetDir3'

            New-Item -ItemType Directory -Path $sourceDir | Out-Null
            'content' | Out-File (Join-Path $sourceDir 'file.txt')

            New-Item -ItemType Directory -Path $targetDir | Out-Null
            'content' | Out-File (Join-Path $targetDir 'file.txt')
            'extra' | Out-File (Join-Path $targetDir 'extra.txt')

            $inputJson = @{
                path = $targetDir
                sourcePath = $sourceDir
            } | ConvertTo-Json -Compress

            $testResult = dsc resource test -r OpenDsc.FileSystem/Directory --input $inputJson | ConvertFrom-Json
            $testResult.inDesiredState | Should -Be $true
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject when path is missing' {
            $invalidInput = @{
                sourcePath = 'C:\Some\Path'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/Directory --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid directory paths' {
            $testDir = Join-Path $TestDrive 'ValidPath'
            $validInput = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/Directory --input $validInput | Out-Null
            $LASTEXITCODE | Should -Be 0
        }

        It 'should retrieve schema successfully' {
            $schema = dsc resource schema -r OpenDsc.FileSystem/Directory | ConvertFrom-Json
            $schema | Should -Not -BeNullOrEmpty
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.path | Should -Not -BeNullOrEmpty
            $schema.required | Should -Contain 'path'
        }
    }
}
