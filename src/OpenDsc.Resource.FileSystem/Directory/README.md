# OpenDsc.FileSystem/Directory

## Synopsis

Cross-platform DSC resource for managing directories with optional
source-based seeding.

## Description

The `OpenDsc.FileSystem/Directory` resource manages directories across
Windows, Linux, and macOS. Parent directories are created recursively as
needed.

**Source-based seeding:** When `sourcePath` is specified, the resource copies
all files and subdirectories from the source to the target. Directory contents
are compared using SHA256 hash-based comparison to ensure all source files
are present and match in the target. Extra files in the target are ignored,
allowing for additive synchronization.

## Requirements

- Cross-platform support: Windows, Linux, macOS
- File system permissions for directory creation/deletion

## Capabilities

- **get** - Retrieve current directory state
- **set** - Create directory; optionally copy contents from source
- **test** - Check if directory matches desired state
- **delete** - Remove a directory

## Properties

### Required Properties

- **path** (string) - Absolute path to the directory

### Optional Properties

- **sourcePath** (string) - Path to source directory to copy contents from.
  Files are compared using SHA256 hashes
- **_exist** (boolean) - Whether the directory should exist (default: `true`)

### Read-only Properties

- **_inDesiredState** (boolean) - Whether the directory is in desired state

## Examples

### Get directory state

```powershell
$config = @'
path: C:\temp\dir
'@

dsc resource get -r OpenDsc.FileSystem/Directory -i $config
```

### Create directory

Parent directories are created recursively if needed:

```powershell
$config = @'
path: C:\temp\parent\child\dir
'@

dsc resource set -r OpenDsc.FileSystem/Directory -i $config
```

### Delete directory

```powershell
$config = @'
path: C:\temp\dir
'@

dsc resource delete -r OpenDsc.FileSystem/Directory -i $config
```

### Seed directory from source

Copy all files and subdirectories from source using hash-based comparison:

```powershell
$config = @'
path: C:\temp\target
sourcePath: C:\temp\source
'@

dsc resource set -r OpenDsc.FileSystem/Directory -i $config
```

### Test directory compliance

Verify all source files are present and match via SHA256 hashes:

```powershell
$config = @'
path: C:\temp\target
sourcePath: C:\temp\source
'@

dsc resource test -r OpenDsc.FileSystem/Directory -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - IO error
- **6** - Access denied
