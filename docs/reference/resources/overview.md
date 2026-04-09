# OpenDsc resource reference

This document lists all built-in OpenDsc Resources organized by platform area.
Each resource
includes its type name, supported capabilities, and key properties.

## Windows resources

Windows resources require a Windows operating system. They are included in the
Windows build of the
resource executable.

| Resource                                                                    | Description                            | Capabilities             |
| :-------------------------------------------------------------------------- | :------------------------------------- | :----------------------- |
| [`OpenDsc.Windows/Environment`](windows/environment.md)                     | Manage Windows environment variables   | Get, Set, Delete, Export |
| [`OpenDsc.Windows/User`](windows/user.md)                                   | Manage local Windows user accounts     | Get, Set, Delete, Export |
| [`OpenDsc.Windows/Group`](windows/group.md)                                 | Manage local Windows groups            | Get, Set, Delete, Export |
| [`OpenDsc.Windows/Service`](windows/service.md)                             | Manage Windows services                | Get, Set, Delete, Export |
| [`OpenDsc.Windows/Shortcut`](windows/shortcut.md)                           | Manage Windows shortcuts (.lnk)        | Get, Set, Delete         |
| [`OpenDsc.Windows/ScheduledTask`](windows/scheduled-task.md)                | Manage Windows scheduled tasks         | Get, Set, Delete, Export |
| [`OpenDsc.Windows/OptionalFeature`](windows/optional-feature.md)            | Manage Windows optional features       | Get, Set, Delete, Export |
| [`OpenDsc.Windows/UserRight`](windows/user-right.md)                        | Manage Windows user rights assignments | Get, Set, Export         |
| [`OpenDsc.Windows.FileSystem/AccessControlList`](windows/filesystem-acl.md) | Manage file and directory permissions  | Get, Set                 |

## SQL Server resources

SQL Server resources connect to SQL Server instances. They require SMO (SQL
Server Management
Objects) and work on all platforms.

| Resource                                                                   | Description                     | Capabilities             |
| :------------------------------------------------------------------------- | :------------------------------ | :----------------------- |
| [`OpenDsc.SqlServer/Login`](sqlserver/login.md)                            | Manage SQL Server logins        | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/Database`](sqlserver/database.md)                      | Manage SQL Server databases     | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/DatabaseRole`](sqlserver/database-role.md)             | Manage database roles           | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/DatabaseUser`](sqlserver/database-user.md)             | Manage database users           | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/ServerRole`](sqlserver/server-role.md)                 | Manage server roles             | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/ServerPermission`](sqlserver/server-permission.md)     | Manage server-level permissions | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/DatabasePermission`](sqlserver/database-permission.md) | Manage database permissions     | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/ObjectPermission`](sqlserver/object-permission.md)     | Manage object-level permissions | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/LinkedServer`](sqlserver/linked-server.md)             | Manage linked servers           | Get, Set, Delete, Export |
| [`OpenDsc.SqlServer/Configuration`](sqlserver/configuration.md)            | Manage instance configuration   | Get, Set, Export         |
| [`OpenDsc.SqlServer/AgentJob`](sqlserver/agent-job.md)                     | Manage SQL Server Agent jobs    | Get, Set, Delete, Export |

## Cross-platform resources

These resources work on Windows, Linux, and macOS.

### File system

| Resource                                                         | Description           | Capabilities           |
| :--------------------------------------------------------------- | :-------------------- | :--------------------- |
| [`OpenDsc.FileSystem/File`](filesystem/file.md)                  | Manage files          | Get, Set, Delete       |
| [`OpenDsc.FileSystem/Directory`](filesystem/directory.md)        | Manage directories    | Get, Set, Delete, Test |
| [`OpenDsc.FileSystem/SymbolicLink`](filesystem/symbolic-link.md) | Manage symbolic links | Get, Set, Delete       |

### JSON

| Resource                              | Description                              | Capabilities     |
| :------------------------------------ | :--------------------------------------- | :--------------- |
| [`OpenDsc.Json/Value`](json/value.md) | Manage JSON values at JSONPath locations | Get, Set, Delete |

### XML

| Resource                                | Description                        | Capabilities     |
| :-------------------------------------- | :--------------------------------- | :--------------- |
| [`OpenDsc.Xml/Element`](xml/element.md) | Manage XML elements and attributes | Get, Set, Delete |

### Archive

| Resource                                                  | Description          | Capabilities   |
| :-------------------------------------------------------- | :------------------- | :------------- |
| [`OpenDsc.Archive.Zip/Compress`](archive/zip-compress.md) | Create ZIP archives  | Get, Set, Test |
| [`OpenDsc.Archive.Zip/Expand`](archive/zip-expand.md)     | Extract ZIP archives | Get, Set, Test |

## POSIX resources

POSIX resources work on Linux and macOS only.

| Resource                                                                | Description                   | Capabilities |
| :---------------------------------------------------------------------- | :---------------------------- | :----------- |
| [`OpenDsc.Posix.FileSystem/Permission`](posix/filesystem-permission.md) | Manage POSIX file permissions | Get, Set     |
