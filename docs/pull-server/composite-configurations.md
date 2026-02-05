# Composite Configuration Guide

The OpenDSC Pull Server supports **composite configurations** (also called
meta configurations) that allow you to combine multiple existing
configurations into a single logical unit. Composite configurations provide
orchestration and composition capabilities without containing their own
configuration files or parameters.

## Overview

A **composite configuration** is a container that references other
configurations (called child configurations). This enables:

- **Configuration Composition** - Combine multiple configurations into a
  single deployment unit
- **Modular Architecture** - Build configurations from reusable components
- **Version Pinning** - Lock child configurations to specific versions or
  allow automatic updates
- **Ordered Execution** - Control the sequence of child configuration
  application
- **Centralized Management** - Manage related configurations as a single
  entity

## Key Concepts

### Composite vs Regular Configurations

| Feature | Regular Configuration | Composite Configuration |
| --- | --- | --- |
| **Contains Files** | Yes - DSC YAML files | No - references only |
| **Contains Parameters** | Optional | No - child configs provide parameters |
| **Direct Assignment** | Can be assigned to nodes | Can be assigned to nodes |
| **Versioning** | Yes | Yes |
| **Draft/Publish** | Yes | Yes |
| **Child Configs** | N/A | Contains 1+ child configurations |

### Nesting Rules

**Composites cannot contain other composites** - only regular configurations
can be added as children. This prevents circular dependencies and keeps the
composition structure simple.

### Entry Point

Composite configurations have an `EntryPoint` property (default:
`main.dsc.yaml`) which defines the name of the orchestrator file that the
server generates. This file is automatically created during bundle
generation and contains references to all child configurations.

## Creating Composite Configurations

### Create a Composite Configuration

**Endpoint:** `POST /api/v1/composite-configurations`

```json
{
  "name": "FullWebStack",
  "description": "Complete web server stack with database and monitoring",
  "entryPoint": "main.dsc.yaml"
}
```

**Response:**

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "name": "FullWebStack",
  "description": "Complete web server stack with database and monitoring",
  "entryPoint": "main.dsc.yaml",
  "isServerManaged": true,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

**Notes:**

- `entryPoint` defaults to `main.dsc.yaml` if omitted
- `isServerManaged` is always `true` for composites (server generates the
  orchestrator file)

### Create a Version

**Endpoint:** `POST /api/v1/composite-configurations/{name}/versions`

```json
{
  "version": "1.0.0",
  "isDraft": true
}
```

**Response:**

```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "compositeConfigurationId": "12345678-1234-1234-1234-123456789012",
  "version": "1.0.0",
  "isDraft": true,
  "isArchived": false,
  "items": [],
  "createdAt": "2024-01-15T10:00:00Z"
}
```

## Managing Child Configurations

### Add a Child Configuration

**Endpoint:** `POST /api/v1/composite-configurations/{name}/versions/`
`{version}/children`

```json
{
  "childConfigurationName": "WebServer",
  "activeVersionId": null,
  "order": 1
}
```

**Parameters:**

- `childConfigurationName` - Name of the regular configuration to add
- `activeVersionId` - (Optional) Pin to specific version GUID, or `null` for
  latest published
- `order` - Execution order (1-based)

**Response:**

```json
{
  "id": "11111111-2222-3333-4444-555555555555",
  "compositeConfigurationVersionId": "87654321-4321-4321-4321-210987654321",
  "childConfigurationId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "childConfigurationName": "WebServer",
  "activeVersionId": null,
  "order": 1
}
```

**Notes:**

- When `activeVersionId` is `null`, the child will automatically use the
  latest published version
- When `activeVersionId` is set to a specific version GUID, the child is
  pinned to that version
- `order` determines the sequence in which child configurations are applied

### Update a Child Configuration

**Endpoint:** `PUT /api/v1/composite-configurations/{name}/versions/`
`{version}/children/{childItemId}`

```json
{
  "activeVersionId": "ffffffff-eeee-dddd-cccc-bbbbbbbbbbbb",
  "order": 1
}
```

Use this to:

- Pin a child to a specific version
- Unpin a child (set `activeVersionId` to `null`)
- Reorder children

### Remove a Child Configuration

**Endpoint:** `DELETE /api/v1/composite-configurations/{name}/versions/`
`{version}/children/{childItemId}`

Removes a child configuration from the composite version.

## Publishing and Archiving

### Publish a Version

**Endpoint:** `PUT /api/v1/composite-configurations/{name}/versions/`
`{version}/publish`

Publishes a draft version, making it available for node assignment.

**Response:**

```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "compositeConfigurationId": "12345678-1234-1234-1234-123456789012",
  "version": "1.0.0",
  "isDraft": false,
  "isArchived": false,
  "items": [
    {
      "id": "11111111-2222-3333-4444-555555555555",
      "childConfigurationName": "WebServer",
      "order": 1
    }
  ],
  "createdAt": "2024-01-15T10:00:00Z"
}
```

## Node Assignment

### Assign to a Node

**Endpoint:** `PUT /api/v1/nodes/{nodeId}/configuration`

```json
{
  "compositeConfigurationName": "FullWebStack",
  "isComposite": true,
  "activeVersionId": null
}
```

**Parameters:**

- `compositeConfigurationName` - Name of the composite configuration
- `isComposite` - Must be `true` for composite configurations
- `activeVersionId` - (Optional) Pin to specific composite version GUID, or
  `null` for latest published

**Mutual Exclusivity:** A node can be assigned either:

- A regular configuration (`configurationName` + `activeVersionId`)
- OR a composite configuration (`compositeConfigurationName` +
  `activeCompositeVersionId`)

But not both. Assigning one clears the other.

## Bundle Generation

When a node requests a configuration bundle for a composite configuration,
the Pull Server automatically generates a ZIP bundle containing:

### Bundle Structure

```text
root/
├── main.dsc.yaml              # Orchestrator file (auto-generated)
├── WebServer/
│   └── main.dsc.yaml          # Child configuration files
│       modules/
│       ├── iis.dsc.yaml
│       └── firewall.dsc.yaml
├── Database/
│   └── main.dsc.yaml          # Another child configuration
│       modules/
│       └── sqlserver.dsc.yaml
└── Monitoring/
    └── main.dsc.yaml          # Third child configuration
        modules/
        └── prometheus.dsc.yaml
```

### Orchestrator File (main.dsc.yaml)

The server automatically generates the orchestrator file that references all
child configurations:

```yaml
# Auto-generated orchestrator for composite configuration
# FullWebStack
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: WebServer
    type: Microsoft.DSC/Include
    properties:
      configurationFile: WebServer/main.dsc.yaml
      parametersFile: WebServer/parameters.yaml

  - name: Database
    type: Microsoft.DSC/Include
    properties:
      configurationFile: Database/main.dsc.yaml
      parametersFile: Database/parameters.yaml

  - name: Monitoring
    type: Microsoft.DSC/Include
    properties:
      configurationFile: Monitoring/main.dsc.yaml
      parametersFile: Monitoring/parameters.yaml
```

### Parameter Merging

Each child configuration's parameters are merged individually according to
the node's scope tags and node-specific parameters. The server:

1. Resolves the child configuration version (pinned or latest published)
2. Merges parameters for that specific child configuration
3. Applies the merged parameters to the child's files
4. Packages all children into the composite bundle

### Version Resolution

For each child configuration:

- If `activeVersionId` is set on the composite item → uses that specific version
- If `activeVersionId` is `null` → uses the latest published version of the
  child configuration

This allows flexible version management where some children are pinned and
others automatically track the latest version.

## Checksum Calculation

The Pull Server calculates a checksum for composite configurations by:

1. Generating the complete bundle (orchestrator + all child configurations
   with merged parameters)
2. Computing SHA256 hash of the entire ZIP bundle
3. Returning the checksum to the node

This ensures nodes only download bundles when any child configuration or
parameter changes.

## Complete Example Workflow

### 1. Create Regular Configurations

First, create the child configurations:

```sh
# Create WebServer configuration
POST /api/v1/configurations
{
  "name": "WebServer",
  "version": "1.0.0",
  "files": ["main.dsc.yaml", "modules/iis.dsc.yaml"]
}

# Publish WebServer
PUT /api/v1/configurations/WebServer/versions/1.0.0/publish

# Create Database configuration
POST /api/v1/configurations
{
  "name": "Database",
  "version": "1.0.0",
  "files": ["main.dsc.yaml", "modules/sqlserver.dsc.yaml"]
}

# Publish Database
PUT /api/v1/configurations/Database/versions/1.0.0/publish
```

### 2. Create Composite Configuration

```sh
POST /api/v1/composite-configurations
{
  "name": "FullWebStack",
  "description": "Complete web application stack"
}
```

**Response:** Save the `id` from the response.

### 3. Create Composite Version

```sh
POST /api/v1/composite-configurations/FullWebStack/versions
{
  "version": "1.0.0",
  "isDraft": true
}
```

### 4. Add Child Configurations

```sh
# Add WebServer as first child
POST /api/v1/composite-configurations/FullWebStack/versions/1.0.0/children
{
  "childConfigurationName": "WebServer",
  "activeVersionId": null,
  "order": 1
}

# Add Database as second child
POST /api/v1/composite-configurations/FullWebStack/versions/1.0.0/children
{
  "childConfigurationName": "Database",
  "activeVersionId": null,
  "order": 2
}
```

### 5. Publish Composite Version

```sh
PUT /api/v1/composite-configurations/FullWebStack/versions/1.0.0/publish
```

### 6. Assign to Node

```sh
PUT /api/v1/nodes/{nodeId}/configuration
{
  "compositeConfigurationName": "FullWebStack",
  "isComposite": true,
  "activeVersionId": null
}
```

### 7. Node Downloads Bundle

When the LCM on the node requests the configuration:

```sh
GET /api/v1/nodes/{nodeId}/configuration/bundle
```

The server generates and returns a ZIP bundle containing:

- Orchestrator file (`main.dsc.yaml`)
- All child configuration files with merged parameters
- Proper directory structure for each child

## Version Management Scenarios

### Scenario 1: All Children Track Latest

```json
{
  "version": "1.0.0",
  "items": [
    {
      "childConfigurationName": "WebServer",
      "activeVersionId": null,
      "order": 1
    },
    {
      "childConfigurationName": "Database",
      "activeVersionId": null,
      "order": 2
    }
  ]
}
```

**Behavior:** When you publish new versions of WebServer or Database, nodes
automatically receive the latest versions through the composite.

### Scenario 2: Mixed Pinning

```json
{
  "version": "1.0.0",
  "items": [
    {
      "childConfigurationName": "WebServer",
      "activeVersionId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "order": 1
    },
    {
      "childConfigurationName": "Database",
      "activeVersionId": null,
      "order": 2
    }
  ]
}
```

**Behavior:** WebServer is locked to a specific version while Database tracks
the latest published version.

### Scenario 3: Node-Level Version Pinning

```json
PUT /api/v1/nodes/{nodeId}/configuration
{
  "compositeConfigurationName": "FullWebStack",
  "isComposite": true,
  "activeVersionId": "12345678-1234-1234-1234-123456789012"
}
```

**Behavior:** The node is pinned to a specific composite version. Child
configurations use whatever versions were defined in that composite version.

## API Endpoints Reference

| Method | Endpoint | Description |
| --- | --- | --- |
| `POST` | `/api/v1/composite-configurations` | Create composite configuration |
| `GET` | `/api/v1/composite-configurations` | List all composite configurations |
| `GET` | `/api/v1/composite-configurations/{name}` | Get composite details |
| `PUT` | `/api/v1/composite-configurations/{name}` | Update composite |
| `DELETE` | `/api/v1/composite-configurations/{name}` | Delete composite |
| `POST` | `/api/v1/composite-configurations/{name}/versions` | Create version |
| `GET` | `/api/v1/composite-configurations/{name}/versions` | List versions |
| `GET` | `/api/v1/composite-configurations/{name}/versions/{version}` | Get version details |
| `PUT` | `/api/v1/composite-configurations/{name}/versions/{version}/publish` | Publish draft version |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}` | Delete version |
| `POST` | `/api/v1/composite-configurations/{name}/versions/{version}/children` | Add child configuration |
| `PUT` | `/api/v1/composite-configurations/{name}/versions/{version}/children/{id}` | Update child configuration |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}/children/{id}` | Remove child configuration |

## Best Practices

### 1. Logical Grouping

Use composite configurations to group related configurations that are always
deployed together:

```text
FullWebStack (Composite)
├── WebServer
├── Database
└── Monitoring
```

### 2. Version Stability

- **Pin stable dependencies** - Lock critical child configurations to tested
  versions
- **Track active development** - Let non-critical children track latest for
  automatic updates

### 3. Ordering Strategy

Order child configurations by dependency:

```json
{
  "order": 1,  // Database (foundation)
  "order": 2,  // WebServer (depends on database)
  "order": 3   // Monitoring (observes both)
}
```

### 4. Composite Versioning

Create new composite versions when:

- Adding or removing child configurations
- Changing child configuration order
- Pinning/unpinning child versions

### 5. Testing Workflow

Use draft versions for testing:

```sh
# Create draft composite version
POST /api/v1/composite-configurations/FullWebStack/versions
{"version": "2.0.0", "isDraft": true}

# Add/update children
POST /api/v1/composite-configurations/FullWebStack/versions/2.0.0/children
{...}

# Test on non-production nodes
PUT /api/v1/nodes/{testNodeId}/configuration
{"compositeConfigurationName": "FullWebStack", "activeVersionId": "{v2.0.0-guid}"}

# Publish when ready
PUT /api/v1/composite-configurations/FullWebStack/versions/2.0.0/publish
```

## Troubleshooting

### Error: "Cannot nest composite configurations"

**Cause:** Attempting to add a composite configuration as a child of another
composite.

**Solution:** Only regular configurations can be children of composites.
Flatten your composition structure.

### Error: "Child configuration not found"

**Cause:** The referenced child configuration doesn't exist or has been deleted.

**Solution:** Ensure the child configuration exists before adding it to a
composite:

```sh
GET /api/v1/configurations/{childName}
```

### Bundle Contains No Child Configurations

**Cause:** Composite version has no child configurations added, or all
children were removed.

**Solution:** Add at least one child configuration to the composite version
before publishing.

### Checksum Changes Frequently

**Cause:** Child configurations using `activeVersionId: null` track latest
versions. Each new publish changes the bundle.

**Solution:** Pin stable versions using `activeVersionId` to prevent
automatic updates:

```json
{
  "activeVersionId": "specific-version-guid"
}
```

## See Also

- [Configuration Management Guide](configuration-management.md) - Regular
  configuration management
- [Parameter Merging](parameter-merging.md) - How parameters are merged for
  child configurations
- [Scope System](scope-system.md) - Organizing parameters with scopes
- [Quick Start Guide](quickstart.md) - Getting started with the Pull Server
