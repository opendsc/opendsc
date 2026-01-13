if (!$IsWindows) {
    $script:isAdmin = (id -u) -eq 0
}

Describe 'POSIX File System Permission Resource' -Tag 'Linux', 'macOS' -Skip:($IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
        $script:testDir = Join-Path $TestDrive "posix-permission-tests"
        New-Item -ItemType Directory -Path $script:testDir -Force | Out-Null
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Posix.FileSystem/Permission | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Posix.FileSystem/Permission'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Posix.FileSystem/Permission | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Not -Contain 'delete'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should throw error for non-existent file' {
            $inputJson = @{
                path = '/tmp/nonexistent_file_12345_xyz'
            } | ConvertTo-Json -Compress

            dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should read permissions of existing file' {
            $testFile = Join-Path $script:testDir "test-get.txt"
            "test content" | Out-File -FilePath $testFile
            chmod 644 $testFile

            $inputJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | ConvertFrom-Json
            $result.actualState.path | Should -Be $testFile
            $result.actualState.mode | Should -Match '^0?644$'
            $result.actualState.owner | Should -Not -BeNullOrEmpty
            $result.actualState.group | Should -Not -BeNullOrEmpty
        }

        It 'should read permissions of existing directory' {
            $testDir = Join-Path $script:testDir "test-dir"
            New-Item -ItemType Directory -Path $testDir -Force | Out-Null
            chmod 755 $testDir

            $inputJson = @{
                path = $testDir
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | ConvertFrom-Json
            $result.actualState.mode | Should -Match '^0?755$'
        }
    }

    Context 'Set Operation - Mode' -Tag 'Set' {
        It 'should set file mode with octal notation' {
            $testFile = Join-Path $script:testDir "test-mode.txt"
            "test" | Out-File -FilePath $testFile
            chmod 777 $testFile

            $inputJson = @{
                path = $testFile
                mode = '0644'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.mode | Should -Match '^0?644$'
        }

        It 'should accept octal without leading zero' {
            $testFile = Join-Path $script:testDir "test-mode-no-leading.txt"
            "test" | Out-File -FilePath $testFile

            $inputJson = @{
                path = $testFile
                mode = '755'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.mode | Should -Match '^0?755$'
        }
    }

    Context 'Set Operation - Owner and Group' -Tag 'Set' -Skip:(!$script:isAdmin) {
        It 'should change file owner by username' {
            $testFile = Join-Path $script:testDir "test-owner.txt"
            "test" | Out-File -FilePath $testFile

            # Use root as owner (available on all Unix systems)
            $inputJson = @{
                path = $testFile
                owner = 'root'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -Be 'root'
        }

        It 'should change file owner by numeric UID' {
            $testFile = Join-Path $script:testDir "test-owner-uid.txt"
            "test" | Out-File -FilePath $testFile

            $inputJson = @{
                path = $testFile
                owner = '0'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -Be 'root'
        }

        It 'should change both owner and group' {
            $testFile = Join-Path $script:testDir "test-owner-group.txt"
            "test" | Out-File -FilePath $testFile

            # Determine appropriate group (different on Linux vs macOS)
            $testGroup = if ($IsMacOS) { 'wheel' } else { 'root' }

            $inputJson = @{
                path = $testFile
                owner = 'root'
                group = $testGroup
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.owner | Should -Be 'root'
            $getResult.actualState.group | Should -Be $testGroup
        }
    }

    Context 'Set Operation - Combined' -Tag 'Set' -Skip:(!$script:isAdmin) {
        It 'should set mode, owner, and group together' {
            $testFile = Join-Path $script:testDir "test-combined.txt"
            "test" | Out-File -FilePath $testFile

            $testGroup = if ($IsMacOS) { 'wheel' } else { 'root' }

            $inputJson = @{
                path = $testFile
                mode = '0640'
                owner = 'root'
                group = $testGroup
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null

            $verifyJson = @{
                path = $testFile
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.mode | Should -Match '^0?640$'
            $getResult.actualState.owner | Should -Be 'root'
            $getResult.actualState.group | Should -Be $testGroup
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject invalid octal mode' {
            $testFile = Join-Path $script:testDir "test-invalid-mode.txt"
            "test" | Out-File -FilePath $testFile

            $invalidInput = @{
                path = $testFile
                mode = '999'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should reject mode with invalid characters' {
            $testFile = Join-Path $script:testDir "test-invalid-chars.txt"
            "test" | Out-File -FilePath $testFile

            $invalidInput = @{
                path = $testFile
                mode = '0abc'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept 4-digit octal mode' {
            $testFile = Join-Path $script:testDir "test-4digit.txt"
            "test" | Out-File -FilePath $testFile

            $inputJson = @{
                path = $testFile
                mode = '0755'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }

    Context 'Error Handling' -Tag 'ErrorHandling' {
        It 'should throw error for non-existent file during set' {
            $inputJson = @{
                path = '/tmp/does_not_exist_12345.txt'
                mode = '0644'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should throw error for non-existent user' -Skip:(!$script:isAdmin) {
            $testFile = Join-Path $script:testDir "test-bad-user.txt"
            "test" | Out-File -FilePath $testFile

            $inputJson = @{
                path = $testFile
                owner = 'nonexistent_user_xyz_12345'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should throw error for non-existent group' -Skip:(!$script:isAdmin) {
            $testFile = Join-Path $script:testDir "test-bad-group.txt"
            "test" | Out-File -FilePath $testFile

            $inputJson = @{
                path = $testFile
                group = 'nonexistent_group_xyz_12345'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    AfterAll {
        if (Test-Path $script:testDir) {
            Remove-Item -Path $script:testDir -Recurse -Force
        }
    }
}
