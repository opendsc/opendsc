# OpenDsc.FileSystem/File

## Synopsis

Cross-platform DSC resource for managing file content.

## Description

The `OpenDsc.FileSystem/File` resource manages files across Windows, Linux,
and macOS. It can create, update, or delete files with text content. Content
is written as UTF-8 text. Relative paths are resolved from the current
working directory.

## Requirements

- Cross-platform support: Windows, Linux, macOS
- File system write permissions for the target directory

## Capabilities

- **get** - Retrieve current file content and existence
- **set** - Create or update file with specified content
- **delete** - Remove a file

## Properties

### Required Properties

- **path** (string) - Absolute or relative path to the file

### Optional Properties

- **content** (string) - Text content to write. If omitted, creates an
  empty file
- **_exist** (boolean) - Whether the file should exist (default: `true`)

## Examples

### Get file content

```powershell
$config = @'
path: /path/to/file.txt
'@

dsc resource get -r OpenDsc.FileSystem/File -i $config
```

### Create file with content

```powershell
$config = @'
path: /path/to/newfile.txt
content: Hello, World!
'@

dsc resource set -r OpenDsc.FileSystem/File -i $config
```

### Update existing file

```powershell
$config = @'
path: /path/to/existing.txt
content: Updated content
'@

dsc resource set -r OpenDsc.FileSystem/File -i $config
```

### Create empty file

```powershell
$config = @'
path: /path/to/empty.txt
'@

dsc resource set -r OpenDsc.FileSystem/File -i $config
```

### Delete file

```powershell
$config = @'
path: /path/to/file.txt
'@

dsc resource delete -r OpenDsc.FileSystem/File -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - IO error
