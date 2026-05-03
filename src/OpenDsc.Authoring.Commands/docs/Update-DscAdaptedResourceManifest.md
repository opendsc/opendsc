---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: Update-DscAdaptedResourceManifest
---

<!-- markdownlint-disable MD025 -->

# Update-DscAdaptedResourceManifest

## SYNOPSIS

Applies post-processing overrides to adapted resource manifest objects.

## SYNTAX

```powershell
Update-DscAdaptedResourceManifest -InputObject <DscAdaptedResourceManifest> [-PropertyOverride <DscPropertyOverride[]>] [-Description <string>] [<CommonParameters>]
```

## DESCRIPTION

Modifies the embedded JSON schema of a `DscAdaptedResourceManifest` object by
applying property-level overrides. This enables customization that AST
extraction alone cannot provide, such as meaningful property descriptions, JSON
schema keywords like `anyOf` or `oneOf` for complex type unions, default values,
numeric ranges, and string patterns.

Property overrides are specified via `DscPropertyOverride` objects that target
individual properties by name. Each override can change the description, title,
required status, remove existing JSON schema keys, and merge in new JSON schema
keywords.

## EXAMPLES

### Example 1 - Override a property description

```powershell
New-DscAdaptedResourceManifest -Path ./MyModule/MyModule.psd1 |
    Update-DscAdaptedResourceManifest -PropertyOverride @(
        New-DscPropertyOverride -Name 'Name' -Description 'The unique name identifying this resource instance.'
    )
```

Overrides the auto-generated description for the `Name` property.

### Example 2 - Replace a property schema with anyOf

```powershell
$overrides = @(
    New-DscPropertyOverride -Name 'Status' -Description 'The desired status, as a label or numeric code.' -RemoveKeys 'type','enum' -JsonSchema @{
        anyOf = @(
            @{ type = 'string'; enum = @('Active', 'Inactive') }
            @{ type = 'integer'; minimum = 0 }
        )
    }
)
New-DscAdaptedResourceManifest -Path ./MyModule.psd1 |
    Update-DscAdaptedResourceManifest -PropertyOverride $overrides
```

Replaces a simple enum property with an `anyOf` schema allowing either a string
enum or an integer value.

### Example 3 - Add numeric constraints and a default value

```powershell
$override = New-DscPropertyOverride -Name 'Count' -JsonSchema @{ minimum = 0; maximum = 100; default = 1 }
$manifest | Update-DscAdaptedResourceManifest -PropertyOverride $override
```

Adds numeric constraints and a default value to an existing integer property.

### Example 4 - Remove a property from the required list

```powershell
$override = New-DscPropertyOverride -Name 'Tags' -Required $false
$manifest | Update-DscAdaptedResourceManifest -PropertyOverride $override
```

Removes a property from the required list.

## PARAMETERS

### -InputObject

A `DscAdaptedResourceManifest` object to update. Typically produced by
`New-DscAdaptedResourceManifest`. Accepts pipeline input.

```yaml
Type: OpenDsc.Authoring.Commands.DscAdaptedResourceManifest
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PropertyOverride

One or more `DscPropertyOverride` objects specifying modifications to individual
properties in the embedded JSON schema. Each override targets a property by
`Name`.

```yaml
Type: OpenDsc.Authoring.Commands.DscPropertyOverride[]
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

### -Description

Override the resource-level description on both the manifest object and the
embedded JSON schema.

```yaml
Type: System.String
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

### OpenDsc.Authoring.Commands.DscAdaptedResourceManifest

Returns the modified `DscAdaptedResourceManifest` object.

## NOTES

## RELATED LINKS

- [New-DscAdaptedResourceManifest](New-DscAdaptedResourceManifest.md)
- [New-DscPropertyOverride](New-DscPropertyOverride.md)
