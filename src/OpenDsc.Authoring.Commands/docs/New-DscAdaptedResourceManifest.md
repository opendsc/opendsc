---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: New-DscAdaptedResourceManifest
---

<!-- markdownlint-disable MD025 -->

# New-DscAdaptedResourceManifest

## SYNOPSIS

Creates adapted resource manifest objects from class-based PowerShell DSC
resources.

## SYNTAX

```powershell
New-DscAdaptedResourceManifest -Path <string> [<CommonParameters>]
```

## DESCRIPTION

Parses the AST of a PowerShell file (`.ps1`, `.psm1`, or `.psd1`) to find
class-based DSC resources decorated with the `[DscResource()]` attribute. For
each resource found, it returns a `DscAdaptedResourceManifest` object that
complies with the DSCv3 adapted resource manifest JSON schema.

The returned objects can be serialized to JSON using the `.ToJson()` method and
written to `.dsc.adaptedResource.json` files. These manifests enable DSCv3 to
discover and use PowerShell DSC resources without running
`Invoke-DscCacheRefresh`.

When a `.psd1` is provided, the RootModule is resolved and parsed automatically.

## EXAMPLES

### Example 1 - Generate manifests from a module manifest

```powershell
New-DscAdaptedResourceManifest -Path ./MyModule/MyModule.psd1
```

Returns adapted resource manifest objects for all class-based DSC resources in
the module.

### Example 2 - Write each manifest to a JSON file

```powershell
New-DscAdaptedResourceManifest -Path ./MyResource.ps1 | ForEach-Object {
    $_.ToJson() | Set-Content "$($_.Type -replace '/', '.').dsc.adaptedResource.json"
}
```

Generates manifest objects and writes each to a JSON file.

### Example 3 - Discover and process all modules recursively

```powershell
Get-ChildItem -Path ./MyModules -Filter *.psd1 -Recurse | New-DscAdaptedResourceManifest
```

Discovers all module manifests under `./MyModules` and pipes them into the
cmdlet to generate adapted resource manifests for every class-based DSC resource
found.

## PARAMETERS

### -Path

The path to a `.ps1`, `.psm1`, or `.psd1` file containing class-based DSC
resources. When a `.psd1` is provided, the RootModule is resolved and parsed
automatically.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- FullName
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

## INPUTS

### System.String

The path to a PowerShell file, typically piped from `Get-ChildItem`.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscAdaptedResourceManifest

Returns a `DscAdaptedResourceManifest` object for each class-based DSC resource
found. The object has a `.ToJson()` method for serialization to the adapted
resource manifest JSON format.

## NOTES

## RELATED LINKS

- [New-DscResourceManifest](New-DscResourceManifest.md)
- [Update-DscAdaptedResourceManifest](Update-DscAdaptedResourceManifest.md)
- [Import-DscAdaptedResourceManifest](Import-DscAdaptedResourceManifest.md)
