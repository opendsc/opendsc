# OpenDsc.Windows/Shortcut

## Synopsis

Manages Windows shortcuts (.lnk files).

## Description

The `OpenDsc.Windows/Shortcut` resource enables you to create, read, update,
and delete Windows shortcuts (.lnk files). It supports configuring target
paths, command-line arguments, working directories, descriptions, icon
locations, hotkeys, and window styles.

This resource uses the Windows Shell Link COM interface (`IShellLinkW`)
through properly-defined P/Invoke declarations instead of dynamic COM
interop. While the code structure is AOT-compatible (using source generators
for JSON serialization and proper COM interface definitions), full Native AOT
compilation is not supported due to limitations with COM class instantiation
in NativeAOT. The resource works perfectly in standard .NET runtime with JIT
compilation.

## Requirements

- Windows operating system
- .NET 10.0 runtime

## Capabilities

- **get** - Query shortcut properties
- **set** - Create or update shortcuts
- **delete** - Remove shortcuts

## Properties

### Required Properties

- **path** (string) - Full path to the shortcut file (.lnk)

### Optional Properties

- **targetPath** (string) - Full path to the target executable or file
- **arguments** (string) - Command-line arguments to pass to the target
- **workingDirectory** (string) - Working directory for the target
- **description** (string) - Description/comment for the shortcut
- **iconLocation** (string) - Icon path and index (format: "path,index")
- **hotkey** (string) - Hotkey combination (format: "MODIFIER+KEY",
  e.g., "CTRL+ALT+F")
- **windowStyle** (enum) - Window state when launched. Valid values:
  `Normal`, `Minimized`, `Maximized`
- **_exist** (boolean) - Whether the shortcut should exist. Default: `true`

## Examples

### Get shortcut properties

```powershell
$config = @'
path: C:\Users\Public\Desktop\Notepad.lnk
'@

dsc resource get -r OpenDsc.Windows/Shortcut -i $config
```

### Create a basic shortcut

```powershell
$config = @'
path: C:\Users\Public\Desktop\Notepad.lnk
targetPath: C:\Windows\System32\notepad.exe
description: Text Editor
'@

dsc resource set -r OpenDsc.Windows/Shortcut -i $config
```

### Create a shortcut with arguments and custom settings

```powershell
$config = @'
path: C:\Users\Public\Desktop\Command Prompt.lnk
targetPath: C:\Windows\System32\cmd.exe
arguments: /k echo Hello
workingDirectory: C:\Users\Public
windowStyle: Normal
iconLocation: C:\Windows\System32\cmd.exe,0
'@

dsc resource set -r OpenDsc.Windows/Shortcut -i $config
```

### Create a shortcut with hotkey

```powershell
$config = @'
path: C:\Users\Public\Desktop\Calc.lnk
targetPath: C:\Windows\System32\calc.exe
hotkey: CTRL+ALT+C
'@

dsc resource set -r OpenDsc.Windows/Shortcut -i $config
```

### Delete a shortcut

```powershell
$config = @'
path: C:\Users\Public\Desktop\Notepad.lnk
'@

dsc resource delete -r OpenDsc.Windows/Shortcut -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Failed to generate schema
- **4** - Directory not found
