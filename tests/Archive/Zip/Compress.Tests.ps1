Describe 'Archive Zip Compress Resource' -Tag 'Archive', 'Zip', 'Compress' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testArchivePath = Join-Path $TestDrive "test.zip"
        $script:testSourcePath = Join-Path $TestDrive "source"
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
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Archive.Zip/Compress | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Archive.Zip/Compress'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Archive.Zip/Compress | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'test'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should retrieve schema successfully' {
            $result = dsc resource schema -r OpenDsc.Archive.Zip/Compress
            $result | Should -Not -BeNullOrEmpty
            $schema = $result | ConvertFrom-Json
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.archivePath | Should -Not -BeNullOrEmpty
            $schema.properties.sourcePath | Should -Not -BeNullOrEmpty
        }

        It 'should require archivePath' {
            $invalidInput = @{
                sourcePath = "C:\\source"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should require sourcePath' {
            $invalidInput = @{
                archivePath = "C:\\test.zip"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return current state' {
            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState | Should -Not -BeNullOrEmpty
            $result.actualState.archivePath | Should -Be $script:testArchivePath
            $result.actualState.sourcePath | Should -Be $script:testSourcePath
        }

        It 'should preserve input parameters including compression level' {
            $inputJson = @{
                archivePath = "C:\\custom\\archive.zip"
                sourcePath = "C:\\custom\\source"
                compressionLevel = "Optimal"
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState.archivePath | Should -Be "C:\\custom\\archive.zip"
            $result.actualState.sourcePath | Should -Be "C:\\custom\\source"
            $result.actualState.compressionLevel | Should -Be "Optimal"
        }
    }

    Context 'Set Operation' -Tag 'Set' {
        It 'should create archive from directory' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content 1"
            Set-Content -Path $script:testFile2Path -Value "Test content 2"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $script:testArchivePath | Should -Be $true

            # Verify archive contents
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($script:testArchivePath)
            $archive.Entries.Count | Should -Be 2
            $archive.Dispose()
        }

        It 'should create archive from single file' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testFile1Path
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $script:testArchivePath | Should -Be $true

            # Verify archive contains the file
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($script:testArchivePath)
            $archive.Entries.Count | Should -Be 1
            $archive.Entries[0].Name | Should -Be "file1.txt"
            $archive.Dispose()
        }

        It 'should rebuild archive when source changes' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Original content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            # Modify source and recreate
            Set-Content -Path $script:testFile1Path -Value "Modified content"
            Set-Content -Path $script:testFile2Path -Value "New file"

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify archive has both files
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($script:testArchivePath)
            $archive.Entries.Count | Should -Be 2
            $archive.Dispose()
        }

        It 'should respect compression level' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value ("A" * 1000)

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
                compressionLevel = "Fastest"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $script:testArchivePath | Should -Be $true
        }

        It 'should error when source does not exist' {
            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = "C:\\NonExistentPath"
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should handle empty directory' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $script:testArchivePath | Should -Be $true

            # Verify archive is empty
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($script:testArchivePath)
            $archive.Entries.Count | Should -Be 0
            $archive.Dispose()
        }

        It 'should handle nested directory structure' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $nestedDir = Join-Path $script:testSourcePath "subdir"
            New-Item -Path $nestedDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $nestedDir "nested.txt") -Value "Nested content"
            Set-Content -Path $script:testFile1Path -Value "Root content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify archive contains nested structure
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($script:testArchivePath)
            $entries = $archive.Entries | Select-Object -ExpandProperty FullName
            $entries -contains "file1.txt" | Should -Be $true
            $entries -contains "subdir/nested.txt" | Should -Be $true
            $archive.Dispose()
        }

        It 'should create parent directories for archive path' {
            $nestedArchivePath = Join-Path $TestDrive "nested\path\archive.zip"
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $nestedArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            Test-Path $nestedArchivePath | Should -Be $true
        }

        It 'should handle different compression levels' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value ("A" * 1000)  # Large content for compression

            $compressionLevels = @("NoCompression", "Fastest", "Optimal")

            foreach ($level in $compressionLevels) {
                $archivePath = Join-Path $TestDrive "test_$level.zip"
                $inputJson = @{
                    archivePath = $archivePath
                    sourcePath = $script:testSourcePath
                    compressionLevel = $level
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null
                $LASTEXITCODE | Should -Be 0
                Test-Path $archivePath | Should -Be $true
            }
        }
    }

    Context 'Test Operation' -Tag 'Test' {
        It 'should return false when archive does not exist' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return false when source does not exist' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            # Remove source
            Remove-Item $script:testSourcePath -Recurse -Force

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return true when archive matches source' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should return false when source content changes' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Original content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            # Modify source file
            Set-Content -Path $script:testFile1Path -Value "Modified content"

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should return false when source has additional files' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            # Add new file to source
            Set-Content -Path $script:testFile2Path -Value "New content"

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }

        It 'should handle empty directory in test' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should handle nested directories in test' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            $nestedDir = Join-Path $script:testSourcePath "subdir"
            New-Item -Path $nestedDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $nestedDir "nested.txt") -Value "Nested content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $true
        }

        It 'should return false when archive has different file count' {
            New-Item -Path $script:testSourcePath -ItemType Directory -Force | Out-Null
            Set-Content -Path $script:testFile1Path -Value "Test content"

            $inputJson = @{
                archivePath = $script:testArchivePath
                sourcePath = $script:testSourcePath
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Archive.Zip/Compress --input $inputJson | Out-Null

            # Manually modify archive to have different content
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::Open($script:testArchivePath, [System.IO.Compression.ZipArchiveMode]::Update)
            $entry = $archive.CreateEntry("extra.txt")
            $writer = New-Object System.IO.StreamWriter($entry.Open())
            $writer.Write("Extra content")
            $writer.Dispose()
            $archive.Dispose()

            $result = dsc resource test -r OpenDsc.Archive.Zip/Compress --input $inputJson | ConvertFrom-Json
            $result.actualState._inDesiredState | Should -Be $false
        }
    }
}
