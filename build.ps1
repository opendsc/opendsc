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
    Skip running tests after building.
.PARAMETER Pack
    Pack NuGet packages after building.
.PARAMETER InstallDsc
    Install DSC CLI before running tests.
.PARAMETER GitHubToken
    GitHub token for API authentication to avoid rate limiting.
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
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipBuild,

    [switch] $SkipTest,

    [switch] $Pack,

    [switch] $InstallDsc,

    [string] $GitHubToken
)

$ErrorActionPreference = 'Stop'

if (-not $SkipBuild) {
    dotnet publish $PSScriptRoot --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

if ($Pack) {
    dotnet pack $PSScriptRoot --configuration $Configuration --output "$PSScriptRoot\packages"

    if ($LASTEXITCODE -ne 0) {
        throw "Pack failed with exit code $LASTEXITCODE"
    }
}

if ($InstallDsc) {
    if (Get-Command dsc -ErrorAction SilentlyContinue) {
        Write-Host "DSC is already installed."
    } else {
        $headers = if ($GitHubToken) { @{Authorization = "Bearer $GitHubToken"} } else { $null }
        $release = Invoke-RestMethod -Uri 'https://api.github.com/repos/PowerShell/DSC/releases/latest' -Headers $headers
        $tag = $release.tag_name
        $version = $tag -replace '^v', ''
        Write-Host "Latest DSC version: $version"

        if ($IsWindows) {
            $platform = "x86_64-pc-windows-msvc"
            $extension = "zip"
        } elseif ($IsLinux) {
            $platform = "x86_64-linux"
            $extension = "tar.gz"
        } elseif ($IsMacOS) {
            $platform = "aarch64-apple-darwin"
            $extension = "tar.gz"
        }

        $url = "https://github.com/PowerShell/DSC/releases/download/$tag/DSC-$version-$platform.$extension"
        $archive = "dsc.$extension"

        Invoke-WebRequest -Uri $url -OutFile $archive
        New-Item -ItemType Directory -Path ./dsc -Force | Out-Null

        if ($extension -eq "zip") {
            Expand-Archive -Path $archive -DestinationPath ./dsc
        } else {
            tar -xzf $archive -C ./dsc
            if (-not $IsWindows) {
                chmod +x ./dsc/dsc
            }
        }

        Remove-Item $archive

        $pathSeparator = if ($IsWindows) { ";" } else { ":" }
        $env:PATH += "$pathSeparator$($PSScriptRoot)/dsc"
    }

    $dscVersion = dsc --version
    Write-Host "Installed DSC version: $dscVersion"
}

if (-not $IsWindows) {
    $testExecutables = @(
        "tests/TestResource.Aot/bin/Release/net10.0/publish/test-resource-aot",
        "tests/TestResource.NonAot/bin/Release/net10.0/publish/test-resource-non-aot",
        "tests/TestResource.Options/bin/Release/net10.0/publish/test-resource-options"
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

Invoke-Pester
