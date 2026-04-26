---
document type: cmdlet
external help file: OpenDsc.Authoring-Help.xml
HelpUri: ''
Locale: en-US
Module Name: OpenDsc.Authoring
ms.date: 04/04/2026
PlatyPS schema version: 2024-05-01
title: ConvertFrom-DscConfigurationMof
---

<!-- markdownlint-disable MD025 -->

# ConvertFrom-DscConfigurationMof

## SYNOPSIS

Converts DSC v1 compiled MOF text to a DSC v3 configuration document.

## SYNTAX

```powershell
ConvertFrom-DscConfigurationMof -InputObject <string> [-AsJson] [<CommonParameters>]
```

## DESCRIPTION

Reads the text of a DSC v1 compiled MOF file and converts it to a DSC v3
`DscConfigDocument` object. Each resource instance in the MOF becomes a resource
entry in the configuration document. Use the `-AsJson` switch to receive the
output as a JSON string instead of an object.

Pipe the output of `Get-Content` on a compiled `.mof` file to this cmdlet.

## EXAMPLES

### Example 1 - Convert a compiled MOF to a configuration object

```powershell
Get-Content -Path ./localhost.mof | ConvertFrom-DscConfigurationMof
```

Reads a compiled MOF file and returns a `DscConfigDocument` object.

### Example 2 - Convert a compiled MOF to JSON

```powershell
Get-Content -Path ./localhost.mof | ConvertFrom-DscConfigurationMof -AsJson
```

Reads a compiled MOF file and returns the DSC v3 configuration as a JSON string.

### Example 3 - Save the converted configuration to a YAML file

```powershell
Get-Content -Path ./localhost.mof |
    ConvertFrom-DscConfigurationMof -AsJson |
    Set-Content -Path ./configuration.dsc.json
```

Converts a compiled MOF to JSON and writes it to a file.

## PARAMETERS

### -InputObject

The MOF text to convert. Pipe the output of `Get-Content` on a compiled `.mof`
file.

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

### -AsJson

If set, output JSON text instead of a `DscConfigDocument` object.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: 'False'
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

### System.String

The compiled MOF text, typically piped from `Get-Content`.

## OUTPUTS

### OpenDsc.Schema.DscConfigDocument

When `-AsJson` is not specified, returns a `DscConfigDocument` object
representing the DSC v3 configuration.

### System.String

When `-AsJson` is specified, returns the configuration as a JSON string.

## NOTES

## RELATED LINKS

- [ConvertFrom-DscSchemaMof](ConvertFrom-DscSchemaMof.md)
