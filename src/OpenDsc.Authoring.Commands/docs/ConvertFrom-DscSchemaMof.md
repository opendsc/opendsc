---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: ConvertFrom-DscSchemaMof
---

<!-- markdownlint-disable MD025 -->

# ConvertFrom-DscSchemaMof

## SYNOPSIS

Converts a MOF schema class definition to a JSON Schema string.

## SYNTAX

```powershell
ConvertFrom-DscSchemaMof -InputObject <string> [<CommonParameters>]
```

## DESCRIPTION

Reads the text of a DSC v1 `.schema.mof` file and converts each class definition
to a JSON Schema document. The output is a JSON string that describes the
resource properties, types, and metadata from the original MOF schema.

Pipe the output of `Get-Content` on a `.schema.mof` file to this cmdlet.

## EXAMPLES

### Example 1 - Convert a schema MOF to JSON Schema

```powershell
Get-Content -Path ./MSFT_xWebsite.schema.mof | ConvertFrom-DscSchemaMof
```

Reads a schema MOF file and returns the equivalent JSON Schema as a string.

### Example 2 - Save the JSON Schema to a file

```powershell
Get-Content -Path ./MSFT_xWebsite.schema.mof |
    ConvertFrom-DscSchemaMof |
    Set-Content -Path ./MSFT_xWebsite.schema.json
```

Converts a schema MOF and writes the JSON Schema to a file.

## PARAMETERS

### -InputObject

The MOF schema text to convert. Pipe the output of `Get-Content` on a
`.schema.mof` file.

```yaml
Type: System.String
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

## INPUTS

### System.String

The MOF schema text, typically piped from `Get-Content`.

## OUTPUTS

### System.String

Returns a JSON Schema string representing the converted MOF schema.

## NOTES

## RELATED LINKS

- [ConvertFrom-DscConfigurationMof](ConvertFrom-DscConfigurationMof.md)
