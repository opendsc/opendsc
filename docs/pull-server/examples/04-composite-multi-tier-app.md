# Example: Multi-Tier Application Deployment

This example demonstrates using composite configurations to deploy a
complete multi-tier application stack with separate configurations for
each layer.

## Scenario

Deploy a three-tier web application consisting of:

1. **Database Layer** - SQL Server database with security hardening
2. **Application Layer** - Web API server with IIS configuration
3. **Frontend Layer** - Static web content and reverse proxy

## Prerequisites

- OpenDSC Pull Server running
- Admin API key configured
- Three node machines registered (or one node for testing)

## Step 1: Create Individual Configurations

### Database Configuration

Create the database layer configuration:

#### File: database-main.dsc.yaml

```yaml
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
parameters:
  dbPort:
    type: int
    defaultValue: 1433
  dbInstanceName:
    type: string
    defaultValue: MSSQLSERVER

resources:
  - name: SQL Server Service
    type: OpenDsc.Windows/Service
    properties:
      name: "[parameters('dbInstanceName')]"
      status: Running
      startupType: Automatic

  - name: SQL Server Firewall Rule
    type: OpenDsc.Windows.Firewall/Rule
    properties:
      name: SQL-Server-In
      localPort: "[parameters('dbPort')]"
      protocol: TCP
      action: Allow
      direction: Inbound
```

**Upload to server:**

```sh
curl -X POST http://localhost:5000/api/v1/configurations \
  -H "X-API-Key: admin-key" \
  -F "name=DatabaseServer" \
  -F "description=SQL Server database configuration" \
  -F "version=1.0.0" \
  -F "isDraft=false" \
  -F "files=@database-main.dsc.yaml"
```

### Application Configuration

Create the web API layer configuration:

#### File: api-main.dsc.yaml

```yaml
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
parameters:
  apiPort:
    type: int
    defaultValue: 8080
  connectionString:
    type: string

resources:
  - name: API Application Directory
    type: OpenDsc.FileSystem/Directory
    properties:
      path: C:\WebAPI
      ensure: Present

  - name: API Configuration File
    type: OpenDsc.FileSystem/File
    properties:
      path: C:\WebAPI\appsettings.json
      content: |
        {
          "ConnectionStrings": {
            "Default": "[parameters('connectionString')]"
          },
          "Kestrel": {
            "Endpoints": {
              "Http": {
                "Url": "http://*:[parameters('apiPort')]"
              }
            }
          }
        }

  - name: API Windows Service
    type: OpenDsc.Windows/Service
    properties:
      name: WebAPIService
      status: Running
      startupType: Automatic
```

**Upload to server:**

```sh
curl -X POST http://localhost:5000/api/v1/configurations \
  -H "X-API-Key: admin-key" \
  -F "name=ApplicationServer" \
  -F "description=Web API application server" \
  -F "version=1.0.0" \
  -F "isDraft=false" \
  -F "files=@api-main.dsc.yaml"
```

### Frontend Configuration

Create the frontend layer configuration:

#### File: frontend-main.dsc.yaml

```yaml
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
parameters:
  webPort:
    type: int
    defaultValue: 80
  apiBackend:
    type: string
    defaultValue: http://localhost:8080

resources:
  - name: Install IIS
    type: OpenDsc.Windows/OptionalFeature
    properties:
      name: IIS-WebServer
      ensure: Present

  - name: Web Content Directory
    type: OpenDsc.FileSystem/Directory
    properties:
      path: C:\WebContent
      ensure: Present

  - name: IIS Site Configuration
    type: OpenDsc.Windows.IIS/Site
    properties:
      name: FrontendSite
      physicalPath: C:\WebContent
      bindings:
        - protocol: http
          port: "[parameters('webPort')]"
          ipAddress: "*"
```

**Upload to server:**

```sh
curl -X POST http://localhost:5000/api/v1/configurations \
  -H "X-API-Key: admin-key" \
  -F "name=FrontendServer" \
  -F "description=Frontend web server with IIS" \
  -F "version=1.0.0" \
  -F "isDraft=false" \
  -F "files=@frontend-main.dsc.yaml"
```

## Step 2: Create Parameters for Each Layer

### Default Parameters

Set baseline parameters that apply to all environments:

```sh
curl -X POST http://localhost:5000/api/v1/parameters/default \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "configurationName": "DatabaseServer",
    "parameters": {
      "dbPort": 1433,
      "dbInstanceName": "MSSQLSERVER"
    }
  }'

curl -X POST http://localhost:5000/api/v1/parameters/default \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "configurationName": "ApplicationServer",
    "parameters": {
      "apiPort": 8080
    }
  }'

curl -X POST http://localhost:5000/api/v1/parameters/default \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "configurationName": "FrontendServer",
    "parameters": {
      "webPort": 80,
      "apiBackend": "http://localhost:8080"
    }
  }'
```

### Environment-Specific Parameters

Create different parameters for development and production:

```sh
# Development environment
curl -X POST http://localhost:5000/api/v1/parameters/scope \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "scopeValueName": "Development",
    "configurationName": "DatabaseServer",
    "parameters": {
      "dbPort": 11433
    }
  }'

# Production environment
curl -X POST http://localhost:5000/api/v1/parameters/scope \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "scopeValueName": "Production",
    "configurationName": "DatabaseServer",
    "parameters": {
      "dbPort": 1433
    }
  }'
```

## Step 3: Create Composite Configuration

### Create the Composite

```sh
curl -X POST http://localhost:5000/api/v1/composite-configurations \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "FullApplicationStack",
    "description": "Complete three-tier application deployment"
  }'
```

### Create a Draft Version

```sh
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.0.0",
    "isDraft": true
  }'
```

### Add Child Configurations in Order

Add the configurations in dependency order (database first, then
application, then frontend):

```sh
# 1. Database Layer
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.0.0/children \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "childConfigurationName": "DatabaseServer",
    "activeVersion": null,
    "order": 1
  }'

# 2. Application Layer
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.0.0/children \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "childConfigurationName": "ApplicationServer",
    "activeVersion": null,
    "order": 2
  }'

# 3. Frontend Layer
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.0.0/children \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "childConfigurationName": "FrontendServer",
    "activeVersion": null,
    "order": 3
  }'
```

### Publish the Version

```sh
curl -X PUT http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.0.0/publish \
  -H "X-API-Key: admin-key"
```

## Step 4: Assign to Nodes

### Assign to Production Node

```sh
curl -X PUT http://localhost:5000/api/v1/nodes/{nodeId}/configuration \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "compositeConfigurationName": "FullApplicationStack",
    "isComposite": true,
    "activeVersion": null
  }'
```

## Step 5: Node Downloads and Applies Configuration

When the LCM on the node runs, it will:

1. **Check for updates** - Compare checksums
2. **Download bundle** - Get ZIP with all three configurations
3. **Extract bundle** - Unpack to local directory
4. **Apply configuration** - Run `dsc config set` on the orchestrator

### Generated Bundle Structure

```text
root/
├── main.dsc.yaml                    # Orchestrator
├── DatabaseServer/
│   └── main.dsc.yaml                # DB config with merged parameters
├── ApplicationServer/
│   └── main.dsc.yaml                # API config with merged parameters
└── FrontendServer/
    └── main.dsc.yaml                # Frontend config with merged parameters
```

### Generated Orchestrator (main.dsc.yaml)

```yaml
# Auto-generated orchestrator for composite: FullApplicationStack
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: DatabaseServer
    type: Microsoft.DSC/Include
    properties:
      configurationFile: DatabaseServer/main.dsc.yaml
      parametersFile: DatabaseServer/parameters.yaml

  - name: ApplicationServer
    type: Microsoft.DSC/Include
    properties:
      configurationFile: ApplicationServer/main.dsc.yaml
      parametersFile: ApplicationServer/parameters.yaml

  - name: FrontendServer
    type: Microsoft.DSC/Include
    properties:
      configurationFile: FrontendServer/main.dsc.yaml
      parametersFile: FrontendServer/parameters.yaml
```

## Step 6: Update a Child Configuration

When you need to update just one layer (e.g., the application server):

```sh
# Create new version of ApplicationServer
curl -X POST http://localhost:5000/api/v1/configurations/ApplicationServer/versions \
  -H "X-API-Key: admin-key" \
  -F "version=1.1.0" \
  -F "isDraft=false" \
  -F "files=@api-main-updated.dsc.yaml"
```

**Result:** Because the composite uses `activeVersion: null`, nodes
will automatically receive the updated ApplicationServer v1.1.0 on their
next check without needing to update the composite configuration.

## Step 7: Pin a Stable Version

To prevent automatic updates for production:

```sh
# Get the version ID of DatabaseServer 1.0.0
curl -X GET http://localhost:5000/api/v1/configurations/DatabaseServer/versions \
  -H "X-API-Key: admin-key"

# Create new composite version with pinned database
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.1.0",
    "isDraft": true
  }'

# Add DatabaseServer with pinned version
curl -X POST http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.1.0/children \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "childConfigurationName": "DatabaseServer",
    "activeVersion": "1.0.0",
    "order": 1
  }'

# Add other children (tracking latest)
# ... (repeat for ApplicationServer and FrontendServer)

# Publish
curl -X PUT http://localhost:5000/api/v1/composite-configurations/FullApplicationStack/versions/1.1.0/publish \
  -H "X-API-Key: admin-key"
```

## Benefits Demonstrated

1. **Modularity** - Each tier is managed independently
2. **Reusability** - DatabaseServer config can be used in other composites
3. **Version Control** - Pin critical components, track others
4. **Parameter Scoping** - Each child gets correct environment parameters
5. **Ordered Deployment** - Database → Application → Frontend sequence
6. **Single Assignment** - One operation assigns all three tiers

## Next Steps

- Add monitoring configuration as a fourth child
- Create region-specific parameter scopes
- Set up different composite versions for staging and production
- Use node tags for automatic environment parameter merging
