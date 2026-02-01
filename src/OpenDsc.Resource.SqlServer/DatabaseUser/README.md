# OpenDsc.SqlServer/DatabaseUser

## Synopsis

Manage SQL Server database users.

## Description

The `OpenDsc.SqlServer/DatabaseUser` resource enables you to manage SQL Server
database users. Database users are security principals within a database that
map to server-level logins or can exist as contained database users.

This resource supports creating, updating, and deleting database users,
including mapping them to logins, setting default schemas, and configuring
contained database users with passwords.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage database users (typically
  db_owner or db_securityadmin role membership in the target database)
- Windows authentication or SQL Server authentication for connecting

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a database user
- `set` - Create or update a database user
- `test` - Test if a database user is in the desired state
- `delete` - Remove a database user
- `export` - List all database users

## User Types

SQL Server supports different types of database users:

| Type                        | Description                                        |
|-----------------------------|----------------------------------------------------|
| `SqlUser`                   | User mapped to a SQL Server login                  |
| `NoLogin`                   | User without an associated login (orphaned user)   |
| `AsymmetricKeyMappedLogin`  | User mapped to an asymmetric key login             |
| `CertificateMappedLogin`    | User mapped to a certificate login                 |
| `AsymmetricKeyMappedUser`   | Contained user authenticated by asymmetric key     |
| `CertificateMappedUser`     | Contained user authenticated by certificate        |
| `SqlUser` with password     | Contained database user with password (SQL 2012+)  |
| `External`                  | Azure AD user or group                             |

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **databaseName** (string) - The name of the database containing the user.
- **name** (string) - The name of the database user.

### Connection Properties

- **connectUsername** (string) - The username for SQL Server authentication
  when connecting to the server. If not specified, Windows Authentication is
  used. Write-only.
- **connectPassword** (string) - The password for SQL Server authentication
  when connecting to the server. Required when connectUsername is specified.
  Write-only.

### Optional Properties

- **userType** (string) - The type of user. Determines how the user
  authenticates to the database. See User Types table above.
- **login** (string) - The name of the login to map to this database user.
  Required for SqlUser user type.
- **defaultSchema** (string) - The default schema for the user. If not
  specified, defaults to 'dbo'.
- **password** (string) - The password for contained database users.
  Write-only.
- **asymmetricKey** (string) - The asymmetric key for AsymmetricKeyMappedUser
  type.
- **certificate** (string) - The certificate for CertificateMappedUser type.
- **_exist** (boolean) - Indicates whether the database user should exist.
  Default: `true`.

### Read-Only Properties

- **createDate** (datetime) - The creation date of the user.
- **dateLastModified** (datetime) - The date the user was last modified.
- **defaultLanguage** (string) - The default language for the user.
- **hasDBAccess** (boolean) - Whether the user has access to the database.
- **isSystemObject** (boolean) - Whether this is a system user.
- **sid** (string) - The security identifier (SID) of the user.
- **authenticationType** (string) - The authentication type of the user.

## Examples

### Get Database User

Retrieve the current state of a database user:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUser
```

```powershell
dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUser"}'
```

### Create User Mapped to Login

Create a database user mapped to an existing SQL Server login:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUser
login: AppLogin
defaultSchema: dbo
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUser","login":"AppLogin","defaultSchema":"dbo"}'
```

### Create User Without Login

Create a database user without an associated login (useful for testing or when
the login will be created later):

```yaml
serverInstance: .
databaseName: MyDatabase
name: OrphanUser
userType: NoLogin
defaultSchema: dbo
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"OrphanUser","userType":"NoLogin","defaultSchema":"dbo"}'
```

### Create Contained Database User

Create a contained database user with a password (requires contained database):

```yaml
serverInstance: .
databaseName: ContainedDb
name: ContainedUser
password: SecureP@ssw0rd!
defaultSchema: dbo
```

### Update User Default Schema

Change the default schema for an existing user:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUser
defaultSchema: app
```

### Delete Database User

Remove a database user:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUser
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUser"}'
```

### Export All Database Users

Export all non-system database users:

```powershell
# Export from all user databases on local instance
$env:SQLSERVER_INSTANCE = "."
dsc resource export -r OpenDsc.SqlServer/DatabaseUser

# Export from specific database
$env:SQLSERVER_DATABASE = "MyDatabase"
dsc resource export -r OpenDsc.SqlServer/DatabaseUser
```

## Notes

- When creating a user mapped to a login, the login must already exist on the
  SQL Server instance.
- Contained database users with passwords require the database to be configured
  as a contained database (`ALTER DATABASE dbname SET CONTAINMENT = PARTIAL`).
- System users (like `dbo`, `guest`, `INFORMATION_SCHEMA`, `sys`) cannot be
  deleted.
- The `password` property is write-only and cannot be retrieved.
- When updating a user's password, the new password is set directly without
  requiring the old password (administrator password change).

## Limitations

### Immutable Properties

The following properties cannot be changed after the database user is created:

- **login** - The login mapping cannot be modified after creation. This is a
  limitation of SQL Server Management Objects (SMO). Attempting to change the
  login will result in an `InvalidOperationException`. To change a user's login
  mapping, you must drop and recreate the user.

- **userType** - The user type is determined at creation time and cannot be
  changed.

- **asymmetricKey** / **certificate** - The key or certificate mapping is set
  at creation and cannot be modified.

If you need to change an immutable property, delete the user and recreate it
with the new settings:

```powershell
# Delete the existing user
dsc resource delete -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUser"}'

# Recreate with new login
dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUser","login":"NewLogin"}'
```

## See Also

- [OpenDsc.SqlServer/Login](../Login/README.md) - Manage SQL Server logins
- [OpenDsc.SqlServer/DatabaseRole](../DatabaseRole/README.md) - Manage database
  roles
- [OpenDsc.SqlServer/DatabasePermission](../DatabasePermission/README.md) -
  Manage database permissions
