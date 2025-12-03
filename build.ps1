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
.EXAMPLE
    .\build.ps1
    Builds and tests the solution in Release configuration.
.EXAMPLE
    .\build.ps1 -Configuration Debug
    Builds and tests the solution in Debug configuration.
.EXAMPLE
    .\build.ps1 SkipTest
    Builds the solution without running tests.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipBuild,

    [switch] $SkipTest
)

$ErrorActionPreference = 'Stop'

if (-not $SkipBuild) {
    dotnet build "$PSScriptRoot\src" --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    dotnet publish "$PSScriptRoot\tests\TestResource.Aot\TestResource.Aot.csproj" --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Publish TestResource.Aot failed with exit code $LASTEXITCODE"
    }

    dotnet publish "$PSScriptRoot\tests\TestResource.NonAot\TestResource.NonAot.csproj" --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Publish TestResource.NonAot failed with exit code $LASTEXITCODE"
    }

    dotnet publish "$PSScriptRoot\tests\TestResource.Options\TestResource.Options.csproj" --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Publish TestResource.Options failed with exit code $LASTEXITCODE"
    }
}

if ($SkipTest) {
    exit 0
}

$env:BUILD_CONFIGURATION = $Configuration

Invoke-Pester
