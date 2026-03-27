---
description: >-
  Configure hierarchical parameter merging in the OpenDsc Pull Server to manage environment-specific
  settings across scope types like Region, Environment, and Node.
title: "How to: Set up parameter merging"
date: 2026-03-27
topic: how-to
---

# Set up parameter merging

The Pull Server's parameter merging system lets you define configuration values
at different scope
levels and automatically merges them into a single parameter set for each node.
Narrower scopes
override broader ones.

## When to use this guide

Use parameter merging when you need to:

- Define global baseline settings that apply to all nodes.
- Override specific values per region, environment, or team.
- Customize individual nodes without duplicating common settings.
- Track where each parameter value originated (provenance).

## Merge order

Parameters are merged from broadest scope to narrowest:

```
Default → Custom Scope Types (by precedence) → Node
```

For example, with scope types Region (precedence 10) and Environment (precedence
20):

```
Default → Region → Environment → Node
```

Higher precedence values are applied later and override lower ones.

## Prerequisites

- A running Pull Server with at least one published configuration.
- An authenticated web session or API access.

## Step 1: Create scope types

Define the hierarchy of scope types for your organization.

### Using the web UI

1. Navigate to **Settings → Scope Types**.
2. Click **Create**.
3. Enter the name `Region` with precedence `10`.
4. Click **Save**.
5. Repeat for `Environment` with precedence `20`.

<!-- TODO: Replace with actual screenshot -->
![Create scope types](media/parameter-merging/create-scope-types.png)

### Using PowerShell

```powershell
# Create Region scope type
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-types' `
    -Method Post -ContentType 'application/json' `
    -Body '{"name":"Region","precedence":10}' `
    -WebSession $session

# Create Environment scope type
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-types' `
    -Method Post -ContentType 'application/json' `
    -Body '{"name":"Environment","precedence":20}' `
    -WebSession $session
```

## Step 2: Create scope values

Create specific values for each scope type.

### Using the web UI

1. Navigate to **Settings → Scope Values**.
2. Create values for each scope type:
   - Region: `US-West`, `EU-Central`
   - Environment: `Development`, `Production`

<!-- TODO: Replace with actual screenshot -->
![Create scope values](media/parameter-merging/create-scope-values.png)

### Using PowerShell

```powershell
# Create scope values for Region
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-values' `
    -Method Post -ContentType 'application/json' `
    -Body '{"scopeTypeName":"Region","value":"US-West"}' `
    -WebSession $session

Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-values' `
    -Method Post -ContentType 'application/json' `
    -Body '{"scopeTypeName":"Region","value":"EU-Central"}' `
    -WebSession $session

# Create scope values for Environment
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-values' `
    -Method Post -ContentType 'application/json' `
    -Body '{"scopeTypeName":"Environment","value":"Development"}' `
    -WebSession $session

Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/scope-values' `
    -Method Post -ContentType 'application/json' `
    -Body '{"scopeTypeName":"Environment","value":"Production"}' `
    -WebSession $session
```

## Step 3: Tag nodes with scope values

Associate nodes with specific scope values so the server knows which parameters
to merge.

### Using the web UI

1. Navigate to **Nodes** and click on your node.
2. Under **Tags**, add `Region: US-West` and `Environment: Development`.
3. Click **Save**.

<!-- TODO: Replace with actual screenshot -->
![Tag node with scope values](media/parameter-merging/tag-node.png)

### Using PowerShell

```powershell
$nodeId = '<your-node-id>'

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/node-tags" `
    -Method Post -ContentType 'application/json' `
    -Body (@{ nodeId = $nodeId; scopeTypeName = 'Region'; scopeValue = 'US-West' } | ConvertTo-Json) `
    -WebSession $session

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/node-tags" `
    -Method Post -ContentType 'application/json' `
    -Body (@{ nodeId = $nodeId; scopeTypeName = 'Environment'; scopeValue = 'Development' } | ConvertTo-Json) `
    -WebSession $session
```

## Step 4: Upload parameters at different scopes

Upload parameter files for the `LabConfig` configuration at each scope level.

### Default parameters

```yaml
# parameters-default.yaml
appSettings:
  logLevel: Warning
  maxConnections: 100
  featureFlags:
    betaFeatures: false
    darkMode: false
```

### Region parameters

```yaml
# parameters-us-west.yaml
appSettings:
  timezone: America/Los_Angeles
  cdn: us-west-cdn.example.com
```

### Environment parameters

```yaml
# parameters-development.yaml
appSettings:
  logLevel: Debug
  maxConnections: 10
  featureFlags:
    betaFeatures: true
```

### Upload with PowerShell

```powershell
# Upload default parameters
$form = @{
    files = Get-Item "$env:TEMP\parameters-default.yaml"
}
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/parameters/LabConfig/Default' `
    -Method Post -Form $form -WebSession $session

# Upload region parameters for US-West
$form = @{
    files = Get-Item "$env:TEMP\parameters-us-west.yaml"
}
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/parameters/LabConfig/Region/US-West' `
    -Method Post -Form $form -WebSession $session

# Upload environment parameters for Development
$form = @{
    files = Get-Item "$env:TEMP\parameters-development.yaml"
}
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/parameters/LabConfig/Environment/Development' `
    -Method Post -Form $form -WebSession $session
```

## Step 5: View merged parameters

After uploading parameters at multiple scopes, view the merged result for a
node.

### Using the web UI

1. Navigate to **Parameters** and select the `LabConfig` configuration.
2. Click **View Merged** for your node.
3. The **Provenance Visualization** panel shows where each value originated.

<!-- TODO: Replace with actual screenshot -->
![Merged parameters with provenance](media/parameter-merging/merged-parameters.png)

### Merged result

For a node tagged with `Region: US-West` and `Environment: Development`, the
merged parameters
are:

```yaml
appSettings:
  logLevel: Debug                    # Overridden by Environment
  maxConnections: 10                 # Overridden by Environment
  timezone: America/Los_Angeles      # From Region
  cdn: us-west-cdn.example.com      # From Region
  featureFlags:
    betaFeatures: true               # Overridden by Environment
    darkMode: false                  # From Default
```

## See also

- [Parameter merging concepts][01]
- [Scope system concepts][02]
- [Parameter validation][03]

<!-- Link references -->
[01]: ../concepts/pull-server/parameter-merging.md
[02]: ../concepts/pull-server/scope-system.md
[03]: ../concepts/pull-server/parameter-validation.md
