# OpenDsc.Windows/Environment

## Synopsis

Manage Windows environment variables.

## Description

The `OpenDsc.Windows/Environment` resource enables you to manage Windows
environment variables for both user and machine scopes. You can create,
update, retrieve, and delete environment variables using Microsoft DSC.

Environment variables can be scoped to either the current user or
system-wide (machine scope). Machine-scoped operations require administrator
privileges.

## Requirements

- Windows operating system
- Administrator privileges required for machine-scoped environment variables

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current value of an environment variable
- `set` - Create or update an environment variable
- `delete` - Remove an environment variable
- `export` - List all environment variables (user and machine scope)

## Properties

### Required Properties

- **name** (string) - The name of the environment variable. Cannot contain
  equals sign (=) or null characters. Maximum 254 characters.

### Optional Properties

- **value** (string) - The value of the environment variable.
- **_exist** (boolean) - Indicates whether the environment variable should
  exist. Default: `true`.
- **_scope** (enum) - The scope of the environment variable. Valid values:
  `User` (default), `Machine`.

### Scope Values

- `User` - Environment variable is set for the current user only
- `Machine` - Environment variable is set system-wide for all users
  (requires administrator privileges)

## Examples

### Get Environment Variable

Retrieve the value of a user environment variable:

```powershell
$config = @'
name: MY_VAR
'@

dsc resource get -r OpenDsc.Windows/Environment -i $config
```

Output:

```yaml
actualState:
  name: MY_VAR
  value: SomeValue
  _exist: true
```

### Get Non-Existent Variable

Query a variable that doesn't exist:

```powershell
$config = @'
name: NON_EXISTENT_VAR
'@

dsc resource get -r OpenDsc.Windows/Environment -i $config
```

Output:

```yaml
actualState:
  name: NON_EXISTENT_VAR
  value: null
  _exist: false
```

### Create User Environment Variable

Create a new environment variable in user scope:

```powershell
$config = @'
name: MY_APP_HOME
value: C:\Users\MyUser\MyApp
'@

dsc resource set -r OpenDsc.Windows/Environment -i $config
```

### Create Machine Environment Variable

Create a system-wide environment variable (requires administrator):

```powershell
$config = @'
name: COMPANY_LICENSE_SERVER
value: license.example.com
_scope: Machine
'@

dsc resource set -r OpenDsc.Windows/Environment -i $config
```

**Note**: Requires administrator privileges

### Update Existing Variable

Update the value of an existing environment variable:

```powershell
$config = @'
name: MY_APP_HOME
value: D:\Applications\MyApp
'@

dsc resource set -r OpenDsc.Windows/Environment -i $config
```

### Set PATH Variable

Update or append to the PATH environment variable:

```powershell
$config = @'
name: PATH
value: C:\MyTools;C:\Python311;%PATH%
'@

dsc resource set -r OpenDsc.Windows/Environment -i $config
```

**Note**: Be careful when modifying PATH. Consider reading the current value
first and appending to it.

### Delete Environment Variable

Remove an environment variable:

```powershell
$config = @'
name: MY_APP_HOME
_exist: false
'@

dsc resource delete -r OpenDsc.Windows/Environment -i $config
```

### Delete Machine Variable

Remove a machine-scoped environment variable:

```powershell
$config = @'
name: COMPANY_LICENSE_SERVER
_exist: false
_scope: Machine
'@

dsc resource delete -r OpenDsc.Windows/Environment -i $config
```

**Note**: Requires administrator privileges

### Export All Variables

List all environment variables (both user and machine scope):

```powershell
dsc resource export -r OpenDsc.Windows/Environment
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
