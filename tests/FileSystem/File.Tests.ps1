Describe 'File Resource' -Tag 'Windows', 'Linux', 'macOS' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.FileSystem/File | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.FileSystem/File'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.FileSystem/File | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }

}

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent file' {
            $testFile = Join-Path $TestDrive 'NonExistentFile.txt'
            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/File --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.path | Should -Be $testFile
        }

        It 'should read content of existing file' {
            $testFile = Join-Path $TestDrive 'ExistingFile.txt'
            $testContent = 'Hello, World!'
            Set-Content -Path $testFile -Value $testContent -NoNewline

            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.FileSystem/File --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $testFile
            $result.actualState.content | Should -Be $testContent
            $result.actualState._exist | Should -BeNullOrEmpty

            Remove-Item $testFile
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should create a new file with content' {
            $testFile = Join-Path $TestDrive 'NewFile.txt'
            $testContent = 'New file content'

            $inputJson = @{
                path = $testFile
                content = $testContent
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/File --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.content | Should -Be $testContent
            $getResult.actualState._exist | Should -BeNullOrEmpty

            Remove-Item $testFile
        }

        It 'should create a new empty file when content is null' {
            $testFile = Join-Path $TestDrive 'EmptyFile.txt'

            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/File --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.content | Should -Be ''
            $getResult.actualState._exist | Should -BeNullOrEmpty

            Remove-Item $testFile
        }

        It 'should update existing file content' {
            $testFile = Join-Path $TestDrive 'UpdateFile.txt'
            $originalContent = 'Original content'
            $newContent = 'Updated content'

            Set-Content -Path $testFile -Value $originalContent

            $inputJson = @{
                path = $testFile
                content = $newContent
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/File --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.content | Should -Be $newContent

            Remove-Item $testFile
        }

        It 'should fail when parent directory does not exist' {
            $nonExistentDir = Join-Path $TestDrive 'NonExistentDir'
            $testFile = Join-Path $nonExistentDir 'FileInNonExistentDir.txt'
            $testContent = 'This should fail'

            $inputJson = @{
                path = $testFile
                content = $testContent
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Be 2
        }

    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should delete an existing file' {
            $testFile = Join-Path $TestDrive 'FileToDelete.txt'
            $testContent = 'Content to delete'
            Set-Content -Path $testFile -Value $testContent

            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.FileSystem/File --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.FileSystem/File --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should not error when deleting non-existent file' {
            $testFile = Join-Path $TestDrive 'NonExistentToDelete.txt'

            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.FileSystem/File --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject when path is missing' {
            $invalidInput = @{
                content = 'Some content'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid file paths' {
            $testFile = Join-Path $TestDrive 'ValidFile.txt'
            $validInput = @{
                path = $testFile
                content = 'Test content'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.FileSystem/File --input $validInput | Out-Null
            $LASTEXITCODE | Should -Be 0

            Remove-Item $testFile -ErrorAction SilentlyContinue
        }

        It 'should retrieve schema successfully' {
            $schema = dsc resource schema -r OpenDsc.FileSystem/File | ConvertFrom-Json
            $schema | Should -Not -BeNullOrEmpty
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.path | Should -Not -BeNullOrEmpty
            $schema.required | Should -Contain 'path'
        }
    }
}
