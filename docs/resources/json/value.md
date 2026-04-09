# Value Resource

## Synopsis

Manages JSON values at specific JSONPath locations within JSON files. Supports
setting, reading, and removing values of any JSON type (string, number, boolean,
null, object, or array). Parent paths are created recursively when they don't
exist.

## Type

```text
OpenDsc.Json/Value
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | No        |

## Properties

### path

Absolute file path to the JSON document.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### jsonPath

JSONPath expression to locate the value. Must start with `$`.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### value

JSON value to set. Can be a string, number, boolean, null, object, or array.

```yaml
Type: any
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the value should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get a value

```powershell
dsc resource get -r OpenDsc.Json/Value --input '{"path":"/opt/myapp/config.json","jsonPath":"$.logging.level"}'
```

### Example 2 — Set a string value

```powershell
dsc resource set -r OpenDsc.Json/Value --input '{"path":"/opt/myapp/config.json","jsonPath":"$.logging.level","value":"Warning"}'
```

### Example 3 — Set an object value

```powershell
dsc resource set -r OpenDsc.Json/Value --input '{
  "path": "/opt/myapp/config.json",
  "jsonPath": "$.database",
  "value": {
    "host": "localhost",
    "port": 5432,
    "name": "appdb"
  }
}'
```

### Example 4 — Delete a value

```powershell
dsc resource delete -r OpenDsc.Json/Value --input '{"path":"/opt/myapp/config.json","jsonPath":"$.deprecated.setting"}'
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set application log level
    type: OpenDsc.Json/Value
    properties:
      path: /opt/myapp/config.json
      jsonPath: $.logging.level
      value: Warning

  - name: Set connection string
    type: OpenDsc.Json/Value
    properties:
      path: /opt/myapp/config.json
      jsonPath: $.connectionStrings.default
      value: "Host=db.example.com;Database=app;Username=appuser"
```

## Exit codes

| Code | Description         |
| :--- | :------------------ |
| 0    | Success             |
| 1    | Error               |
| 2    | Invalid JSON        |
| 3    | JSON file not found |
| 4    | Invalid argument    |
| 5    | IO error            |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.Xml/Element](../xml/element.md)
