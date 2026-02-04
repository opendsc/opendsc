# Quick Start: Pull Server with Scope-Based Parameters

This tutorial walks through setting up the OpenDSC Pull Server with scope-based
parameter management. You'll create custom scope types, upload parameters at
different scope levels, and see how parameters merge when nodes download
configurations.

**What You'll Learn:**

- Creating custom scope types and values
- Uploading parameters at different scope levels
- Tagging nodes with multiple scope values
- Understanding parameter merge order and precedence
- Viewing parameter provenance

**Prerequisites:**

- OpenDSC Pull Server running and accessible
- Registration key for node registration
- Admin API key for administrative operations
- Basic familiarity with YAML and REST APIs

## Tutorial Scenario

We'll configure a web server with parameters managed by different teams:

- **Default scope**: Global baseline settings (Platform team)
- **Region scope**: Regional compliance (US-West, EU-Central)
- **Environment scope**: Environment-specific limits (Development, Production)
- **Node scope**: Server-specific overrides

## API Documentation Reference

All API operations in this tutorial are documented in the interactive API
reference. While running the Pull Server, navigate to:

```text
http://localhost:5000/scalar/v1
```

The Scalar API documentation provides:

- Interactive API explorer with request/response examples
- Authentication setup (API keys, mTLS)
- Full request/response schemas
- Try-it-out functionality for testing endpoints

**Recommended Workflow:**

1. Open Scalar documentation in your browser
2. Follow this tutorial for the conceptual workflow
3. Use Scalar to execute the actual API calls with proper authentication

## Step 1: Create Scope Types

Create custom scope types for regional and environmental organization.

**API Endpoint:** `POST /api/v1/scope-types`

### Create Region Scope Type

Create a scope type for geographic regions with precedence 10:

```yaml
name: Region
precedence: 10
description: Geographic region for compliance and networking
allowDefaultValue: false
```

**Response:** Returns the created scope type with a unique GUID:

```json
{
  "id": "aaaaaaaa-1111-2222-3333-444444444444",
  "name": "Region",
  "precedence": 10,
  "description": "Geographic region for compliance and networking",
  "allowDefaultValue": false,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

**Save the `id` value** - you'll need it for creating scope values.

### Create Environment Scope Type

Create a scope type for deployment environments with precedence 15:

```yaml
name: Environment
precedence: 15
description: Deployment environment (dev, staging, prod)
allowDefaultValue: false
```

**Response:**

```json
{
  "id": "bbbbbbbb-2222-3333-4444-555555555555",
  "name": "Environment",
  "precedence": 15,
  "description": "Deployment environment (dev, staging, prod)",
  "allowDefaultValue": false,
  "createdAt": "2024-01-15T10:01:00Z"
}
```

## Step 2: Create Scope Values

Create specific values for each scope type.

**API Endpoint:** `POST /api/v1/scope-types/{scopeTypeId}/values`

### Create Region Values

Using the Region scope type ID (`aaaaaaaa-1111-2222-3333-444444444444`):

**US-West Region:**

```yaml
name: US-West
description: US West Coast region
```

**Response:**

```json
{
  "id": "11111111-aaaa-bbbb-cccc-dddddddddddd",
  "scopeTypeId": "aaaaaaaa-1111-2222-3333-444444444444",
  "name": "US-West",
  "description": "US West Coast region",
  "createdAt": "2024-01-15T10:02:00Z"
}
```

**EU-Central Region:**

```yaml
name: EU-Central
description: EU Central region (GDPR compliance)
```

**Response:**

```json
{
  "id": "22222222-bbbb-cccc-dddd-eeeeeeeeeeee",
  "scopeTypeId": "aaaaaaaa-1111-2222-3333-444444444444",
  "name": "EU-Central",
  "description": "EU Central region (GDPR compliance)",
  "createdAt": "2024-01-15T10:03:00Z"
}
```

### Create Environment Values

Using the Environment scope type ID (`bbbbbbbb-2222-3333-4444-555555555555`):

**Development Environment:**

```yaml
name: Development
description: Development environment with verbose logging
```

**Production Environment:**

```yaml
name: Production
description: Production environment with strict limits
```

## Step 3: Upload Parameters at Each Scope Level

Upload parameters for the configuration `webserver-config` at different
scope levels.

**API Endpoint:** `POST /api/v1/parameters/{scopeTypeId}/{configurationId}`

Query parameter: `?scopeValue={scopeValueId}` (omit for Default scope)

### Default Scope Parameters

Upload global baseline parameters that apply to all servers.

**Scope Type ID:** `00000000-0000-0000-0000-000000000001` (Default scope)

**Configuration Name:** `webserver-config`

**Parameters (YAML):**

```yaml
# Global defaults for all web servers
appSettings:
  maxConcurrentRequests: 1000
  requestTimeout: 30
  cacheEnabled: true

resourceLimits:
  maxMemoryMb: 2048
  maxCpuPercent: 80

loggingSettings:
  level: Information
  retentionDays: 30

networkSettings:
  corsEnabled: true
  rateLimiting:
    requestsPerMinute: 100
```

### Region Scope - US-West

Upload US-West specific parameters (CCPA compliance, higher capacity).

**Scope Type ID:** `aaaaaaaa-1111-2222-3333-444444444444` (Region)

**Scope Value ID:** `11111111-aaaa-bbbb-cccc-dddddddddddd` (US-West)

**Parameters (YAML):**

```yaml
# US-West regional parameters
appSettings:
  maxConcurrentRequests: 2000  # Override: higher capacity

resourceLimits:
  maxMemoryMb: 4096            # Override: more memory

loggingSettings:
  retentionDays: 365           # CCPA compliance requirement

complianceSettings:
  framework: CCPA
  dataResidency:
    region: US
    allowTransfer: false

networkSettings:
  cdn:
    enabled: true
    provider: CloudFlare
    edgeLocations:
      - us-west-1
      - us-west-2
```

### Region Scope - EU-Central

Upload EU-Central specific parameters (GDPR compliance).

**Scope Type ID:** `aaaaaaaa-1111-2222-3333-444444444444` (Region)

**Scope Value ID:** `22222222-bbbb-cccc-dddd-eeeeeeeeeeee` (EU-Central)

**Parameters (YAML):**

```yaml
# EU-Central regional parameters
complianceSettings:
  framework: GDPR
  dataResidency:
    region: EU
    allowTransfer: false
  rightToBeForgotten:
    enabled: true
    automatedDeletion: true

loggingSettings:
  retentionDays: 90            # GDPR requirement
  anonymizeIpAddresses: true

networkSettings:
  cdn:
    enabled: true
    edgeLocations:
      - eu-central-1
      - eu-west-1
```

### Environment Scope - Production

Upload production-specific parameters (strict limits, minimal logging).

**Scope Type ID:** `bbbbbbbb-2222-3333-4444-555555555555` (Environment)

**Scope Value ID:** (Production scope value ID from Step 2)

**Parameters (YAML):**

```yaml
# Production environment parameters
appSettings:
  maxConcurrentRequests: 5000  # Production capacity

resourceLimits:
  maxMemoryMb: 8192            # Production resources
  maxCpuPercent: 90

loggingSettings:
  level: Warning               # Minimal logging in production

monitoring:
  enabled: true
  alerting:
    enabled: true
    thresholds:
      cpuPercent: 85
      memoryPercent: 90
      errorRate: 1

highAvailability:
  enabled: true
  minimumHealthyInstances: 2
```

## Step 4: Tag Node with Scope Values

Tag a specific node to receive parameters from multiple scopes.

**API Endpoint:** `POST /api/v1/nodes/{nodeId}/tags`

### Tag Production Node in US-West

For node `webserver01.example.com` (production server in US-West):

**Tag with Region scope (US-West):**

```yaml
scopeTypeId: aaaaaaaa-1111-2222-3333-444444444444  # Region
scopeValueId: 11111111-aaaa-bbbb-cccc-dddddddddddd  # US-West
```

**Tag with Environment scope (Production):**

```yaml
scopeTypeId: bbbbbbbb-2222-3333-4444-555555555555  # Environment
scopeValueId: <production-scope-value-id>          # Production
```

**Important:** Nodes can have only ONE tag per scope type. If you tag with
multiple values from the same scope type, the last tag wins.

## Step 5: View Parameter Merge Result

Check how parameters merge for the tagged node.

**API Endpoint:** `GET /api/v1/parameters/{scopeTypeId}/
{configurationId}/provenance`

Query parameter: `?scopeValue={scopeValueId}` for non-Default scopes

### View Provenance for webserver01.example.com

The provenance API shows which scope contributed each parameter value.

**Example Provenance Response:**

```json
{
  "configurationId": "webserver-config",
  "parameters": {
    "appSettings": {
      "maxConcurrentRequests": {
        "value": 5000,
        "source": "Environment:Production",
        "scopeTypeId": "bbbbbbbb-2222-3333-4444-555555555555",
        "scopeValueId": "<production-scope-value-id>",
        "precedence": 15,
        "overrides": [
          {
            "value": 2000,
            "source": "Region:US-West",
            "precedence": 10
          },
          {
            "value": 1000,
            "source": "Default",
            "precedence": 0
          }
        ]
      },
      "requestTimeout": {
        "value": 30,
        "source": "Default",
        "scopeTypeId": "00000000-0000-0000-0000-000000000001",
        "precedence": 0
      },
      "cacheEnabled": {
        "value": true,
        "source": "Default",
        "precedence": 0
      }
    },
    "resourceLimits": {
      "maxMemoryMb": {
        "value": 8192,
        "source": "Environment:Production",
        "precedence": 15,
        "overrides": [
          {
            "value": 4096,
            "source": "Region:US-West",
            "precedence": 10
          },
          {
            "value": 2048,
            "source": "Default",
            "precedence": 0
          }
        ]
      },
      "maxCpuPercent": {
        "value": 90,
        "source": "Environment:Production",
        "precedence": 15,
        "overrides": [
          {
            "value": 80,
            "source": "Default",
            "precedence": 0
          }
        ]
      }
    },
    "loggingSettings": {
      "level": {
        "value": "Warning",
        "source": "Environment:Production",
        "precedence": 15,
        "overrides": [
          {
            "value": "Information",
            "source": "Default",
            "precedence": 0
          }
        ]
      },
      "retentionDays": {
        "value": 365,
        "source": "Region:US-West",
        "precedence": 10,
        "overrides": [
          {
            "value": 30,
            "source": "Default",
            "precedence": 0
          }
        ]
      },
      "anonymizeIpAddresses": {
        "value": null,
        "source": "none",
        "note": "EU-Central only, not set for US-West"
      }
    },
    "complianceSettings": {
      "framework": {
        "value": "CCPA",
        "source": "Region:US-West",
        "precedence": 10
      },
      "dataResidency": {
        "region": {
          "value": "US",
          "source": "Region:US-West",
          "precedence": 10
        },
        "allowTransfer": {
          "value": false,
          "source": "Region:US-West",
          "precedence": 10
        }
      }
    },
    "monitoring": {
      "enabled": {
        "value": true,
        "source": "Environment:Production",
        "precedence": 15
      }
    }
  }
}
```

### Understanding Provenance Output

**Key Fields:**

- `value`: The final merged value used
- `source`: Which scope contributed this value (format: `ScopeType:ScopeValue`)
- `scopeTypeId`: GUID of the scope type
- `scopeValueId`: GUID of the scope value (if applicable)
- `precedence`: Numeric precedence (higher wins)
- `overrides`: Array of values that were overridden by higher precedence scopes

**Merge Order** (lowest to highest precedence):

1. Default (precedence 0) - base values
2. Region:US-West (precedence 10) - regional overrides
3. Environment:Production (precedence 15) - environment overrides (wins)

## Step 6: Download Configuration Bundle

When the node requests its configuration, it receives a ZIP bundle with
merged parameters.

**API Endpoint (Node):** `GET /api/v1/nodes/{nodeId}/configuration`

**Bundle Contents:**

```text
configuration-bundle.zip
├── main.dsc.yaml              # Main configuration file
├── parameters.yaml            # Merged parameters (final result)
├── modules/                   # Any required modules
└── metadata.json              # Configuration metadata
```

**parameters.yaml** (merged result):

```yaml
# Final merged parameters for webserver01.example.com
# Merge order: Default → Region:US-West → Environment:Production

appSettings:
  maxConcurrentRequests: 5000  # From Environment:Production
  requestTimeout: 30           # From Default
  cacheEnabled: true           # From Default

resourceLimits:
  maxMemoryMb: 8192            # From Environment:Production
  maxCpuPercent: 90            # From Environment:Production

loggingSettings:
  level: Warning               # From Environment:Production
  retentionDays: 365           # From Region:US-West

networkSettings:
  corsEnabled: true            # From Default
  rateLimiting:
    requestsPerMinute: 100     # From Default
  cdn:                         # From Region:US-West
    enabled: true
    provider: CloudFlare
    edgeLocations:
      - us-west-1
      - us-west-2

complianceSettings:
  framework: CCPA              # From Region:US-West
  dataResidency:
    region: US
    allowTransfer: false

monitoring:                    # From Environment:Production
  enabled: true
  alerting:
    enabled: true
    thresholds:
      cpuPercent: 85
      memoryPercent: 90
      errorRate: 1

highAvailability:              # From Environment:Production
  enabled: true
  minimumHealthyInstances: 2
```

## Merge Behavior Examples

### Deep Merge (Objects)

Objects are deeply merged - keys from higher precedence override, but other
keys are preserved.

**Default:**

```yaml
networkSettings:
  corsEnabled: true
  rateLimiting:
    requestsPerMinute: 100
```

**Region:US-West:**

```yaml
networkSettings:
  cdn:
    enabled: true
```

**Merged Result:**

```yaml
networkSettings:
  corsEnabled: true           # From Default (preserved)
  rateLimiting:               # From Default (preserved)
    requestsPerMinute: 100
  cdn:                        # From Region (added)
    enabled: true
```

### Array Replace

Arrays are replaced entirely by higher precedence scopes (not merged
element-by-element).

**Default:**

```yaml
networkSettings:
  cdn:
    edgeLocations:
      - default-edge-1
      - default-edge-2
```

**Region:US-West:**

```yaml
networkSettings:
  cdn:
    edgeLocations:
      - us-west-1
      - us-west-2
```

**Merged Result:**

```yaml
networkSettings:
  cdn:
    edgeLocations:            # Entire array from Region (replaces Default)
      - us-west-1
      - us-west-2
```

### Scalar Replace

Scalar values (strings, numbers, booleans) are replaced by higher precedence.

**Default:** `maxConcurrentRequests: 1000`

**Region:** `maxConcurrentRequests: 2000`

**Environment:** `maxConcurrentRequests: 5000`

**Result:** `maxConcurrentRequests: 5000` (Environment wins)

## Parameter Version Management

Parameters support versioning for safe updates.

### Upload New Parameter Version

**API Endpoint:** `POST /api/v1/parameters/{scopeTypeId}/{configurationId}`

All parameter uploads create a new version in "Draft" state.

**Draft State:**

- Editable
- Not used by nodes
- Can be modified or deleted

### Publish Parameter Version

**API Endpoint:** `POST /api/v1/parameters/{scopeTypeId}/
{configurationId}/versions/{versionNumber}/publish`

Activate a draft version for node use:

**Response:**

```json
{
  "versionNumber": 2,
  "state": "Active",
  "publishedAt": "2024-01-15T14:30:00Z"
}
```

**Active State:**

- Read-only
- Used by nodes
- Can be archived but not modified

### View Version History

**API Endpoint:** `GET /api/v1/parameters/{scopeTypeId}/
{configurationId}/versions`

See all parameter versions with their state and timestamps.

## Testing Different Scenarios

### Scenario 1: EU-Central Production Server

Tag a node with:

- Region: `EU-Central`
- Environment: `Production`

**Key Differences from US-West:**

- `complianceSettings.framework: GDPR` (instead of CCPA)
- `loggingSettings.retentionDays: 90` (instead of 365)
- `loggingSettings.anonymizeIpAddresses: true` (GDPR requirement)
- Different CDN edge locations (EU regions)

### Scenario 2: US-West Development Server

Tag a node with:

- Region: `US-West`
- Environment: `Development`

**Key Differences from Production:**

- Lower resource limits (dev capacity)
- `loggingSettings.level: Debug` (verbose logging)
- No monitoring/alerting (dev doesn't need it)
- Shorter log retention

## Common Operations

### Update Regional Parameters

To update US-West compliance settings:

1. Upload new parameters to Region:US-West scope
2. Creates new draft version
3. Publish the draft version
4. All US-West nodes receive updated parameters on next refresh

### Add New Region

To add Asia-Pacific region:

1. Create scope value: `Asia-Pacific`
2. Upload regional parameters
3. Tag nodes with Asia-Pacific scope
4. Nodes download configuration with APAC parameters

### Override for Specific Node

To temporarily increase capacity on one production server:

1. Upload parameters to Node scope for that FQDN
2. Node scope has highest precedence (20)
3. Overrides Environment and Region scopes
4. Only affects that specific node

## Next Steps

**Explore More:**

- [Scope System Concepts](scope-system.md) -
  Deep dive into scope types and precedence
- [Parameter Merging Algorithm](parameter-merging.md) -
  Understand merge behavior
- [Configuration Management](configuration-management.md) -
  Version lifecycle and bundles

**Real-World Examples:**

- [Multi-Team Web Server](examples/01-web-server-baseline.md) -
  Complex multi-team scenario
- [Multi-Region Deployment](examples/02-multi-region-deployment.md) -
  Geographic distribution
- [Environment Promotion](examples/03-environment-promotion.md) -
  Dev/staging/prod workflow

**Advanced Topics:**

- Configuration retention policies
- Parameter encryption and secrets management
- Automated compliance reporting
- Parameter drift detection
