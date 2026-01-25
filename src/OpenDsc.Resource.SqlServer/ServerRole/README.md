# OpenDsc.SqlServer/ServerRole

Manages SQL Server server roles. This resource allows you to create, modify,
and delete custom server roles, as well as manage membership for both custom
and fixed server roles.

## Properties

| Property          | Type     | Required | Description                                                             |
|-------------------|----------|----------|-------------------------------------------------------------------------|
| `serverInstance`  | string   | Yes      | The SQL Server instance to connect to                                   |
| `connectUsername` | string   | No       | Username for SQL authentication (write-only)                            |
| `connectPassword` | string   | No       | Password for SQL authentication (write-only)                            |
| `name`            | string   | Yes      | The name of the server role                                             |
| `owner`           | string   | No       | The owner of the role (login or server role)                            |
| `members`         | string[] | No       | Members of the role (logins or server roles)                            |
| `_purge`          | bool     | No       | When true, removes members not in the list (write-only, default: false) |
| `dateCreated`     | DateTime | No       | Creation date (read-only)                                               |
| `dateModified`    | DateTime | No       | Last modified date (read-only)                                          |
| `isFixedRole`     | bool     | No       | Whether this is a fixed server role (read-only)                         |
| `_exist`          | bool     | No       | Whether the role should exist (default: true)                           |

## Fixed Server Roles

The following fixed server roles cannot be created, deleted, or have their owner
changed. However, you can manage their members:

| Role            | Description                                                                   |
|-----------------|-------------------------------------------------------------------------------|
| `sysadmin`      | Members can perform any activity in the server                                |
| `serveradmin`   | Members can change server-wide configuration options and shut down the server |
| `securityadmin` | Members manage logins and their properties                                    |
| `processadmin`  | Members can terminate processes running in an instance of SQL Server          |
| `setupadmin`    | Members can add and remove linked servers                                     |
| `bulkadmin`     | Members can run the BULK INSERT statement                                     |
| `diskadmin`     | Members can manage disk files                                                 |
| `dbcreator`     | Members can create, alter, drop, and restore any database                     |
| `public`        | Every SQL Server login belongs to the public server role                      |

## Member Management with `_purge`

The `_purge` property controls how member lists are managed:

- **`_purge: false` (default)**: Additive mode - only adds members from the
  `members` list without removing existing members
- **`_purge: true`**: Exact mode - ensures only the specified members are
  present, removing any others

## Examples

### Get a server role

```powershell
dsc resource get -r OpenDsc.SqlServer/ServerRole --input '{"serverInstance": ".", "name": "MyCustomRole"}'
```

### Create a custom server role

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "MyCustomRole",
  "owner": "sa"
}'
```

### Create a server role with members

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "MyCustomRole",
  "owner": "sa",
  "members": ["MyLogin1", "MyLogin2"]
}'
```

### Add members to a fixed server role

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "sysadmin",
  "members": ["MyLogin1"]
}'
```

### Add members without removing existing ones (default)

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "MyCustomRole",
  "members": ["NewMember1", "NewMember2"]
}'
```

### Replace all members (purge mode)

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "MyCustomRole",
  "members": ["OnlyThisMember"],
  "_purge": true
}'
```

### Remove all members from a role

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "MyCustomRole",
  "members": [],
  "_purge": true
}'
```

### Delete a custom server role

```powershell
dsc resource delete -r OpenDsc.SqlServer/ServerRole --input '{"serverInstance": ".", "name": "MyCustomRole"}'
```

### Using SQL Server Authentication

```powershell
dsc resource get -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": "myserver\\instance",
  "connectUsername": "sa",
  "connectPassword": "MyPassword123",
  "name": "MyCustomRole"
}'
```

### Export all custom server roles

```powershell
$env:SQLSERVER_INSTANCE = "."
dsc resource export -r OpenDsc.SqlServer/ServerRole
```

## DSC Configuration Example

```yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Create DevOps server role
    type: OpenDsc.SqlServer/ServerRole
    properties:
      serverInstance: "."
      name: DevOpsRole
      owner: sa
      members:
        - DevOpsLogin1
        - DevOpsLogin2

  - name: Add login to sysadmin
    type: OpenDsc.SqlServer/ServerRole
    properties:
      serverInstance: "."
      name: sysadmin
      members:
        - AdminLogin

  - name: Exact membership for security role
    type: OpenDsc.SqlServer/ServerRole
    properties:
      serverInstance: "."
      name: SecurityTeamRole
      members:
        - SecurityAdmin1
        - SecurityAdmin2
      _purge: true
```

## Notes

- Fixed server roles cannot be created, deleted, or have their owner changed
- You can only manage members of fixed server roles
- The `public` server role cannot have members added or removed
- Server roles are only available in SQL Server 2012 and later
- Custom server roles require the `ALTER ANY SERVER ROLE` or `CONTROL SERVER`
  permission to create
