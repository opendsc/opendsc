---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: Import-DscResourceManifest
---

<!-- markdownlint-disable MD025 -->

# Import-DscResourceManifest

## SYNOPSIS

Imports a DSC resource manifest list from a `.dsc.manifests.json` file.

## SYNTAX

```powershell
Import-DscResourceManifest -Path <string> [<CommonParameters>]
```

## DESCRIPTION

Reads a `.dsc.manifests.json` file and returns a `DscResourceManifestList`
object containing the adapted resources, command-based resources, and extensions
defined in the file. This is the inverse of serializing a manifest list with
`.ToJson()`.

The adapted resources in the returned list are hydrated into
`DscAdaptedResourceManifest` objects. Resources and extensions are stored as
hashtables.

## EXAMPLES

### Example 1 - Import a manifest list

```powershell
Import-DscResourceManifest -Path ./MyModule.dsc.manifests.json
```

Imports a manifest list file and returns a `DscResourceManifestList` object.

### Example 2 - Import all manifest lists in a directory

```powershell
Get-ChildItem -Filter *.dsc.manifests.json | Import-DscResourceManifest
```

Imports all manifest list files in the current directory.

### Example 3 - Inspect the adapted resources count

```powershell
$list = Import-DscResourceManifest -Path ./existing.dsc.manifests.json
$list.AdaptedResources.Count
```

Imports a manifest list and inspects the number of adapted resources.

## PARAMETERS

### -Path

The path to a `.dsc.manifests.json` file. Accepts pipeline input.

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

The path to a `.dsc.manifests.json` file, typically piped from `Get-ChildItem`.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscResourceManifestList

Returns a `DscResourceManifestList` object with `.ToJson()` for serialization.

## NOTES

## RELATED LINKS

- [New-DscResourceManifest](New-DscResourceManifest.md)
- [Import-DscAdaptedResourceManifest](Import-DscAdaptedResourceManifest.md)
