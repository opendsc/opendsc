#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds and tests the OpenDsc solution.
.DESCRIPTION
    This script builds the entire OpenDsc solution and runs all tests.
.PARAMETER Configuration
    The build configuration to use (Debug or Release). Default is Release.
.PARAMETER SkipBuild
    Skip running build.
.PARAMETER SkipTest
    Skip running all tests after building.
.PARAMETER SkipUnitTests
    Skip running unit tests (xUnit tests with Category=Unit).
.PARAMETER SkipIntegrationTests
    Skip running integration tests (xUnit tests with Category=Integration).
.PARAMETER SkipFunctionalTests
    Skip running functional tests (cross-database provider tests with Testcontainers).
.PARAMETER SkipE2ETests
    Skip running E2E tests (Pester tests for DSC resources).
.PARAMETER Pack
    Pack NuGet packages after building.
.PARAMETER InstallDsc
    Install DSC CLI before running tests.
.PARAMETER GitHubToken
    GitHub token for API authentication to avoid rate limiting.
.PARAMETER Portable
    Build self-contained portable versions with embedded .NET runtime.
.PARAMETER Msi
    Build MSI installer packages (Windows only).
.EXAMPLE
    .\build.ps1
    Builds and tests the solution in Release configuration.
.EXAMPLE
    .\build.ps1 -Configuration Debug
    Builds and tests the solution in Debug configuration.
.EXAMPLE
    .\build.ps1 -SkipTest
    Builds the solution without running tests.
.EXAMPLE
    .\build.ps1 -Pack
    Builds the solution and creates NuGet packages.
.EXAMPLE
    .\build.ps1 -InstallDsc
    Builds the solution, installs DSC, and runs tests.
.EXAMPLE
    .\build.ps1 -Portable
    Builds a self-contained portable version with embedded .NET runtime.
.EXAMPLE
    .\build.ps1 -Msi
    Builds the MSI installer package.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipBuild,

    [switch] $SkipTest,

    [switch] $SkipUnitTests,

    [switch] $SkipIntegrationTests,

    [switch] $SkipFunctionalTests,

    [switch] $SkipE2ETests,

    [switch] $Pack,

    [switch] $InstallDsc,

    [switch] $Portable,

    [switch] $Msi,

    [string] $GitHubToken
)

$ErrorActionPreference = 'Stop'

if (-not $SkipBuild) {
    Write-Host 'Building OpenDsc solution...' -ForegroundColor Cyan

    $publishDir = Join-Path $PSScriptRoot 'artifacts\publish'
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    if ($Portable) {
        $version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
    }

    if ($IsWindows) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        if (Test-Path $resourcesProj) {
            dotnet publish $resourcesProj -c $Configuration -f net10.0-windows -o $publishDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
            }

            Write-Host 'Building LCM Service...' -ForegroundColor Cyan
            $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
            if (Test-Path $lcmProj) {
                $lcmDir = Join-Path $PSScriptRoot 'artifacts\Lcm'
                dotnet publish $lcmProj -c $Configuration -f net10.0-windows -o $lcmDir -p:GenerateDocumentationFile=false
                if ($LASTEXITCODE -ne 0) {
                    throw "Build failed for OpenDsc.Lcm with exit code $LASTEXITCODE"
                }
            }

            if ($Portable) {
                Write-Host 'Building self-contained portable version...' -ForegroundColor Cyan
                $portableDir = Join-Path $PSScriptRoot 'artifacts\portable'
                New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
                dotnet publish $resourcesProj `
                    --configuration $Configuration `
                    --runtime win-x64 `
                    --self-contained true `
                    -f net10.0-windows `
                    -p:PublishSingleFile=true `
                    -p:IncludeNativeLibrariesForSelfExtract=true `
                    -p:EnableCompressionInSingleFile=false `
                    -p:DebugType=None `
                    -p:DebugSymbols=false `
                    -p:GenerateDocumentationFile=false `
                    -p:CopyOutputSymbolsToPublishDirectory=false `
                    --output $portableDir
                if ($LASTEXITCODE -ne 0) {
                    throw "Portable build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
                }

                Write-Host 'Creating portable ZIP archive...' -ForegroundColor Cyan
                $zipDir = Join-Path $PSScriptRoot 'artifacts\zip'
                New-Item -ItemType Directory -Path $zipDir -Force | Out-Null
                $zipPath = Join-Path $zipDir "OpenDSC.Resources.Windows.Portable-$version.zip"
                Compress-Archive -Path "$portableDir\*" -DestinationPath $zipPath -Force
                Write-Host 'Self-contained portable version built successfully!' -ForegroundColor Green
                Write-Host "Output location: $portableDir" -ForegroundColor Green
                Write-Host "ZIP archive: $zipPath" -ForegroundColor Green
            }
        }

        if ($Msi) {
            Write-Host 'Building MSI installer...' -ForegroundColor Cyan
            $wixProj = Join-Path $PSScriptRoot 'packaging\msi\OpenDsc.Resources\OpenDsc.Resources.wixproj'
            if (Test-Path $wixProj) {
                dotnet build $wixProj -c $Configuration
                if ($LASTEXITCODE -ne 0) {
                    throw "Build failed for MSI installer with exit code $LASTEXITCODE"
                }
                $msiDir = Join-Path $PSScriptRoot 'artifacts\msi'
                Write-Host 'MSI installer built successfully!' -ForegroundColor Green
                Write-Host "Output location: $msiDir" -ForegroundColor Green
            }

            Write-Host 'Building LCM MSI installer...' -ForegroundColor Cyan
            $lcmWixProj = Join-Path $PSScriptRoot 'packaging\msi\OpenDsc.Lcm\OpenDsc.Lcm.wixproj'
            if (Test-Path $lcmWixProj) {
                dotnet build $lcmWixProj -c $Configuration
                if ($LASTEXITCODE -ne 0) {
                    throw "Build failed for LCM MSI installer with exit code $LASTEXITCODE"
                }
                Write-Host 'LCM MSI installer built successfully!' -ForegroundColor Green
                Write-Host "Output location: $msiDir" -ForegroundColor Green
            }
        }

        Write-Host 'Building TestService...' -ForegroundColor Cyan
        $testServiceProj = Join-Path $PSScriptRoot 'tests\TestService\TestService.csproj'
        if (Test-Path $testServiceProj) {
            $testServiceDir = Join-Path $PSScriptRoot 'artifacts\TestService'
            dotnet publish $testServiceProj -c $Configuration -o $testServiceDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for TestService with exit code $LASTEXITCODE"
            }
        }
    } elseif ($IsLinux) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        if (Test-Path $resourcesProj) {
            dotnet publish $resourcesProj -c $Configuration -f net10.0 -o $publishDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
            }
        }

        Write-Host 'Building LCM Service for Linux...' -ForegroundColor Cyan
        $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
        if (Test-Path $lcmProj) {
            $lcmDir = Join-Path $PSScriptRoot 'artifacts\Lcm'
            dotnet publish $lcmProj -c $Configuration -f net10.0 -o $lcmDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for OpenDsc.Lcm with exit code $LASTEXITCODE"
            }
        }

        if ($Portable) {
            Write-Host 'Building self-contained portable version for Linux...' -ForegroundColor Cyan
            $portableLinuxDir = Join-Path $PSScriptRoot 'artifacts\portable'
            New-Item -ItemType Directory -Path $portableLinuxDir -Force | Out-Null
            dotnet publish $resourcesProj `
                --configuration $Configuration `
                --runtime linux-x64 `
                --self-contained true `
                -f net10.0 `
                -p:PublishSingleFile=true `
                -p:IncludeNativeLibrariesForSelfExtract=true `
                -p:EnableCompressionInSingleFile=false `
                -p:DebugType=None `
                -p:DebugSymbols=false `
                -p:GenerateDocumentationFile=false `
                -p:CopyOutputSymbolsToPublishDirectory=false `
                --output $portableLinuxDir
            if ($LASTEXITCODE -ne 0) {
                throw "Portable build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
            }

            Write-Host 'Creating portable ZIP archive for Linux...' -ForegroundColor Cyan
            $zipDir = Join-Path $PSScriptRoot 'artifacts\zip'
            New-Item -ItemType Directory -Path $zipDir -Force | Out-Null
            $zipPath = Join-Path $zipDir "OpenDSC.Resources.Linux.Portable-$version.zip"
            Compress-Archive -Path "$portableLinuxDir\*" -DestinationPath $zipPath -Force
            Write-Host 'Self-contained portable version for Linux built successfully!' -ForegroundColor Green
            Write-Host "Output location: $portableLinuxDir" -ForegroundColor Green
            Write-Host "ZIP archive: $zipPath" -ForegroundColor Green
        }
    } elseif ($IsMacOS) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        if (Test-Path $resourcesProj) {
            dotnet publish $resourcesProj -c $Configuration -f net10.0 -o $publishDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
            }
        }

        Write-Host 'Building LCM Service for macOS...' -ForegroundColor Cyan
        $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
        if (Test-Path $lcmProj) {
            $lcmDir = Join-Path $PSScriptRoot 'artifacts\Lcm'
            dotnet publish $lcmProj -c $Configuration -f net10.0 -o $lcmDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for OpenDsc.Lcm with exit code $LASTEXITCODE"
            }
        }

        if ($Portable) {
            Write-Host 'Building self-contained portable version for macOS...' -ForegroundColor Cyan
            $portableMacDir = Join-Path $PSScriptRoot 'artifacts\portable'
            New-Item -ItemType Directory -Path $portableMacDir -Force | Out-Null
            dotnet publish $resourcesProj `
                --configuration $Configuration `
                --runtime osx-arm64 `
                --self-contained true `
                -f net10.0 `
                -p:PublishSingleFile=true `
                -p:IncludeNativeLibrariesForSelfExtract=true `
                -p:EnableCompressionInSingleFile=false `
                -p:DebugType=None `
                -p:DebugSymbols=false `
                -p:GenerateDocumentationFile=false `
                -p:CopyOutputSymbolsToPublishDirectory=false `
                --output $portableMacDir
            if ($LASTEXITCODE -ne 0) {
                throw "Portable build failed for OpenDsc.Resources with exit code $LASTEXITCODE"
            }

            Write-Host 'Creating portable ZIP archive for macOS...' -ForegroundColor Cyan
            $zipDir = Join-Path $PSScriptRoot 'artifacts\zip'
            New-Item -ItemType Directory -Path $zipDir -Force | Out-Null
            $zipPath = Join-Path $zipDir "OpenDSC.Resources.macOS.Portable-$version.zip"
            Compress-Archive -Path "$portableMacDir\*" -DestinationPath $zipPath -Force
            Write-Host 'Self-contained portable version for macOS built successfully!' -ForegroundColor Green
            Write-Host "Output location: $portableMacDir" -ForegroundColor Green
            Write-Host "ZIP archive: $zipPath" -ForegroundColor Green
        }
    }

    if ($Pack) {
        $packagesDir = Join-Path $PSScriptRoot 'artifacts\packages'
        New-Item -ItemType Directory -Path $packagesDir -Force | Out-Null
        dotnet pack $PSScriptRoot --configuration $Configuration --output $packagesDir
        if ($LASTEXITCODE -ne 0) {
            throw "Pack failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host 'Building test resources...' -ForegroundColor Cyan
    $testResourceProjects = @(
        @{ Name = 'TestResource.Aot'; Project = 'tests/TestResource.Aot/TestResource.Aot.csproj' },
        @{ Name = 'TestResource.NonAot'; Project = 'tests/TestResource.NonAot/TestResource.NonAot.csproj' },
        @{ Name = 'TestResource.Options'; Project = 'tests/TestResource.Options/TestResource.Options.csproj' },
        @{ Name = 'TestResource.Multi'; Project = 'tests/TestResource.Multi/TestResource.Multi.csproj' }
    )
    foreach ($proj in $testResourceProjects) {
        $projPath = Join-Path $PSScriptRoot $proj.Project
        if (Test-Path $projPath) {
            $outputDir = Join-Path $PSScriptRoot "artifacts\$($proj.Name)"
            dotnet publish $projPath -c $Configuration -o $outputDir -p:GenerateDocumentationFile=false
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for $($proj.Name) with exit code $LASTEXITCODE"
            }
        }
    }

    Write-Host 'Build completed successfully!' -ForegroundColor Green
}

if ($InstallDsc) {
    if (Get-Command dsc -ErrorAction SilentlyContinue) {
        Write-Host 'DSC is already installed.'
    } else {
        $headers = if ($GitHubToken) { @{Authorization = "Bearer $GitHubToken" } } else { $null }
        $releases = Invoke-RestMethod -Uri 'https://api.github.com/repos/PowerShell/DSC/releases' -Headers $headers
        $release = $releases | Where-Object { $_.prerelease } | Select-Object -First 1
        if (-not $release) {
            $release = $releases | Select-Object -First 1  # Fallback to latest if no prerelease
        }
        $tag = $release.tag_name
        $version = $tag -replace '^v', ''
        Write-Host "Latest DSC prerelease version: $version"

        if ($IsWindows) {
            $platform = 'x86_64-pc-windows-msvc'
            $extension = 'zip'
        } elseif ($IsLinux) {
            $platform = 'x86_64-linux'
            $extension = 'tar.gz'
        } elseif ($IsMacOS) {
            $platform = 'aarch64-apple-darwin'
            $extension = 'tar.gz'
        }

        $url = "https://github.com/PowerShell/DSC/releases/download/$tag/DSC-$version-$platform.$extension"
        $archive = "dsc.$extension"

        Invoke-WebRequest -Uri $url -OutFile $archive
        New-Item -ItemType Directory -Path ./dsc -Force | Out-Null

        if ($extension -eq 'zip') {
            Expand-Archive -Path $archive -DestinationPath ./dsc
        } else {
            tar -xzf $archive -C ./dsc
            if (-not $IsWindows) {
                chmod +x ./dsc/dsc
            }
        }

        Remove-Item $archive

        $pathSeparator = if ($IsWindows) { ';' } else { ':' }
        $env:PATH += "$pathSeparator$($PSScriptRoot)/dsc"
    }

    $dscVersion = dsc --version
    Write-Host "Installed DSC version: $dscVersion"
}

if (-not $IsWindows) {
    $testExecutables = @(
        'artifacts/TestResource.Aot/test-resource-aot',
        'artifacts/TestResource.NonAot/test-resource-non-aot',
        'artifacts/TestResource.Options/test-resource-options',
        'artifacts/TestResource.Multi/test-resource-multi'
    )
    foreach ($exe in $testExecutables) {
        if (Test-Path $exe) {
            chmod +x $exe
        }
    }
}

if ($SkipTest) {
    exit 0
}

$env:BUILD_CONFIGURATION = $Configuration

$publishDir = Join-Path $PSScriptRoot 'artifacts\publish'
if (Test-Path $publishDir) {
    $env:DSC_RESOURCE_PATH = $publishDir
}

Write-Host 'Running tests...' -ForegroundColor Cyan

$testBuildArgs = if ($SkipBuild) { @() } else { @('--no-build') }

if (-not $SkipUnitTests) {
    Write-Host 'Running unit tests...' -ForegroundColor Cyan
    dotnet test --configuration $Configuration @testBuildArgs --filter 'Category=Unit' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Unit tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Unit tests passed!' -ForegroundColor Green
}

if (-not $SkipIntegrationTests) {
    Write-Host 'Running integration tests...' -ForegroundColor Cyan
    dotnet test --configuration $Configuration @testBuildArgs --filter 'Category=Integration' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Integration tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Integration tests passed!' -ForegroundColor Green
}

if (-not $SkipFunctionalTests) {
    Write-Host 'Running functional tests (cross-database provider tests)...' -ForegroundColor Cyan
    Write-Host 'Note: This requires Docker to be running for SQL Server and PostgreSQL containers.' -ForegroundColor Yellow
    dotnet test --configuration $Configuration @testBuildArgs --filter 'Category=Functional' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Functional tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Functional tests passed!' -ForegroundColor Green
}

if (-not $SkipE2ETests) {
    Write-Host 'Running E2E tests (Pester)...' -ForegroundColor Cyan
    Invoke-Pester -Output Detailed
}
