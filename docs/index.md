# OpenDSC

OpenDSC is an open-source configuration management platform built on Microsoft's
Desired State
Configuration (DSC) v3. It extends the DSC ecosystem with built-in resources for
managing Windows,
SQL Server, and cross-platform systems, a Local Configuration Manager (LCM)
service for continuous
monitoring and remediation, and a Pull Server for centralized configuration
delivery.

With OpenDSC, you can:

- **Manage Windows systems** using built-in resources for environment variables,
  services,
  scheduled tasks, local users and groups, optional features, shortcuts, and
  file system ACLs.
- **Manage SQL Server** with resources for logins, databases, roles,
  permissions, linked servers,
  and Agent jobs.
- **Manage cross-platform systems** with resources for files, directories,
  symbolic links, XML
  elements, JSON values, ZIP archives, and POSIX permissions.
- **Monitor configuration drift** with the LCM service, which periodically
  checks whether systems
  match their desired state.
- **Remediate drift automatically** by running the LCM in Remediate mode, so
  systems self-correct
  when they deviate from their declared configuration.
- **Centralize configuration delivery** with the Pull Server, which stores
  versioned
  configurations, merges parameters across scopes, and delivers configurations
  to managed nodes
  over HTTPS.

## Components

OpenDSC has three main components:

### DSC Resources

OpenDSC ships a set of built-in DSC Resources packaged into a single executable.
The resources
work with the standard DSC v3 CLI and follow the same patterns as any other DSC
command resource.
You can use them individually with `dsc resource get`, `dsc resource set`, and
`dsc resource delete`, or compose them into configuration documents.

For a full list of available resources, see [resource reference][01].

### Local Configuration Manager (LCM)

The LCM is a background service that continuously applies a DSC configuration
document. It
operates in two modes:

- **Monitor** — periodically runs `dsc config test` and reports whether the
  system is in the
  desired state without making changes.
- **Remediate** — periodically runs `dsc config test` and, when drift is
  detected, runs
  `dsc config set` to bring the system back into compliance.

The LCM can pull its configuration from a local file or from the Pull Server.
For more
information, see [LCM concepts][02].

### Pull Server

The Pull Server is an ASP.NET Core application that combines a REST API with a
Blazor web UI. It
provides:

- **Node registration** with mutual TLS (mTLS) certificate-based authentication.
- **Versioned configuration storage** with semantic versioning and draft/publish
  lifecycle.
- **Composite configurations** that combine multiple configuration documents
  into a single
  deployment unit.
- **Hierarchical parameter merging** across scope types (Default → Region →
  Environment → Node).
- **Compliance reporting** with drift detection and historical tracking.
- **Role-based access control (RBAC)** with users, groups, and fine-grained
  authorization
  policies.

For more information, see [Pull Server concepts][03].

## How OpenDSC relates to Microsoft DSC

OpenDSC is built on top of Microsoft's DSC v3 platform. It uses the `dsc` CLI as
its engine and
follows the same configuration document format, resource manifest conventions,
and operational
model. OpenDSC doesn't replace or fork DSC — it extends the ecosystem with
additional resources
and management infrastructure.

If you're familiar with DSC v3, you already know how to use OpenDSC resources.
They appear
alongside built-in DSC resources in `dsc resource list` and work identically
with `dsc config`
commands.

- Browse the [resource reference][01] for available resources.

[01]: reference/resources/index.md
[02]: concepts/lcm/overview.md
[03]: concepts/pull-server/overview.md
