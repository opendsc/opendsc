#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates Homebrew formula files by substituting version and SHA256 hashes into templates.
.PARAMETER Version
    The release version string (e.g. 0.5.1).
.PARAMETER ResourcesSha256
    The SHA256 hash of OpenDSC.Resources.macOS.arm64.SelfContained-<version>.tar.gz.
.PARAMETER LcmSha256
    The SHA256 hash of OpenDSC.Lcm.macOS.arm64-<version>.tar.gz.
.PARAMETER ResourcesUrl
    URL for the Resources tarball. Defaults to the GitHub Releases URL for the given version.
    Use a file:// URL to test with a locally built tarball.
.PARAMETER LcmUrl
    URL for the LCM tarball. Defaults to the GitHub Releases URL for the given version.
    Use a file:// URL to test with a locally built tarball.
.PARAMETER OutputDir
    Directory to write the rendered formula .rb files. Defaults to artifacts/homebrew.
.EXAMPLE
    .\generate-formula.ps1 -Version 0.5.1 -ResourcesSha256 abc123 -LcmSha256 def456
#>
param(
    [Parameter(Mandatory)]
    [string] $Version,

    [Parameter(Mandatory)]
    [string] $ResourcesSha256,

    [Parameter(Mandatory)]
    [string] $LcmSha256,

    [string] $ResourcesUrl = 'https://github.com/opendsc/opendsc/releases/download/v{{VERSION}}/OpenDSC.Resources.macOS.arm64.SelfContained-{{VERSION}}.tar.gz',

    [string] $LcmUrl = 'https://github.com/opendsc/opendsc/releases/download/v{{VERSION}}/OpenDSC.Lcm.macOS.arm64-{{VERSION}}.tar.gz',

    [string] $OutputDir = (Join-Path $PSScriptRoot '..\..\artifacts\homebrew')
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$templateDir = $PSScriptRoot

$templates = @(
    @{
        Template      = Join-Path $templateDir 'opendsc-resources.rb.template'
        Output        = Join-Path $OutputDir 'opendsc-resources.rb'
        Substitutions = @{
            '{{VERSION}}'          = $Version
            '{{RESOURCES_URL}}'    = $ResourcesUrl.Replace('{{VERSION}}', $Version)
            '{{RESOURCES_SHA256}}' = $ResourcesSha256
        }
    },
    @{
        Template      = Join-Path $templateDir 'opendsc-lcm.rb.template'
        Output        = Join-Path $OutputDir 'opendsc-lcm.rb'
        Substitutions = @{
            '{{VERSION}}'    = $Version
            '{{LCM_URL}}'    = $LcmUrl.Replace('{{VERSION}}', $Version)
            '{{LCM_SHA256}}' = $LcmSha256
        }
    }
)

foreach ($entry in $templates) {
    $content = Get-Content -Raw $entry.Template
    foreach ($key in $entry.Substitutions.Keys) {
        $content = $content.Replace($key, $entry.Substitutions[$key])
    }
    Set-Content -Path $entry.Output -Value $content -NoNewline
    Write-Host "Generated: $($entry.Output)" -ForegroundColor Green
}
