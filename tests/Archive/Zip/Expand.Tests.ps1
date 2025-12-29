Describe 'Archive Zip Expand Resource' -Tag 'Archive', 'Zip', 'Expand' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testArchivePath = Join-Path $TestDrive "test.zip"
        $script:testSourcePath = Join-Path $TestDrive "source"
        $script:testDestinationPath = Join-Path $TestDrive "destination"
        $script:testFile1Path = Join-Path $script:testSourcePath "file1.txt"
        $script:testFile2Path = Join-Path $script:testSourcePath "file2.txt"
    }

    AfterEach {
        if (Test-Path $script:testArchivePath) {
            Remove-Item $script:testArchivePath -Force
        }
        if (Test-Path $script:testSourcePath) {
            Remove-Item $script:testSourcePath -Recurse -Force
        }
        if (Test-Path $script:testDestinationPath) {
            Remove-Item $script:testDestinationPath -Recurse -Force
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Archive.Zip/Expand | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Archive.Zip/Expand'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Archive.Zip/Expand | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'test'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should retrieve schema successfully' {
            $result = dsc resource schema -r OpenDsc.Archive.Zip/Expand
            $result | Should -Not -BeNullOrEmpty
            $schema = $result | ConvertFrom-Json
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.archivePath | Should -Not -BeNullOrEmpty
            $schema.properties.destinationPath | Should -Not -BeNullOrEmpty
        }

        It 'should require archivePath' {
            $invalidInput = @{
                destinationPath = "C:\\destination"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should require destinationPath' {
            $invalidInput = @{
                archivePath = "C:\\test.zip"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return current state' {
            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState | Should -Not -BeNullOrEmpty
            $result.actualState.archivePath | Should -Be $script:testArchivePath
            $result.actualState.destinationPath | Should -Be $script:testDestinationPath
        }

        It 'should preserve input parameters' {
            $inputJson = @{
                archivePath = "C:\\custom\\archive.zip"
                destinationPath = "C:\\custom\\destination"
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState.archivePath | Should -Be "C:\\custom\\archive.zip"
            $result.actualState.destinationPath | Should -Be "C:\\custom\\destination"
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should extract archive to destination' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content 1"
            Set-Content -Path $script:testFile2Path -Value "Test content 2"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path (Join-Path $script:testDestinationPath "file1.txt") | Should -Be $true
            Test-Path (Join-Path $script:testDestinationPath "file2.txt") | Should -Be $true
            Get-Content (Join-Path $script:testDestinationPath "file1.txt") | Should -Be "Test content 1"
        }

        It 'should create destination directory if it does not exist' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $script:testDestinationPath | Should -Be $true
        }

        It 'should overwrite existing files' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "New content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            # Create existing file with different content
            New-Item -Path $script:testDestinationPath -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $script:testDestinationPath "file1.txt") -Value "Old content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Get-Content (Join-Path $script:testDestinationPath "file1.txt") | Should -Be "New content"
        }

        It 'should preserve existing files not in archive (additive)' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Archive content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            # Create existing file not in archive
            New-Item -Path $script:testDestinationPath -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $script:testDestinationPath "existing.txt") -Value "Existing content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path (Join-Path $script:testDestinationPath "existing.txt") | Should -Be $true
            Get-Content (Join-Path $script:testDestinationPath "existing.txt") | Should -Be "Existing content"
        }

        It 'should error when archive does not exist' {
            $inputJson = @{
                archivePath = "C:\\NonExistent.zip"
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should handle empty archive' {
            # Create empty archive
            $emptyArchivePath = Join-Path $TestDrive "empty.zip"
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::Open($emptyArchivePath, [System.IO.Compression.ZipArchiveMode]::Create)
            $archive.Dispose()

            $inputJson = @{
                archivePath = $emptyArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Destination directory should exist but be empty
            Test-Path $script:testDestinationPath | Should -Be $true
            (Get-ChildItem $script:testDestinationPath).Count | Should -Be 0
        }

        It 'should handle archive with nested directories' {
            # Create archive with nested structure
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $nestedDir = Join-Path $script:testSourcePath "subdir"
            New-Item -Path $nestedDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $nestedDir "nested.txt") -Value "Nested content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path (Join-Path $script:testDestinationPath "subdir\nested.txt") | Should -Be $true
            Get-Content (Join-Path $script:testDestinationPath "subdir\nested.txt") | Should -Be "Nested content"
        }

        It 'should handle archive with empty files' {
            # Create archive with empty file
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $emptyFile = Join-Path $script:testSourcePath "empty.txt"
            New-Item -Path $emptyFile -ItemType File -Force | Out-Null

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $extractedFile = Join-Path $script:testDestinationPath "empty.txt"
            Test-Path $extractedFile | Should -Be $true
            (Get-Item $extractedFile).Length | Should -Be 0
        }

        It 'should handle files with special characters in names' {
            # Create archive with file having special characters
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $specialFile = Join-Path $script:testSourcePath "file with spaces & special chars!.txt"
            Set-Content -Path $specialFile -Value "Special content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $extractedFile = Join-Path $script:testDestinationPath "file with spaces & special chars!.txt"
            Test-Path $extractedFile | Should -Be $true
            Get-Content $extractedFile | Should -Be "Special content"
        }

        It 'should error when destination path is a file instead of directory' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            # Create a file at destination path
            New-Item -Path $script:testDestinationPath -ItemType File -Force | Out-Null

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Test Operation' -Tag 'Test' {
        It 'should return false when archive does not exist' {
            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return false when destination does not exist' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return true when all archive files exist in destination with matching content' {
            # Create and extract archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should return false when archive file is missing from destination' {
            # Create archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            # Create destination but don't extract
            New-Item -Path $script:testDestinationPath -ItemType Directory -Force | Out-Null

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return false when file content differs' {
            # Create and extract archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Original content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null

            # Modify extracted file
            Set-Content -Path (Join-Path $script:testDestinationPath "file1.txt") -Value "Modified content"

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should ignore extra files in destination (additive behavior)' {
            # Create and extract archive
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null

            # Add extra file to destination
            Set-Content -Path (Join-Path $script:testDestinationPath "extra.txt") -Value "Extra content"

            # Should still be in desired state (extra files don't matter)
            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should handle empty archive in test' {
            # Create empty archive
            $emptyArchivePath = Join-Path $TestDrive "empty.zip"
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::Open($emptyArchivePath, [System.IO.Compression.ZipArchiveMode]::Create)
            $archive.Dispose()

            # Create empty destination
            New-Item -Path $script:testDestinationPath -ItemType Directory -Force | Out-Null

            $inputJson = @{
                archivePath = $emptyArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should handle nested directories in test' {
            # Create and extract archive with nested structure
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $nestedDir = Join-Path $script:testSourcePath "subdir"
            New-Item -Path $nestedDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $nestedDir "nested.txt") -Value "Nested content"

            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::CreateFromDirectory($script:testSourcePath, $script:testArchivePath)

            $inputJson = @{
                archivePath = $script:testArchivePath
                destinationPath = $script:testDestinationPath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Expand --input $inputJson | Out-Null

            $result = dsc resource test -r OpenDsc.Archive.Zip/Expand --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }
    }
}
