# Parameter Validation and Compatibility

The OpenDSC Pull Server provides runtime parameter validation and compatibility
checking to ensure configuration parameters are valid and maintain backward
compatibility across versions.

## Overview

Parameter validation uses **JSON Schema** to validate parameter files against a
defined schema. The system enforces **semantic versioning (semver)** rules to
prevent breaking changes in patch and minor version updates.

### Key Features

- **JSON Schema Validation**: Validate parameter files against type-safe schemas
- **Semver Compatibility Checking**: Detect breaking vs. non-breaking parameter
  changes
- **Automatic Migration Detection**: Identify parameter files requiring
  migration
- **Major Version Scoping**: Isolate breaking changes to major version
  boundaries

## Parameter Schema Format

Parameter schemas define the expected structure and constraints for parameter
files:

```json
{
  "parameters": {
    "appName": {
      "type": "string",
      "description": "Application name",
      "minLength": 3,
      "maxLength": 50
    },
    "port": {
      "type": "int",
      "minValue": 1,
      "maxValue": 65535,
      "defaultValue": 8080
    },
    "environment": {
      "type": "string",
      "allowedValues": ["dev", "staging", "prod"]
    },
    "enabled": {
      "type": "bool",
      "defaultValue": true
    },
    "tags": {
      "type": "array",
      "description": "Custom tags"
    },
    "config": {
      "type": "object",
      "description": "Configuration object"
    }
  }
}
```

### Supported Parameter Types

| Type | Description | Constraints |
| ------ | ------------- | ------------- |
| `string` | Text value | `minLength`, `maxLength`, `allowedValues` |
| `secureString` | Text value (not logged or recorded by DSC) | `minLength`, `maxLength` |
| `int` | Integer number | `minValue`, `maxValue`, `allowedValues` |
| `bool` | Boolean value | None |
| `array` | Array of values | `minLength`, `maxLength` |
| `object` | JSON object | None |
| `secureObject` | Object (not logged or recorded by DSC) | None |

### Constraints

- **`allowedValues`**: Restrict values to a predefined list (enum)
- **`minValue` / `maxValue`**: Numeric range constraints (int only)
- **`minLength` / `maxLength`**: Length constraints (string, array)
- **`defaultValue`**: Default value if parameter not provided (makes parameter
  optional)
- **`description`**: Human-readable parameter description

## Uploading Parameter Schemas

Upload a parameter schema using the REST API:

```sh
# Upload v1.0.0 schema
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -F "version=1.0.0" \
  -F "parametersFile=@parameters.json" \
  "https://dsc-server/api/v1/configurations/my-app/parameters"
```

**PowerShell:**

```powershell
$headers = @{ Authorization = "Bearer $token" }
$form = @{
    version = "1.0.0"
    parametersFile = Get-Item "parameters.json"
}

Invoke-RestMethod -Method Put `
    -Uri "https://dsc-server/api/v1/configurations/my-app/parameters" `
    -Headers $headers `
    -Form $form
```

## Validating Parameter Files

Validate a parameter file against a specific schema version:

```sh
# Validate parameters against v1.0.0 schema
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @my-parameters.json \
  "https://dsc-server/api/v1/configurations/my-app/parameters/validate?version=1.0.0"
```

**Response (success):**

```json
{
  "isValid": true,
  "errors": []
}
```

**Response (validation errors):**

```json
{
  "isValid": false,
  "errors": [
    {
      "path": "/parameters/port",
      "message": "Value is 99999 but should be less than or equal to 65535",
      "code": "maximum"
    },
    {
      "path": "/parameters/environment",
      "message": "Value is not one of the allowed values",
      "code": "enum"
    }
  ]
}
```

## Compatibility Checking

When publishing a new parameter schema, the system automatically checks for
breaking changes against existing parameter files.

### Breaking Changes

Changes that **invalidate existing parameter files** are considered breaking:

- **ParameterRemoved**: Deleting a required parameter
- **BecameRequired**: Adding `required` constraint (removing `defaultValue`)
- **AllowedValuesReduced**: Removing values from `allowedValues` list
- **MinValueIncreased**: Increasing `minValue` constraint
- **MaxValueDecreased**: Decreasing `maxValue` constraint
- **MinLengthIncreased**: Increasing `minLength` constraint
- **MaxLengthDecreased**: Decreasing `maxLength` constraint

### Non-Breaking Changes

Changes that **remain compatible** with existing parameter files:

- **ParameterAdded**: Adding a new optional parameter (with `defaultValue`)
- **BecameOptional**: Removing `required` constraint (adding `defaultValue`)
- **AllowedValuesExpanded**: Adding values to `allowedValues` list

### Semver Enforcement Rules

| Version Type | Breaking Changes Allowed | Auto-Copy Parameters |
| -------------- | -------------------------- | ---------------------- |
| **Patch** (1.0.0 → 1.0.1) | ❌ No | ✅ Yes |
| **Minor** (1.0.0 → 1.1.0) | ❌ No | ✅ Yes |
| **Major** (1.0.0 → 2.0.0) | ✅ Yes | ❌ No (manual migration) |

## Publishing with Compatibility Checks

When uploading a new parameter schema, the system validates compatibility:

### Scenario 1: Non-Breaking Changes (Minor/Patch Update)

```sh
# Upload v1.1.0 schema with non-breaking changes
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -F "version=1.1.0" \
  -F "parametersFile=@parameters-v1.1.json" \
  "https://dsc-server/api/v1/configurations/my-app/parameters"
```

Response: **200 OK** - Parameters auto-copied from v1.0.0 to v1.1.0

### Scenario 2: Breaking Changes (Rejected in Minor/Patch)

```sh
# Upload v1.1.0 schema with breaking changes
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -F "version=1.1.0" \
  -F "parametersFile=@parameters-breaking.json" \
  "https://dsc-server/api/v1/configurations/my-app/parameters"
```

Response: **409 Conflict**

```json
{
  "success": false,
  "compatibilityReport": {
    "hasBreakingChanges": true,
    "breakingChanges": [
      {
        "parameterName": "database",
        "changeType": "ParameterRemoved",
        "details": "Parameter 'database' was removed"
      }
    ],
    "nonBreakingChanges": []
  },
  "migrationRequirements": [
    {
      "scopeTypeName": "Environment",
      "scopeValue": "production",
      "version": "1.0.0",
      "majorVersion": 1,
      "needsMigration": true,
      "errors": [
        {
          "path": "/parameters/database",
          "message": "Required property 'database' is missing",
          "code": "required"
        }
      ]
    }
  ]
}
```

The UI displays the **CompatibilityReportDialog** showing breaking changes and
affected parameter files.

### Scenario 3: Breaking Changes (Allowed in Major Update)

```sh
# Upload v2.0.0 schema with breaking changes
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -F "version=2.0.0" \
  -F "parametersFile=@parameters-v2.json" \
  "https://dsc-server/api/v1/configurations/my-app/parameters"
```

Response: **200 OK** - Breaking changes allowed with major version bump

The UI displays the **ParameterMigrationDialog** to guide manual migration.

## Parameter Migration Workflow

When breaking changes require migration:

### 1. View Migration Requirements

The server identifies parameter files requiring migration and shows validation
errors:

```json
{
  "migrationRequirements": [
    {
      "scopeTypeName": "Environment",
      "scopeValue": "production",
      "version": "1.0.0",
      "majorVersion": 1,
      "needsMigration": true,
      "errors": [
        {
          "path": "/parameters/appName",
          "message": "Required property 'appName' is missing",
          "code": "required"
        }
      ]
    }
  ]
}
```

### 2. Update Parameter Files

Manually update each affected parameter file to match the new schema:

```sh
# Update parameter file to v2.0.0
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -F "scopeTypeName=Environment" \
  -F "scopeValue=production" \
  -F "version=2.0.0" \
  -F "parametersFile=@updated-parameters.json" \
  "https://dsc-server/api/v1/configurations/my-app/parameter-files"
```

### 3. Verify Migration

The system validates the updated parameter file against the v2.0.0 schema. All
parameter files must be migrated before they can be used with the new schema
version.

## Major Version Scoping

Parameter files are scoped to their **major version**. This prevents mixing
incompatible parameter files:

- **v1.x.x parameters** → Only usable with v1.x.x schemas
- **v2.x.x parameters** → Only usable with v2.x.x schemas

When parameter files are auto-copied (patch/minor updates), they maintain their
major version.

Example:

```text
v1.0.0 (schema) → parameter-file-v1.0.0 (Global)
v1.1.0 (schema) → parameter-file-v1.1.0 (Global) [auto-copied from v1.0.0]
v2.0.0 (schema) → [requires manual migration]
```

## Best Practices

### Schema Design

1. **Use `defaultValue` generously** - Makes parameters optional and prevents
   breaking changes
2. **Version schemas semantically** - Follow semver rules strictly
3. **Document parameters** - Use `description` for all parameters
4. **Constrain types** - Use `allowedValues`, `minValue`, `maxValue` for
   validation

### Version Management

1. **Patch updates (x.x.X)** - Bug fixes, no schema changes
2. **Minor updates (x.X.x)** - Add optional parameters only
3. **Major updates (X.x.x)** - Breaking changes require migration

### Migration Strategy

1. **Plan migrations ahead** - Communicate breaking changes to users
2. **Test new schemas** - Validate against existing parameter files before
   publishing
3. **Migrate incrementally** - Update parameter files by scope to minimize
   impact
4. **Use UI for guidance** - The CompatibilityReportDialog and
   ParameterMigrationDialog provide clear migration paths

## API Reference

### Upload Parameter Schema

```http
PUT /api/v1/configurations/{name}/parameters
Content-Type: multipart/form-data

Form fields:
- version: string (semver)
- parametersFile: file (JSON)
```

### Validate Parameter File

```http
POST /api/v1/configurations/{name}/parameters/validate?version={version}
Content-Type: application/json

Body: <parameter JSON>
```

### Get Parameter Schemas

```http
GET /api/v1/configurations/{name}/parameters
```

### Get Specific Schema

```http
GET /api/v1/configurations/{name}/parameters/{version}
```

## See Also

- [Semantic Versioning](semantic-versioning.md)
- [Parameter Merging](parameter-merging.md)
- [Configuration Management](configuration-management.md)
