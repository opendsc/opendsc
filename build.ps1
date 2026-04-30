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
.PARAMETER CollectCoverage
    Collect code coverage during test runs using XPlat Code Coverage.
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
.EXAMPLE
    .\build.ps1 -LinuxPackages
    Builds .deb and .rpm packages for Linux (requires running on Linux with fpm installed).
.PARAMETER Architecture
    Limit Linux builds to a specific architecture (x64 or arm64). Builds all architectures if not specified.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipBuild,

    [switch] $SkipTest,

    [switch] $SkipUnitTests,

    [switch] $SkipIntegrationTests,

    [switch] $SkipFunctionalTests,

    [switch] $Pack,

    [switch] $InstallDsc,

    [switch] $Portable,

    [switch] $Msi,

    [switch] $LinuxPackages,

    [switch] $InstallSqlServer,

    [switch] $CollectCoverage,

    [string] $GitHubToken,

    [ValidateSet('x64', 'arm64')]
    [string] $Architecture
)

$ErrorActionPreference = 'Stop'

function Get-PublishPath {
    param([string]$Proj, [string]$Configuration, [string]$Framework, [string]$Runtime = $null)
    $path = Join-Path (Split-Path $Proj) 'bin' $Configuration $Framework
    if ($Runtime) { $path = Join-Path $path $Runtime }
    return Join-Path $path 'publish'
}

if (-not $SkipBuild) {
    Write-Host 'Building OpenDsc solution...' -ForegroundColor Cyan

    $version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version

    if ($IsWindows) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
        $serverProj = Join-Path $PSScriptRoot 'src\OpenDsc.Server\OpenDsc.Server.csproj'

        $builds = @()
        if (-not $Architecture -or $Architecture -eq 'x64') {
            $builds += @(
                @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0-windows'; Runtime = $null; SC = $false; SingleFile = $false; Tags = @('always') }
                @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0-windows'; Runtime = $null; SC = $false; SingleFile = $false; Tags = @('always') }
            )
        }
        foreach ($arch in @('x64', 'arm64')) {
            if ($Architecture -and $arch -ne $Architecture) { continue }
            $rid = "win-$arch"
            $lcmTags = if ($arch -eq 'x64') { @('always', 'portable') } else { @('portable') }
            $builds += @(
                @{ Name = 'Lcm'; Proj = $lcmProj; Framework = 'net10.0-windows'; Runtime = $rid; SC = $true; SingleFile = $false; Tags = $lcmTags; ZipName = "OpenDSC.Lcm.Windows.$arch-$version.zip" }
                @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0-windows'; Runtime = $rid; SC = $false; SingleFile = $false; Tags = @('portable'); ZipName = "OpenDSC.Resources.Windows.$arch.FrameworkDependent-$version.zip" }
                @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0-windows'; Runtime = $rid; SC = $true; SingleFile = $true; Tags = @('portable'); ZipName = "OpenDSC.Resources.Windows.$arch.SelfContained-$version.zip" }
                @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0-windows'; Runtime = $rid; SC = $false; SingleFile = $false; Tags = @('portable'); ZipName = "OpenDSC.Server.Windows.$arch.FrameworkDependent-$version.zip" }
                @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0-windows'; Runtime = $rid; SC = $true; SingleFile = $true; Tags = @('portable'); ZipName = "OpenDSC.Server.Windows.$arch.SelfContained-$version.zip" }
            )
        }

        foreach ($b in $builds) {
            if (-not (($b.Tags -contains 'always') -or ($Portable -and $b.Tags -contains 'portable'))) { continue }

            $scLabel = if ($b.SC) { 'self-contained' } else { 'framework-dependent' }
            $ridLabel = if ($b.Runtime) { " ($($b.Runtime), $scLabel)" } else { '' }
            Write-Host "  Building $($b.Name)$ridLabel..." -ForegroundColor Cyan

            $publishParams = @('publish', $b.Proj, '-c', $Configuration, '-f', $b.Framework, '-p:GenerateDocumentationFile=false')
            if ($b.Runtime) { $publishParams += '-r', $b.Runtime }
            $publishParams += if ($b.SC) { '--self-contained', 'true' } else { '--no-self-contained' }
            if ($b.SingleFile) {
                $publishParams += '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
                '-p:EnableCompressionInSingleFile=false', '-p:DebugType=None', '-p:DebugSymbols=false',
                '-p:CopyOutputSymbolsToPublishDirectory=false'
            }
            dotnet @publishParams
            if ($LASTEXITCODE -ne 0) { throw "Build failed for $($b.Name)$ridLabel with exit code $LASTEXITCODE" }
        }

        if ($Portable) {
            Write-Host 'Building portable packages for Windows...' -ForegroundColor Cyan
            $zipDir = Join-Path $PSScriptRoot 'artifacts\zip'
            New-Item -ItemType Directory -Path $zipDir -Force | Out-Null

            foreach ($b in $builds | Where-Object { $_.Tags -contains 'portable' -and $_.ZipName }) {
                $publishPath = Get-PublishPath -Proj $b.Proj -Configuration $Configuration -Framework $b.Framework -Runtime $b.Runtime
                $zipPath = Join-Path $zipDir $b.ZipName
                Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
                Write-Host "  Created: $zipPath" -ForegroundColor Green
            }

            Write-Host 'Windows portable packages built successfully!' -ForegroundColor Green
        }

        if ($Msi) {
            Write-Host 'Building MSI installer...' -ForegroundColor Cyan
            $wixProj = Join-Path $PSScriptRoot 'packaging\msi\OpenDsc.Resources\OpenDsc.Resources.wixproj'
            if (Test-Path $wixProj) {
                dotnet build $wixProj -c $Configuration
                if ($LASTEXITCODE -ne 0) { throw "Build failed for MSI installer with exit code $LASTEXITCODE" }
                $msiDir = Join-Path $PSScriptRoot 'artifacts\msi'
                Write-Host 'MSI installer built successfully!' -ForegroundColor Green
                Write-Host "Output location: $msiDir" -ForegroundColor Green
            }

            Write-Host 'Building LCM MSI installer...' -ForegroundColor Cyan
            $lcmWixProj = Join-Path $PSScriptRoot 'packaging\msi\OpenDsc.Lcm\OpenDsc.Lcm.wixproj'
            if (Test-Path $lcmWixProj) {
                dotnet build $lcmWixProj -c $Configuration
                if ($LASTEXITCODE -ne 0) { throw "Build failed for LCM MSI installer with exit code $LASTEXITCODE" }
                Write-Host 'LCM MSI installer built successfully!' -ForegroundColor Green
                Write-Host "Output location: $msiDir" -ForegroundColor Green
            }

            Write-Host 'Building Server MSI installer...' -ForegroundColor Cyan
            $serverWixProj = Join-Path $PSScriptRoot 'packaging\msi\OpenDsc.Server\OpenDsc.Server.wixproj'
            if (Test-Path $serverWixProj) {
                dotnet build $serverWixProj -c $Configuration
                if ($LASTEXITCODE -ne 0) { throw "Build failed for Server MSI installer with exit code $LASTEXITCODE" }
                Write-Host 'Server MSI installer built successfully!' -ForegroundColor Green
                Write-Host "Output location: $msiDir" -ForegroundColor Green
            }
        }

        if (-not $Architecture -or $Architecture -eq 'x64') {
            Write-Host 'Building TestService...' -ForegroundColor Cyan
            $testServiceProj = Join-Path $PSScriptRoot 'tests\TestService\TestService.csproj'
            if (Test-Path $testServiceProj) {
                $testServiceDir = Join-Path $PSScriptRoot 'artifacts\TestService'
                dotnet publish $testServiceProj -c $Configuration -o $testServiceDir -p:GenerateDocumentationFile=false
                if ($LASTEXITCODE -ne 0) { throw "Build failed for TestService with exit code $LASTEXITCODE" }
            }
        }
    } elseif ($IsLinux) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
        $serverProj = Join-Path $PSScriptRoot 'src\OpenDsc.Server\OpenDsc.Server.csproj'

        $builds = @(
            @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = $null; SC = $false; SingleFile = $false; Tags = @('always') }
        )
        foreach ($arch in @('x64', 'arm64')) {
            if ($Architecture -and $arch -ne $Architecture) { continue }
            $rid = "linux-$arch"
            $lcmTags = if ($arch -eq 'x64') { @('always', 'portable', 'package') } else { @('portable', 'package') }
            $builds += @(
                @{ Name = 'Lcm'; Proj = $lcmProj; Framework = 'net10.0'; Runtime = $rid; SC = $true; SingleFile = $false; Tags = $lcmTags; TarName = "OpenDSC.Lcm.Linux.$arch-$version.tar.gz"; PkgRole = 'Lcm' }
                @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = $rid; SC = $false; SingleFile = $false; Tags = @('portable', 'package'); TarName = "OpenDSC.Resources.Linux.$arch.FrameworkDependent-$version.tar.gz"; PkgRole = 'publish' }
                @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = $rid; SC = $true; SingleFile = $true; Tags = @('portable'); TarName = "OpenDSC.Resources.Linux.$arch.SelfContained-$version.tar.gz" }
                @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0'; Runtime = $rid; SC = $false; SingleFile = $false; Tags = @('portable', 'package'); TarName = "OpenDSC.Server.Linux.$arch.FrameworkDependent-$version.tar.gz"; PkgRole = 'Server' }
                @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0'; Runtime = $rid; SC = $true; SingleFile = $true; Tags = @('portable'); TarName = "OpenDSC.Server.Linux.$arch.SelfContained-$version.tar.gz" }
            )
        }

        foreach ($b in $builds) {
            if (-not (($b.Tags -contains 'always') -or ($Portable -and $b.Tags -contains 'portable') -or ($LinuxPackages -and $b.Tags -contains 'package'))) { continue }

            $scLabel = if ($b.SC) { 'self-contained' } else { 'framework-dependent' }
            $ridLabel = if ($b.Runtime) { " ($($b.Runtime), $scLabel)" } else { '' }
            Write-Host "  Building $($b.Name)$ridLabel..." -ForegroundColor Cyan

            $publishParams = @('publish', $b.Proj, '-c', $Configuration, '-f', $b.Framework, '-p:GenerateDocumentationFile=false')
            if ($b.Runtime) { $publishParams += '-r', $b.Runtime }
            $publishParams += if ($b.SC) { '--self-contained', 'true' } else { '--no-self-contained' }
            if ($b.SingleFile) {
                $publishParams += '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
                '-p:EnableCompressionInSingleFile=false', '-p:DebugType=None', '-p:DebugSymbols=false',
                '-p:CopyOutputSymbolsToPublishDirectory=false'
            }
            dotnet @publishParams
            if ($LASTEXITCODE -ne 0) { throw "Build failed for $($b.Name)$ridLabel with exit code $LASTEXITCODE" }

            # Generate manifest explicitly for base Resources build on Linux packaging
            if ($b.Proj -eq $resourcesProj -and -not $b.Runtime -and $LinuxPackages) {
                $publishPath = Get-PublishPath -Proj $b.Proj -Configuration $Configuration -Framework $b.Framework -Runtime $b.Runtime
                $exePath = Join-Path $publishPath 'OpenDsc.Resources'
                if (Test-Path $exePath) {
                    & $exePath manifest --save
                }
            }
        }

        if ($Portable) {
            Write-Host 'Building portable packages for Linux...' -ForegroundColor Cyan
            $tarDir = Join-Path $PSScriptRoot 'artifacts\tar'
            New-Item -ItemType Directory -Path $tarDir -Force | Out-Null

            foreach ($b in $builds | Where-Object { $_.Tags -contains 'portable' -and $_.TarName }) {
                $publishPath = Get-PublishPath -Proj $b.Proj -Configuration $Configuration -Framework $b.Framework -Runtime $b.Runtime
                $tarPath = Join-Path $tarDir $b.TarName
                tar -czf $tarPath -C $publishPath .
                Write-Host "  Created: $tarPath" -ForegroundColor Green
            }

            Write-Host 'Linux portable packages built successfully!' -ForegroundColor Green
        }

        if ($LinuxPackages) {
            foreach ($arch in @('x64', 'arm64')) {
                if ($Architecture -and $arch -ne $Architecture) { continue }
                $rid = "linux-$arch"
                $fpmArch = if ($arch -eq 'x64') { 'amd64' } else { 'arm64' }
                $pkgArtifactsDir = Join-Path $PSScriptRoot "artifacts\pkg-stage\$rid"
                New-Item -ItemType Directory -Path $pkgArtifactsDir -Force | Out-Null

                foreach ($b in $builds | Where-Object { $_.Runtime -eq $rid -and $_.PkgRole }) {
                    $publishPath = Get-PublishPath -Proj $b.Proj -Configuration $Configuration -Framework $b.Framework -Runtime $b.Runtime
                    Copy-Item -Recurse -Force $publishPath (Join-Path $pkgArtifactsDir $b.PkgRole)
                }

                $pkgOutputDir = Join-Path $PSScriptRoot "artifacts\packages\linux\$arch"
                New-Item -ItemType Directory -Path $pkgOutputDir -Force | Out-Null

                $buildPackagesScript = Join-Path $PSScriptRoot 'packaging\linux\build-packages.sh'
                $lcmPostinstall = Join-Path $PSScriptRoot 'packaging\linux\lcm\postinstall.sh'
                $lcmPreremove = Join-Path $PSScriptRoot 'packaging\linux\lcm\preremove.sh'
                $serverPostinstall = Join-Path $PSScriptRoot 'packaging\linux\server\postinstall.sh'
                $serverPreremove = Join-Path $PSScriptRoot 'packaging\linux\server\preremove.sh'
                bash -c "chmod +x '$buildPackagesScript' '$lcmPostinstall' '$lcmPreremove' '$serverPostinstall' '$serverPreremove'"

                bash $buildPackagesScript `
                    --version $version `
                    --artifacts-dir $pkgArtifactsDir `
                    --output-dir $pkgOutputDir `
                    --arch $fpmArch
                if ($LASTEXITCODE -ne 0) { throw "FPM packaging failed for $rid with exit code $LASTEXITCODE" }
            }

            Write-Host 'Linux packages (.deb/.rpm) built successfully!' -ForegroundColor Green
            Write-Host "Output: $(Join-Path $PSScriptRoot 'artifacts\packages\linux')" -ForegroundColor Green
        }
    } elseif ($IsMacOS) {
        $resourcesProj = Join-Path $PSScriptRoot 'src\OpenDsc.Resources\OpenDsc.Resources.csproj'
        $lcmProj = Join-Path $PSScriptRoot 'src\OpenDsc.Lcm\OpenDsc.Lcm.csproj'
        $serverProj = Join-Path $PSScriptRoot 'src\OpenDsc.Server\OpenDsc.Server.csproj'

        $builds = @(
            @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = $null; SC = $false; SingleFile = $false; Tags = @('always') }
            @{ Name = 'Lcm'; Proj = $lcmProj; Framework = 'net10.0'; Runtime = 'osx-arm64'; SC = $true; SingleFile = $false; Tags = @('always', 'portable'); TarName = "OpenDSC.Lcm.macOS.arm64-$version.tar.gz" }
            @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = 'osx-arm64'; SC = $false; SingleFile = $false; Tags = @('portable'); TarName = "OpenDSC.Resources.macOS.arm64.FrameworkDependent-$version.tar.gz" }
            @{ Name = 'Resources'; Proj = $resourcesProj; Framework = 'net10.0'; Runtime = 'osx-arm64'; SC = $true; SingleFile = $true; Tags = @('portable'); TarName = "OpenDSC.Resources.macOS.arm64.SelfContained-$version.tar.gz" }
            @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0'; Runtime = 'osx-arm64'; SC = $false; SingleFile = $false; Tags = @('portable'); TarName = "OpenDSC.Server.macOS.arm64.FrameworkDependent-$version.tar.gz" }
            @{ Name = 'Server'; Proj = $serverProj; Framework = 'net10.0'; Runtime = 'osx-arm64'; SC = $true; SingleFile = $true; Tags = @('portable'); TarName = "OpenDSC.Server.macOS.arm64.SelfContained-$version.tar.gz" }
        )

        foreach ($b in $builds) {
            if (-not (($b.Tags -contains 'always') -or ($Portable -and $b.Tags -contains 'portable'))) { continue }

            $scLabel = if ($b.SC) { 'self-contained' } else { 'framework-dependent' }
            $ridLabel = if ($b.Runtime) { " ($($b.Runtime), $scLabel)" } else { '' }
            Write-Host "  Building $($b.Name)$ridLabel..." -ForegroundColor Cyan

            $publishParams = @('publish', $b.Proj, '-c', $Configuration, '-f', $b.Framework, '-p:GenerateDocumentationFile=false')
            if ($b.Runtime) { $publishParams += '-r', $b.Runtime }
            $publishParams += if ($b.SC) { '--self-contained', 'true' } else { '--no-self-contained' }
            if ($b.SingleFile) {
                $publishParams += '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
                '-p:EnableCompressionInSingleFile=false', '-p:DebugType=None', '-p:DebugSymbols=false',
                '-p:CopyOutputSymbolsToPublishDirectory=false'
            }
            dotnet @publishParams
            if ($LASTEXITCODE -ne 0) { throw "Build failed for $($b.Name)$ridLabel with exit code $LASTEXITCODE" }
        }

        if ($Portable) {
            Write-Host 'Building portable packages for macOS...' -ForegroundColor Cyan
            $tarDir = Join-Path $PSScriptRoot 'artifacts\tar'
            New-Item -ItemType Directory -Path $tarDir -Force | Out-Null

            foreach ($b in $builds | Where-Object { $_.Tags -contains 'portable' -and $_.TarName }) {
                $publishPath = Get-PublishPath -Proj $b.Proj -Configuration $Configuration -Framework $b.Framework -Runtime $b.Runtime
                $tarPath = Join-Path $tarDir $b.TarName
                tar -czf $tarPath -C $publishPath .
                Write-Host "  Created: $tarPath" -ForegroundColor Green
            }

            Write-Host 'macOS portable packages built successfully!' -ForegroundColor Green
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

if ($SkipTest) {
    exit 0
}

$env:BUILD_CONFIGURATION = $Configuration

Write-Host 'Running tests...' -ForegroundColor Cyan

$testBuildArgs = if ($SkipBuild) { @() } else { @('--no-build') }
$coverageArgs = if ($CollectCoverage) { @('--collect', 'XPlat Code Coverage') } else { @() }

if (-not $SkipUnitTests) {
    Write-Host 'Running unit tests...' -ForegroundColor Cyan
    dotnet test --configuration $Configuration @testBuildArgs @coverageArgs --filter 'Category=Unit' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Unit tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Unit tests passed!' -ForegroundColor Green
}

if (-not $SkipIntegrationTests) {
    if ($InstallSqlServer -or $env:GITHUB_ACTIONS) {
        Write-Host 'Preparing SQL Server for integration tests...' -ForegroundColor Cyan
        . (Join-Path $PSScriptRoot 'tools\Install-SqlServer.ps1')
        $sqlServerAvailable = Initialize-SqlServerForTests
        if (-not $sqlServerAvailable) {
            Write-Warning 'SQL Server is not available. SQL Server integration tests may be skipped when no database instance is present.'
        }
    }

    Write-Host 'Running integration tests...' -ForegroundColor Cyan
    dotnet test --configuration $Configuration @testBuildArgs @coverageArgs --filter 'Category=Integration' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Integration tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Integration tests passed!' -ForegroundColor Green
}

if (-not $SkipFunctionalTests) {
    Write-Host 'Running functional tests (cross-database provider tests)...' -ForegroundColor Cyan
    Write-Host 'Note: This requires Docker to be running for SQL Server and PostgreSQL containers.' -ForegroundColor Yellow
    dotnet test --configuration $Configuration @testBuildArgs @coverageArgs --filter 'Category=Functional' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Functional tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host 'Functional tests passed!' -ForegroundColor Green
}

