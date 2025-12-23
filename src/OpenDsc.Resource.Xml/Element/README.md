# OpenDsc.Xml/Element

## Synopsis

Cross-platform DSC resource for managing XML element content and attributes.

## Description

The `OpenDsc.Xml/Element` resource manages XML documents across Windows,
Linux, and macOS by creating, updating, and deleting XML elements and
attributes using XPath expressions. Parent elements are created recursively
as needed.

**XPath support:** Locate elements using standard XPath expressions with
optional namespace prefix mappings.

**Attribute management with `_purge` pattern:** Control attribute behavior
with two modes:

- **Additive mode** (`_purge: false`, default) - Only adds/updates specified
  attributes, preserving existing ones
- **Exact mode** (`_purge: true`) - Ensures element has ONLY the specified
  attributes, removing unlisted ones

The XML file must exist before using the resource. File encoding is
automatically detected and preserved when updating documents.

## Requirements

- Cross-platform support: Windows, Linux, macOS
- File system write permissions for the XML file
- Valid XML document structure

## Capabilities

- **get** - Read XML element text content and attributes
- **set** - Create or update XML elements and attributes
- **delete** - Remove XML elements

## Properties

### Required Properties

- **path** (string) - Absolute file path to the XML document
- **xPath** (string) - XPath expression to locate the element

### Optional Properties

- **value** (string) - Text content (inner text) of the element
- **attributes** (object) - Key-value pairs of attributes to set on the element
- **namespaces** (object) - Namespace prefix mappings for XPath evaluation
  (e.g., `{"ns": "http://example.com/schema"}`)
- **_purge** (boolean) - When `true`, removes attributes not in the
  `attributes` dictionary. When `false` (default), only adds/updates
  specified attributes
- **_exist** (boolean) - Whether the element should exist (default: `true`)

## Examples

### Create element with text content

```powershell
$config = @'
path: /etc/app/config.xml
xPath: /configuration/logging/level
value: Debug
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

### Create element with attributes

```powershell
$config = @'
path: C:\config\app.config
xPath: /configuration/appSettings/add
attributes:
  key: DatabaseConnection
  value: Server=localhost;Database=db
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

### Update attributes (additive mode)

Specified attributes are added/updated; existing attributes are preserved:

```powershell
$config = @'
path: /etc/app/settings.xml
xPath: /configuration/feature
attributes:
  enabled: true
  version: 2.0
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

### Update attributes (purge mode)

Element has ONLY the specified attributes; unlisted attributes are removed:

```powershell
$config = @'
path: /etc/app/settings.xml
xPath: /configuration/feature
attributes:
  enabled: true
  version: 2.0
_purge: true
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

### Create nested elements

Parent elements are created recursively if they don't exist:

```powershell
$config = @'
path: /etc/app/config.xml
xPath: /root/level1/level2/level3/setting
value: DeepValue
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

### Delete element

```powershell
$config = @'
path: C:\config\app.xml
xPath: /configuration/obsoleteSection
'@

dsc resource delete -r OpenDsc.Xml/Element -i $config
```

### Work with namespaced XML

```powershell
$config = @'
path: /etc/app/config.xml
xPath: /ns:configuration/ns:setting
value: Value
namespaces:
  ns: http://example.com/namespace
'@

dsc resource set -r OpenDsc.Xml/Element -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - XML file not found
- **4** - Invalid XML
- **5** - Invalid XPath expression
- **6** - Invalid argument
- **7** - IO error
