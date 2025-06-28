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
DSC Resource (native AOT)  dsc-aot     [C#]      DSC/Resource
```

## Create Project

If you need to create a native AOT or a self-contained trimmed application use
the `dsc-aot` template.

```powershell
dotnet new dsc-aot
```

Otherwise use the `dsc` template.

```powershell
dotnet new dsc
```
