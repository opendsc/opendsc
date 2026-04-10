# Resources

This document lists all built-in OpenDSC Resources organized by platform area.
Each resource
includes its type name, supported capabilities, and key properties.

## Windows resources

Windows resources require a Windows operating system. They are included in the
Windows build of the
resource executable.

| Resource                                                                    | Description                            |
| :-------------------------------------------------------------------------- | :------------------------------------- |
| [`OpenDsc.Windows/Environment`](windows/environment.md)                     | Manage Windows environment variables   |
| [`OpenDsc.Windows/User`](windows/user.md)                                   | Manage local Windows user accounts     |
| [`OpenDsc.Windows/Group`](windows/group.md)                                 | Manage local Windows groups            |
| [`OpenDsc.Windows/Service`](windows/service.md)                             | Manage Windows services                |
| [`OpenDsc.Windows/Shortcut`](windows/shortcut.md)                           | Manage Windows shortcuts (.lnk)        |
| [`OpenDsc.Windows/ScheduledTask`](windows/scheduled-task.md)                | Manage Windows scheduled tasks         |
| [`OpenDsc.Windows/OptionalFeature`](windows/optional-feature.md)            | Manage Windows optional features       |
| [`OpenDsc.Windows/UserRight`](windows/user-right.md)                        | Manage Windows user rights assignments |
| [`OpenDsc.Windows.FileSystem/AccessControlList`](windows/filesystem/acl.md) | Manage file and directory permissions  |

## SQL Server resources

SQL Server resources connect to SQL Server instances. They require SMO (SQL
Server Management
Objects) and work on all platforms.

| Resource                                                                   | Description                     |
| :------------------------------------------------------------------------- | :------------------------------ |
| [`OpenDsc.SqlServer/Login`](sqlserver/login.md)                            | Manage SQL Server logins        |
| [`OpenDsc.SqlServer/Database`](sqlserver/database.md)                      | Manage SQL Server databases     |
| [`OpenDsc.SqlServer/DatabaseRole`](sqlserver/database-role.md)             | Manage database roles           |
| [`OpenDsc.SqlServer/DatabaseUser`](sqlserver/database-user.md)             | Manage database users           |
| [`OpenDsc.SqlServer/ServerRole`](sqlserver/server-role.md)                 | Manage server roles             |
| [`OpenDsc.SqlServer/ServerPermission`](sqlserver/server-permission.md)     | Manage server-level permissions |
| [`OpenDsc.SqlServer/DatabasePermission`](sqlserver/database-permission.md) | Manage database permissions     |
| [`OpenDsc.SqlServer/ObjectPermission`](sqlserver/object-permission.md)     | Manage object-level permissions |
| [`OpenDsc.SqlServer/LinkedServer`](sqlserver/linked-server.md)             | Manage linked servers           |
| [`OpenDsc.SqlServer/Configuration`](sqlserver/configuration.md)            | Manage instance configuration   |
| [`OpenDsc.SqlServer/AgentJob`](sqlserver/agent-job.md)                     | Manage SQL Server Agent jobs    |

## Cross-platform resources

These resources work on Windows, Linux, and macOS.

### File system

| Resource                                                         | Description           |
| :--------------------------------------------------------------- | :-------------------- |
| [`OpenDsc.FileSystem/File`](filesystem/file.md)                  | Manage files          |
| [`OpenDsc.FileSystem/Directory`](filesystem/directory.md)        | Manage directories    |
| [`OpenDsc.FileSystem/SymbolicLink`](filesystem/symbolic-link.md) | Manage symbolic links |

### JSON

| Resource                              | Description                              |
| :------------------------------------ | :--------------------------------------- |
| [`OpenDsc.Json/Value`](json/value.md) | Manage JSON values at JSONPath locations |

### XML

| Resource                                | Description                        |
| :-------------------------------------- | :--------------------------------- |
| [`OpenDsc.Xml/Element`](xml/element.md) | Manage XML elements and attributes |

### Archive

| Resource                                                  | Description          |
| :-------------------------------------------------------- | :------------------- |
| [`OpenDsc.Archive.Zip/Compress`](archive/zip-compress.md) | Create ZIP archives  |
| [`OpenDsc.Archive.Zip/Expand`](archive/zip-expand.md)     | Extract ZIP archives |

## POSIX resources

POSIX resources work on Linux and macOS only.

| Resource                                                                | Description                   |
| :---------------------------------------------------------------------- | :---------------------------- |
| [`OpenDsc.Posix.FileSystem/Permission`](posix/filesystem-permission.md) | Manage POSIX file permissions |
