# OpenDsc.Archive.Zip/Compress

## Synopsis

Create ZIP archives from files and directories.

## Description

The `OpenDsc.Archive.Zip/Compress` resource creates ZIP archives from source
files and directories. It supports cross-platform operation on Windows, Linux,
and macOS with configurable compression levels. The resource uses checksum
verification to determine if the archive is in the desired state.

## Requirements

- Cross-platform support: Windows, Linux, macOS
- File system read permissions for source path
- File system write permissions for archive path

## Capabilities

- **get** - Retrieve current archive state and checksum information
- **set** - Create or update ZIP archive from source files
- **test** - Verify archive contents match source using checksums

## Properties

### Required Properties

- **archivePath** (string) - The path to the ZIP archive file to create
- **sourcePath** (string) - The path to the source directory or file to archive

### Optional Properties

- **compressionLevel** (enum) - The compression level to use. Default: `Optimal`
  - `Optimal` - Balanced compression and speed
  - `Fastest` - Prioritize speed over compression ratio
  - `NoCompression` - Store files without compression
  - `SmallestSize` - Maximum compression (slowest)

### Read-Only Properties

- **_inDesiredState** (boolean) - Indicates whether the archive contents match
  the source files using checksum verification

## Examples

### Get Archive State

Check if an archive exists and matches the source:

```powershell
$config = @'
archivePath: /backups/project.zip
sourcePath: /projects/app
'@

dsc resource get -r OpenDsc.Archive.Zip/Compress -i $config
```

### Create Archive with Default Compression

Create a ZIP archive with optimal compression:

```powershell
$config = @'
archivePath: C:\backups\project.zip
sourcePath: C:\projects\app
'@

dsc resource set -r OpenDsc.Archive.Zip/Compress -i $config
```

### Create Archive with Maximum Compression

Create an archive optimized for smallest size:

```powershell
$config = @'
archivePath: /backups/compressed.zip
sourcePath: /data/files
compressionLevel: SmallestSize
'@

dsc resource set -r OpenDsc.Archive.Zip/Compress -i $config
```

### Create Archive with Fastest Compression

Prioritize speed over compression ratio:

```powershell
$config = @'
archivePath: /tmp/quick-backup.zip
sourcePath: /var/log
compressionLevel: Fastest
'@

dsc resource set -r OpenDsc.Archive.Zip/Compress -i $config
```

### Test Archive State

Verify if archive matches source using checksums:

```powershell
$config = @'
archivePath: /backups/project.zip
sourcePath: /projects/app
'@

dsc resource test -r OpenDsc.Archive.Zip/Compress -i $config
```

## Behavior

### Create/Update Logic

When creating or updating archives:

1. If the archive doesn't exist, it creates a new archive from the source
2. If the archive exists, checksums are compared between archive contents and
   source files
3. If checksums match, no changes are made (_inDesiredState: true)
4. If checksums differ or files are added/removed, the archive is recreated

### Checksum Verification

The resource uses file checksums to determine if the archive is current:

- Compares file names, sizes, and content hashes
- Detects new, modified, or deleted files in the source
- Only recreates the archive when differences are detected

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - IO error

## Related Resources

- [OpenDsc.Archive.Zip/Expand](../Expand/README.md) - Extract ZIP archives
- [OpenDsc.FileSystem/File][file] - Manage individual files
- [OpenDsc.FileSystem/Directory][directory] - Manage directories

[file]: ../../../OpenDsc.Resource.FileSystem/File/README.md
[directory]: ../../../OpenDsc.Resource.FileSystem/Directory/README.md
