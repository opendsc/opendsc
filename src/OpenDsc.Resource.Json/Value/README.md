# OpenDsc.Json/Value

## Synopsis

Manage JSON values at JSONPath locations.

## Description

The `OpenDsc.Json/Value` resource enables you to manage JSON values within JSON
documents using JSONPath expressions. You can create, update, retrieve, and
delete values at any JSONPath location, with automatic recursive creation of
parent paths when needed.

The resource supports all valid JSON types including strings, numbers, booleans,
null, objects, and arrays. JSONPath expressions follow the RFC 9535 standard
using the JsonPath.Net library.

## Requirements

- Cross-platform support (Windows, Linux, macOS)
- JSON document must exist for set operations (file creation not automatic)

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the value at a JSONPath location
- `set` - Create or update a value at a JSONPath location
- `delete` - Remove a value from a JSONPath location

## Properties

### Required Properties

- **path** (string) - The absolute file path to the JSON document.
- **jsonPath** (string) - JSONPath expression to locate the value. Must start
  with `$`. Parent paths will be created recursively if they don't exist.

### Optional Properties

- **value** (any JSON type) - The JSON value to set. Can be a string, number,
  boolean, null, object, or array.
- **_exist** (boolean) - Indicates whether the value should exist.
  Default: `true`.

## Examples

### Get a String Value

Retrieve a string value from a JSON document:

```powershell
$config = @'
path: C:\config.json
jsonPath: $.server.hostname
'@

dsc resource get -r OpenDsc.Json/Value -i $config
```

Output:

```yaml
actualState:
  path: C:\config.json
  jsonPath: $.server.hostname
  value: localhost
```

### Get Non-Existent Value

Query a value that doesn't exist:

```powershell
$config = @'
path: C:\config.json
jsonPath: $.server.timeout
'@

dsc resource get -r OpenDsc.Json/Value -i $config
```

Output:

```yaml
actualState:
  path: C:\config.json
  jsonPath: $.server.timeout
  _exist: false
```

### Set a String Value

Create or update a string value:

```powershell
$config = @'
path: C:\config.json
jsonPath: $.app.name
value: MyApplication
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Set a Number Value

```powershell
$config = @'
path: C:\config.json
jsonPath: $.server.port
value: 8080
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Set a Boolean Value

```powershell
$config = @'
path: C:\config.json
jsonPath: $.features.enabled
value: true
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Set a Null Value

```powershell
$config = @'
path: C:\config.json
jsonPath: $.server.proxy
value: null
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Set an Object Value

```powershell
$config = @'
path: C:\config.json
jsonPath: $.database
value:
  host: localhost
  port: 5432
  ssl: true
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Set an Array Value

```powershell
$config = @'
path: C:\config.json
jsonPath: $.servers
value:
  - web01
  - web02
  - web03
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Recursive Parent Creation

Create nested paths automatically:

```powershell
$config = @'
path: C:\config.json
jsonPath: $.app.server.database.connectionString
value: Server=localhost;Database=MyDb
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

This will automatically create the `app`, `server`, and `database` objects if
they don't exist.

### Set Array Element

```powershell
$config = @'
path: C:\config.json
jsonPath: $.servers[0]
value: primary-server
'@

dsc resource set -r OpenDsc.Json/Value -i $config
```

### Delete a Value

Remove a value from an object:

```powershell
$config = @'
path: C:\config.json
jsonPath: $.server.timeout
_exist: false
'@

dsc resource delete -r OpenDsc.Json/Value -i $config
```

### Delete an Array Element

```powershell
$config = @'
path: C:\config.json
jsonPath: $.servers[1]
_exist: false
'@

dsc resource delete -r OpenDsc.Json/Value -i $config
```

## Configuration Example

Manage multiple JSON values in a configuration:

```yaml
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: Application Name
    type: OpenDsc.Json/Value
    properties:
      path: C:\config\app.json
      jsonPath: $.application.name
      value: MyWebApp

  - name: Server Port
    type: OpenDsc.Json/Value
    properties:
      path: C:\config\app.json
      jsonPath: $.server.port
      value: 8080

  - name: SSL Enabled
    type: OpenDsc.Json/Value
    properties:
      path: C:\config\app.json
      jsonPath: $.server.ssl
      value: true

  - name: Database Configuration
    type: OpenDsc.Json/Value
    properties:
      path: C:\config\app.json
      jsonPath: $.database
      value:
        host: localhost
        port: 5432
        name: mydb
        ssl: true

  - name: Remove Debug Mode
    type: OpenDsc.Json/Value
    properties:
      path: C:\config\app.json
      jsonPath: $.debug
      _exist: false
```

## JSONPath Syntax

The resource uses RFC 9535 JSONPath standard. Common patterns:

- `$.property` - Root-level property
- `$.parent.child` - Nested property
- `$.array[0]` - Array element by index
- `$.parent.array[1]` - Nested array element

## Notes

- The JSON document must exist before using the `set` operation (file creation
  is not automatic)
- Parent paths are created recursively as objects by default
- Array paths require explicit array syntax (`[0]`, `[1]`, etc.)
- The resource preserves JSON formatting (indentation) when updating files
- Use the `delete` operation with `_exist: false` to remove values
- Machine-readable characters in property names should use bracket notation
  in JSONPath

## Error Handling

The resource will return appropriate exit codes for various error conditions:

- Exit code 0: Success
- Exit code 1: General error
- Exit code 2: Invalid JSON
- Exit code 3: JSON file not found
- Exit code 4: Invalid argument
- Exit code 5: IO error

## See Also

- [JSONPath RFC 9535](https://www.rfc-editor.org/rfc/rfc9535.html)
- [JsonPath.Net Library](https://github.com/json-everything/json-everything)
- [OpenDsc.Xml/Element](../../OpenDsc.Resource.Xml/Element/README.md) - Similar
  resource for XML documents
