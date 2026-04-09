---
description: >-
  A glossary of terms for OpenDSC, including DSC Resources, the Local Configuration Manager,
  and the Pull Server.
title: "Glossary: OpenDSC"
date: 2026-03-27
topic: glossary
---

# Glossary: OpenDSC

OpenDSC uses several terms that might have different definitions elsewhere. This
document lists the
terms, their meanings, and shows how they're formatted in the documentation.

<!-- markdownlint-disable MD028 MD036 MD024 -->

## Platform terms

### OpenDSC

The open-source configuration management platform that provides DSC Resources, a
Local
Configuration Manager, and a Pull Server built on Microsoft DSC v3.

#### Guidelines

- Always capitalize as **OpenDsc** (capital O, capital D).
- Don't abbreviate to "ODSC" or "OD".

#### Examples

> OpenDSC extends the DSC ecosystem with built-in resources and management infrastructure.

### DSC v3

Microsoft's Desired State Configuration platform, version 3. OpenDSC builds on
DSC v3 and uses the
`dsc` CLI as its engine.

#### Guidelines

- **First mention:** Microsoft DSC v3
- **Subsequent mentions:** DSC v3 or DSC

## Resource terms

### OpenDsc Resource

A DSC Resource provided by OpenDSC. Resources are organized by platform area:
Windows, SQL Server,
FileSystem, JSON, XML, Archive, and POSIX.

#### Guidelines

- Format specific resource names as code: `OpenDsc.Windows/Environment`.
- Group references use the area prefix: OpenDSC Windows resources, OpenDSC SQL
  Server resources.

#### Examples

> The `OpenDsc.Windows/Environment` resource manages Windows environment variables.

> OpenDSC Windows resources require a Windows operating system.

### Resource type name

The fully qualified identifier for a resource, using the syntax
`OpenDsc.<Area>/<Name>`. The type name uniquely identifies a resource in
configuration documents
and CLI commands.

#### Guidelines

- Always format as code when referencing a specific resource.
- Use the full type name on first reference.

#### Examples

> Use the `OpenDsc.SqlServer/Login` resource to manage SQL Server logins.

## LCM terms

### Local Configuration Manager (LCM)

The background service that monitors and optionally remediates DSC
configurations on a managed
node. The LCM periodically evaluates whether a system matches its desired state.

#### Guidelines

- **First mention:** Local Configuration Manager (LCM)
- **Subsequent mentions:** LCM

#### Examples

> The Local Configuration Manager (LCM) runs as a background service. Configure the LCM to
> operate in Monitor or Remediate mode.

### Monitor mode

The LCM operational mode that detects configuration drift without making
changes. The LCM runs
`dsc config test` and reports compliance status.

### Remediate mode

The LCM operational mode that detects and corrects configuration drift. The LCM
runs
`dsc config test` and, when drift is detected, runs `dsc config set`.

### Configuration source

Where the LCM retrieves its configuration document. The LCM supports two
sources:

- **Local** — reads the configuration from a file path on the local system.
- **Pull** — downloads the configuration from a Pull Server.

## Pull Server terms

### Pull Server

The ASP.NET Core application that provides centralized configuration management
through a REST API
and Blazor web UI.

#### Guidelines

- **First mention:** OpenDSC Pull Server
- **Subsequent mentions:** Pull Server (capitalized)

### Node

A managed system registered with the Pull Server. Each node is identified by its
fully qualified
domain name (FQDN) and authenticates using mutual TLS (mTLS).

#### Guidelines

- Use lowercase "node" when referring to the concept generically.
- Use "managed node" when distinguishing from the Pull Server.

### Registration key

A shared secret used for initial node registration with the Pull Server. After
registration, nodes
authenticate using client certificates.

### Composite configuration

A deployment unit that combines multiple versioned configurations into a single
ordered set. Nodes
receive their assigned composite configuration as a bundle.

### Scope type

A category for organizing parameter overrides in the Pull Server's hierarchical
parameter merging
system. Built-in scope types include Default and Node. Custom scope types (such
as Region or
Environment) can be created by administrators.

### Scope value

A specific instance of a scope type. For example, if the scope type is "Region",
scope values
might be "US-West" and "EU-Central".

### Parameter merging

The process by which the Pull Server combines parameter files across scope types
into a single
resolved parameter set for each node. Parameters flow from broad scope to
narrow, with narrower
scopes overriding broader ones.

### Compliance report

A report submitted by the LCM to the Pull Server after evaluating a
configuration. The report
contains the test results for each resource instance and the overall compliance
status.

### mTLS (mutual TLS)

The authentication mechanism used between managed nodes and the Pull Server.
Both the server and
client present certificates during the TLS handshake.

## Configuration document terms

### Configuration document

A YAML or JSON file that declares the desired state of a system using DSC
resource instances.
OpenDSC uses the same configuration document format as DSC v3.

#### Guidelines

- **First mention:** DSC configuration document
- **Subsequent mentions:** configuration document or configuration

### Entry point

The primary file in a configuration bundle. When a configuration contains
multiple files, the
entry point is the file that DSC processes first.
