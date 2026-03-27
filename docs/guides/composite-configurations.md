---
description: >-
  Combine multiple configuration documents into a single deployment unit using composite
  configurations in the OpenDsc Pull Server.
title: "How to: Use composite configurations"
date: 2026-03-27
topic: how-to
---

# Use composite configurations

Composite configurations let you combine multiple versioned configuration
documents into a single
ordered deployment unit. This is useful when different teams manage different
aspects of a system
and you want to deliver them as one bundle.

## When to use this guide

Use composite configurations when you need to:

- Combine configurations from multiple teams (security, networking, application)
  into one delivery.
- Version and update individual configuration documents independently.
- Control the order in which configurations are applied.

## Prerequisites

- A running Pull Server with at least two published configurations.
- An authenticated web session or API access. See [Get started with the Pull
  Server][01].

## Create individual configurations

Upload two configuration documents to the Pull Server. This example uses a
security baseline and
an application configuration:

### Security baseline

```yaml
# security-baseline.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Disable guest account
    type: OpenDsc.Windows/User
    properties:
      name: Guest
      enabled: false
```

### Application configuration

```yaml
# app-config.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set app home directory
    type: OpenDsc.Windows/Environment
    properties:
      name: APP_HOME
      value: C:\App
      scope: Machine
```

### Upload and publish both configurations

#### Using the web UI

1. Navigate to **Configurations** and click **Create**.
2. Upload `security-baseline.dsc.yaml` as the `SecurityBaseline` configuration.
3. Click **Publish** on version `1.0.0`.
4. Repeat for `AppConfig`.

<!-- TODO: Replace with actual screenshot -->
![Published configurations](media/composite-configurations/published-configs.png)

#### Using PowerShell

```powershell
# Upload security baseline
$form = @{
    name       = 'SecurityBaseline'
    entryPoint = 'security-baseline.dsc.yaml'
    files      = Get-Item "$env:TEMP\security-baseline.dsc.yaml"
}
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/configurations' `
    -Method Post -Form $form -WebSession $session

# Publish it
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/configurations/SecurityBaseline/versions/1.0.0/publish' `
    -Method Put -WebSession $session

# Upload app config
$form = @{
    name       = 'AppConfig'
    entryPoint = 'app-config.dsc.yaml'
    files      = Get-Item "$env:TEMP\app-config.dsc.yaml"
}
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/configurations' `
    -Method Post -Form $form -WebSession $session

# Publish it
Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/configurations/AppConfig/versions/1.0.0/publish' `
    -Method Put -WebSession $session
```

## Create a composite configuration

#### Using the web UI

1. Navigate to **Composite Configurations**.
2. Click **Create**.
3. Enter the name `WebServerBundle`.
4. Add `SecurityBaseline` at order position `1`.
5. Add `AppConfig` at order position `2`.
6. Click **Save**.

<!-- TODO: Replace with actual screenshot -->
![Create composite configuration](media/composite-configurations/create-composite.png)

#### Using PowerShell

```powershell
$body = @{
    name           = 'WebServerBundle'
    configurations = @(
        @{ configurationName = 'SecurityBaseline'; order = 1 }
        @{ configurationName = 'AppConfig'; order = 2 }
    )
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/composite-configurations' `
    -Method Post -ContentType 'application/json' `
    -Body $body -WebSession $session
```

## Assign the composite configuration to a node

#### Using the web UI

1. Navigate to **Nodes** and click on your registered node.
2. Under **Configuration**, select the `WebServerBundle` composite
   configuration.
3. Click **Save**.

<!-- TODO: Replace with actual screenshot -->
![Assign composite to node](media/composite-configurations/assign-composite.png)

#### Using PowerShell

```powershell
$lcmConfig = Get-Content "$env:ProgramData\OpenDSC\LCM\appsettings.json" | ConvertFrom-Json
$nodeId = $lcmConfig.LCM.PullServer.NodeId

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/nodes/$nodeId/configuration" `
    -Method Put -ContentType 'application/json' `
    -Body (@{ compositeConfigurationName = 'WebServerBundle' } | ConvertTo-Json) `
    -WebSession $session
```

## Update an individual configuration

When you update one configuration in a composite, nodes automatically receive
the updated bundle
on their next check-in:

1. Upload a new version of `AppConfig` with the updated document.
2. Publish the new version.
3. The composite configuration automatically uses the latest published version.

## See also

- [Configuration management concepts][02]
- [Semantic versioning][03]

<!-- Link references -->
[01]: ../get-started/pull-server-setup.md
[02]: ../concepts/pull-server/configuration-management.md
[03]: ../concepts/pull-server/versioning.md
