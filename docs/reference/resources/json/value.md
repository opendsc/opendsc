---
description: Reference for the OpenDsc.Json/Value resource, which manages JSON values at JSONPath locations.
title: "OpenDsc.Json/Value"
date: 2026-03-27
topic: reference
---

# OpenDsc.Json/Value

## Synopsis

Manages JSON values at specific JSONPath locations within JSON files. Supports
setting, reading,
and removing values of any JSON type (string, number, boolean, null, object, or
array). Parent
paths are created recursively when they don't exist.

## Type name

```plaintext
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

| Property   | Type   | Required | Access     | Description                                                                  |
| :--------- | :----- | :------- | :--------- | :--------------------------------------------------------------------------- |
| `path`     | string | Yes      | Read/Write | Absolute file path to the JSON document.                                     |
| `jsonPath` | string | Yes      | Read/Write | JSONPath expression to locate the value. Must start with `$`.                |
| `value`    | any    | No       | Read/Write | JSON value to set. Can be a string, number, boolean, null, object, or array. |
| `_exist`   | bool   | No       | Read/Write | Whether the value should exist. Defaults to `true`.                          |

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
