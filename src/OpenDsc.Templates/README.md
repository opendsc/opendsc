# OpenDsc.Templates

The `OpenDsc.Templates` package contains .NET project templates for Microsoft
DSC v3 resources.

## Install

```powershell
dotnet new install OpenDsc.Templates
```

## List Templates

```text
dotnet new list

Template Name              Short Name  Language  Tags
-------------------------  ----------  --------  ------------
DSC Resource               dsc         [C#]      DSC/Resource
```

## Create Project

Use the `dsc` template to create a new DSC resource project.

```powershell
dotnet new dsc
```

### Template Parameters

The template supports the following parameters:

- `--aot`: Enable native AOT (default: false)
- `--use-options`: Use JsonSerializerOptions pattern (default: false)
- `--resource-name`: DSC resource type name (default: "InsertOwner/Resource")
- `--resource-description`: Resource description (default: "Insert description")

Example for creating an AOT-enabled resource:

```powershell
dotnet new dsc --aot true --resource-name "MyCompany/MyResource" --resource-description "My custom DSC resource"
```
