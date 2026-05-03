---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: Import-DscAdaptedResourceManifest
---

<!-- markdownlint-disable MD025 -->

# Import-DscAdaptedResourceManifest

## SYNOPSIS

Imports adapted resource manifest objects from `.dsc.adaptedResource.json`
files.

## SYNTAX

```powershell
Import-DscAdaptedResourceManifest -Path <string> [<CommonParameters>]
```

## DESCRIPTION

Reads one or more `.dsc.adaptedResource.json` files and returns
`DscAdaptedResourceManifest` objects. This is the inverse of serializing a
manifest with `.ToJson()` — it allows you to load existing adapted resource
manifests for inspection, modification, or inclusion in a resource manifest list
via `New-DscResourceManifest`.

## EXAMPLES

### Example 1 - Import a single manifest

```powershell
Import-DscAdaptedResourceManifest -Path ./MyResource.dsc.adaptedResource.json
```

Imports a single adapted resource manifest and returns a
`DscAdaptedResourceManifest` object.

### Example 2 - Import all manifests in a directory

```powershell
Get-ChildItem -Filter *.dsc.adaptedResource.json | Import-DscAdaptedResourceManifest
```

Imports all adapted resource manifest files in the current directory.

### Example 3 - Import and bundle into a manifest list

```powershell
Import-DscAdaptedResourceManifest -Path ./MyResource.dsc.adaptedResource.json |
    New-DscResourceManifest
```

Imports an adapted resource manifest and bundles it into a resource manifest list.

## PARAMETERS

### -Path

The path to a `.dsc.adaptedResource.json` file. Accepts pipeline input.

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

The path to a `.dsc.adaptedResource.json` file, typically piped from
`Get-ChildItem`.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscAdaptedResourceManifest

Returns a `DscAdaptedResourceManifest` object for each file. The object has
`.ToJson()` and `.ToHashtable()` methods for serialization.

## NOTES

## RELATED LINKS

- [New-DscAdaptedResourceManifest](New-DscAdaptedResourceManifest.md)
- [New-DscResourceManifest](New-DscResourceManifest.md)
