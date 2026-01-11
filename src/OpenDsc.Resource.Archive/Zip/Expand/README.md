# OpenDsc.Archive.Zip/Expand

## Synopsis

Extract ZIP archives to specified directories.

## Description

The `OpenDsc.Archive.Zip/Expand` resource extracts ZIP archives to destination
directories. It supports cross-platform operation on Windows, Linux, and macOS.
The resource uses checksum verification to determine if the destination is in
the desired state by comparing extracted files with archive contents.

## Requirements

- Cross-platform support: Windows, Linux, macOS
- File system read permissions for archive path
- File system write permissions for destination path

## Capabilities

- **get** - Retrieve current extraction state and checksum information
- **set** - Extract ZIP archive contents to destination
- **test** - Verify destination files match archive using checksums

## Properties

### Required Properties

- **archivePath** (string) - The path to the ZIP archive file to extract
- **destinationPath** (string) - The path to the destination directory where
  archive contents will be extracted

### Read-Only Properties

- **_inDesiredState** (boolean) - Indicates whether the destination contains
  all files from the archive with matching checksums

## Examples

### Get Extraction State

Check if archive contents are extracted and current:

```powershell
$config = @'
archivePath: /downloads/release.zip
destinationPath: /opt/app
'@

dsc resource get -r OpenDsc.Archive.Zip/Expand -i $config
```

### Extract Archive

Extract a ZIP archive to a destination directory:

```powershell
$config = @'
archivePath: C:\downloads\software.zip
destinationPath: C:\Program Files\MySoftware
'@

dsc resource set -r OpenDsc.Archive.Zip/Expand -i $config
```

### Extract to Temporary Location

Extract files to a temporary directory:

```powershell
$config = @'
archivePath: /var/cache/updates.zip
destinationPath: /tmp/update-staging
'@

dsc resource set -r OpenDsc.Archive.Zip/Expand -i $config
```

### Test Extraction State

Verify if destination matches archive contents:

```powershell
$config = @'
archivePath: /backups/project.zip
destinationPath: /restore/project
'@

dsc resource test -r OpenDsc.Archive.Zip/Expand -i $config
```

## Behavior

### Extraction Logic

When extracting archives:

1. If the destination directory doesn't exist, it's created
2. Archive contents are compared with existing files in the destination
3. Files are extracted only if:
   - They don't exist in the destination
   - They exist but have different checksums
   - They're missing from the destination
4. If all files match (_inDesiredState: true), no extraction occurs

### Checksum Verification

The resource uses file checksums to determine if extraction is needed:

- Compares file names, sizes, and content hashes between archive and destination
- Detects new, modified, or deleted files
- Only extracts when differences are detected

### Overwrite Behavior

When setting the resource:

- Existing files with matching checksums are preserved
- Files with different checksums are overwritten
- New files from the archive are added
- Existing files not in the archive are left untouched

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - IO error

## Related Resources

- [OpenDsc.Archive.Zip/Compress](../Compress/README.md) - Create ZIP archives
- [OpenDsc.FileSystem/File][file] - Manage individual files
- [OpenDsc.FileSystem/Directory][directory] - Manage directories

[file]: ../../../OpenDsc.Resource.FileSystem/File/README.md
[directory]: ../../../OpenDsc.Resource.FileSystem/Directory/README.md
