---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: New-DscResourceManifest
---

<!-- markdownlint-disable MD025 -->

# New-DscResourceManifest

## SYNOPSIS

Creates a DSC resource manifests list for bundling multiple resources in a single file.

## SYNTAX

```powershell
New-DscResourceManifest [-AdaptedResource <DscAdaptedResourceManifest[]>] [-Resource <OrderedDictionary[]>] [<CommonParameters>]
```

## DESCRIPTION

Builds a `DscResourceManifestList` object that can contain both adapted
resources and command-based resources. The resulting object can be serialized
to JSON and written to a `.dsc.manifests.json` file, which DSCv3 discovers
and loads as a bundle.

Adapted resources can be added by piping `DscAdaptedResourceManifest` objects
from `New-DscAdaptedResourceManifest`. Command-based resources can be added via
the `-Resource` parameter as hashtables matching the DSCv3 resource manifest
schema.

## EXAMPLES

### Example 1 - Create a manifests list from adapted resources

```powershell
$adapted = New-DscAdaptedResourceManifest -Path ./MyModule/MyModule.psd1
New-DscResourceManifest -AdaptedResource $adapted
```

Creates a manifests list from adapted resource manifests generated from a
module.

### Example 2 - Create a manifests list with a command-based resource

```powershell
$resource = @{
    '$schema'  = 'https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json'
    type       = 'MyCompany/MyTool'
    version    = '1.0.0'
    get        = @{ executable = 'mytool'; args = @('get') }
    set        = @{ executable = 'mytool'; args = @('set'); implementsPretest = $false; return = 'state' }
    test       = @{ executable = 'mytool'; args = @('test'); return = 'state' }
    exitCodes  = @{ '0' = 'Success'; '1' = 'Error' }
    schema     = @{ command = @{ executable = 'mytool'; args = @('schema') } }
}
New-DscResourceManifest -Resource $resource
```

Creates a manifests list containing a single command-based resource.

### Example 3 - Combine adapted and command-based resources

```powershell
$adapted = New-DscAdaptedResourceManifest -Path ./MyModule/MyModule.psd1
$resource = @{
    '$schema' = 'https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json'
    type      = 'MyCompany/MyTool'
    version   = '1.0.0'
    get       = @{ executable = 'mytool'; args = @('get') }
}
New-DscResourceManifest -AdaptedResource $adapted -Resource $resource
```

Creates a manifests list combining both adapted and command-based resources.

### Example 4 - Pipeline adapted resources directly

```powershell
New-DscAdaptedResourceManifest -Path ./MyModule/MyModule.psd1 |
    New-DscResourceManifest
```

Pipes adapted resource manifests directly into the cmdlet via the pipeline.

## PARAMETERS

### -AdaptedResource

One or more `DscAdaptedResourceManifest` objects to include in the manifests
list. These are typically produced by `New-DscAdaptedResourceManifest`.

```yaml
Type: OpenDsc.Authoring.Commands.DscAdaptedResourceManifest[]
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Resource

One or more hashtables representing command-based DSC resource manifests. Each
hashtable should conform to the DSCv3 resource manifest schema with keys such as
`$schema`, `type`, `version`, `get`, `set`, `test`, `schema`, etc.

```yaml
Type: System.Collections.Specialized.OrderedDictionary[]
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

## INPUTS

### OpenDsc.Authoring.Commands.DscAdaptedResourceManifest

Accepts `DscAdaptedResourceManifest` objects via the pipeline.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscResourceManifestList

Returns a `DscResourceManifestList` object with a `.ToJson()` method for
serialization to the `.dsc.manifests.json` format.

## NOTES

## RELATED LINKS

- [New-DscAdaptedResourceManifest](New-DscAdaptedResourceManifest.md)
- [Import-DscResourceManifest](Import-DscResourceManifest.md)
