<#
    .SYNOPSIS
        Compiles PlatyPS v2 Markdown help files to MAML XML for the OpenDsc.Authoring module.

    .DESCRIPTION
        Uses Microsoft.PowerShell.PlatyPS to convert the Markdown command help files in the
        docs/ folder to a MAML XML file placed in en-US/ for Get-Help to discover.

        Run this script after editing any Markdown help file in the docs/ folder.

    .EXAMPLE
        .\Build-Help.ps1

        Compiles all Markdown help files to MAML XML in the en-US/ output folder.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$docsPath = Join-Path $PSScriptRoot 'docs'
$outputPath = Join-Path $PSScriptRoot 'en-US'
$tempPath = Join-Path $PSScriptRoot '.maml-temp'

if (-not (Get-Module -ListAvailable -Name Microsoft.PowerShell.PlatyPS))
{
    Write-Host 'Installing Microsoft.PowerShell.PlatyPS module...'
    Install-Module -Name Microsoft.PowerShell.PlatyPS -Scope CurrentUser -Force
}

Import-Module Microsoft.PowerShell.PlatyPS -Force

if (Test-Path $tempPath)
{
    Remove-Item -Path $tempPath -Recurse -Force
}

Write-Host "Compiling Markdown help from '$docsPath'..."

Measure-PlatyPSMarkdown -Path (Join-Path $docsPath '*.md') |
Where-Object Filetype -Match 'CommandHelp' |
Import-MarkdownCommandHelp -Path { $_.FilePath } |
Export-MamlCommandHelp -OutputFolder $tempPath -Force

# PlatyPS exports to a <ModuleName> subfolder — copy the XML directly to en-US/.
if (-not (Test-Path $outputPath))
{
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
}

Get-ChildItem -Path $tempPath -Filter '*.xml' -Recurse | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $outputPath -Force
    Write-Host "  Copied $($_.Name) to en-US/"
}

Remove-Item -Path $tempPath -Recurse -Force

Write-Host "MAML help written to '$outputPath'."
