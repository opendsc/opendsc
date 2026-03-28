---
description: Reference for the OpenDsc.Xml/Element resource, which manages XML element content and attributes.
title: "OpenDsc.Xml/Element"
date: 2026-03-27
topic: reference
---

# OpenDsc.Xml/Element

## Synopsis

Manages XML element content and attributes using XPath expressions. Supports
creating,
modifying, and removing elements. Parent elements are created recursively when
they don't exist.
Uses the hybrid pattern with both `_exist` (for the element) and `_purge` (for
attributes).

## Type name

```plaintext
OpenDsc.Xml/Element
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | No        |

## Properties

| Property     | Type                     | Required | Access     | Description                                                                                 |
| :----------- | :----------------------- | :------- | :--------- | :------------------------------------------------------------------------------------------ |
| `path`       | string                   | Yes      | Read/Write | Absolute file path to the XML document.                                                     |
| `xPath`      | string                   | Yes      | Read/Write | XPath expression to locate the element.                                                     |
| `value`      | string                   | No       | Read/Write | Text content (inner text) of the element.                                                   |
| `attributes` | object (string → string) | No       | Read/Write | Attributes to set on the element as key-value pairs.                                        |
| `namespaces` | object (string → string) | No       | Read/Write | Namespace prefix mappings for XPath evaluation.                                             |
| `_purge`     | bool                     | No       | Write-Only | When `true`, removes attributes not in the list. When `false` (default), only adds/updates. |
| `_exist`     | bool                     | No       | Read/Write | Whether the element should exist. Defaults to `true`.                                       |

## Examples

### Example 1 — Get an XML element

```powershell
dsc resource get -r OpenDsc.Xml/Element --input '{"path":"/opt/myapp/web.config","xPath":"//configuration/appSettings/add[@key=\"AppName\"]"}'
```

### Example 2 — Set element attributes

```powershell
dsc resource set -r OpenDsc.Xml/Element --input '{
  "path": "/opt/myapp/web.config",
  "xPath": "//configuration/appSettings/add[@key=\"AppName\"]",
  "attributes": {
    "key": "AppName",
    "value": "MyApplication"
  }
}'
```

### Example 3 — Set element value with namespace

```powershell
dsc resource set -r OpenDsc.Xml/Element --input '{
  "path": "/opt/myapp/config.xml",
  "xPath": "//ns:configuration/ns:setting",
  "namespaces": {
    "ns": "http://example.com/config"
  },
  "value": "Enabled"
}'
```

### Example 4 — Delete an element

```powershell
dsc resource delete -r OpenDsc.Xml/Element --input '{"path":"/opt/myapp/web.config","xPath":"//configuration/appSettings/add[@key=\"Deprecated\"]"}'
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set application name
    type: OpenDsc.Xml/Element
    properties:
      path: C:\inetpub\wwwroot\web.config
      xPath: "//configuration/appSettings/add[@key='AppName']"
      attributes:
        key: AppName
        value: MyApplication

  - name: Set connection string
    type: OpenDsc.Xml/Element
    properties:
      path: C:\inetpub\wwwroot\web.config
      xPath: "//configuration/connectionStrings/add[@name='Default']"
      attributes:
        name: Default
        connectionString: "Server=db;Database=app;Integrated Security=true"
      _purge: true
```

## Exit codes

| Code | Description              |
| :--- | :----------------------- |
| 0    | Success                  |
| 1    | Error                    |
| 2    | Invalid JSON             |
| 3    | XML file not found       |
| 4    | Invalid XML              |
| 5    | Invalid XPath expression |
| 6    | Invalid argument         |
| 7    | IO error                 |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.Json/Value](../json/value.md)
