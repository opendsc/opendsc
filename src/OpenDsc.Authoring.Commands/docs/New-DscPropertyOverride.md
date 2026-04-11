---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: New-DscPropertyOverride
---

<!-- markdownlint-disable MD025 -->

# New-DscPropertyOverride

## SYNOPSIS

Creates a DscPropertyOverride object for use with
Update-DscAdaptedResourceManifest.

## SYNTAX

```powershell
New-DscPropertyOverride -Name <string> [-Description <string>] [-Title <string>] [-JsonSchema <OrderedDictionary>] [-RemoveKeys <string[]>] [-Required <bool>] [<CommonParameters>]
```

## DESCRIPTION

Constructs a `DscPropertyOverride` object that specifies how to modify a single
property in the embedded JSON schema of an adapted resource manifest. Use this
with `Update-DscAdaptedResourceManifest` to customize property descriptions,
titles, JSON schema keywords, and required status.

## EXAMPLES

### Example 1 - Override a property description

```powershell
New-DscPropertyOverride -Name 'Enabled' -Description 'Whether this resource is active.'
```

Creates an override that sets a custom description for the `Enabled` property.

### Example 2 - Replace type/enum with anyOf

```powershell
New-DscPropertyOverride -Name 'Status' -RemoveKeys 'type','enum' -JsonSchema @{
    anyOf = @(
        @{ type = 'string'; enum = @('Active', 'Inactive') }
        @{ type = 'integer'; minimum = 0 }
    )
}
```

Creates an override that replaces the `type`/`enum` with an `anyOf` schema.

### Example 3 - Create multiple overrides and apply them

```powershell
$overrides = @(
    New-DscPropertyOverride -Name 'Name' -Description 'The unique identifier.'
    New-DscPropertyOverride -Name 'Count' -JsonSchema @{ minimum = 0; maximum = 100 }
)
$manifest | Update-DscAdaptedResourceManifest -PropertyOverride $overrides
```

Creates multiple overrides and pipes them to
`Update-DscAdaptedResourceManifest`.

## PARAMETERS

### -Name

The name of the property in the embedded JSON schema to override.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Description

Override the property description text.

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

### -Title

Override the property title text.

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

### -JsonSchema

A hashtable of JSON schema keywords to merge into the property definition (e.g.,
`anyOf`, `oneOf`, `default`, `minimum`, `maximum`, `pattern`, `format`).

```yaml
Type: System.Collections.Specialized.OrderedDictionary
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

### -RemoveKeys

An array of JSON schema key names to remove from the property before merging
`JsonSchema` (e.g., `'type'`, `'enum'` when replacing with `anyOf`).

```yaml
Type: System.String[]
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

### -Required

Set to `$true` to add the property to the required list, `$false` to remove it,
or omit to leave unchanged.

```yaml
Type: System.Nullable[System.Boolean]
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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### OpenDsc.Authoring.Commands.DscPropertyOverride

Returns a `DscPropertyOverride` object.

## NOTES

## RELATED LINKS

- [Update-DscAdaptedResourceManifest](Update-DscAdaptedResourceManifest.md)
