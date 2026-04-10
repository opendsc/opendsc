# Roadmap

OpenDSC is a configuration management platform for [Microsoft DSC]. It provides
a local configuration manager (LCM) for policy enforcement and remediation, a
centralized pull server platform for configuration management and reporting, and
an extensible framework for building custom resources. This roadmap outlines
current capabilities and near-term initiatives organized by product area.

## Current State

### DSC Resources

OpenDSC ships with a comprehensive, resource library covering
Windows system administration, SQL Server management, and cross-platform
infrastructure tasks. Feature parity with PSDesiredStateConfiguration while
extending to modern Linux and macOS environments.

- [x] **Windows System Management** — Core resource support for system
configuration and hardening
- [x] **SQL Server Administration** — Database and SQL Server management without
external dependencies
- [x] **Cross-Platform Infrastructure** — Consistent file, JSON, XML, and
archive management across Windows, Linux, and macOS

### Resource Authoring Framework (RDK)

The Resource Development Kit enables teams to build custom DSC resources with
minimal boilerplate. Focus on resource logic while the framework handles all
CLI integration and schema management automatically.

- [x] **DSC Resource CLI Generation** — Seamlessly integrate resources with DSC
without coding command interfaces
- [x] **Multi-Resource Executables** — Bundle multiple resources into single
deployable executables with automatic manifest management
- [x] **Standalone Execution** — Run resources without requiring .NET runtime
installation for maximum portability

### Local Configuration Manager (LCM)

The LCM is a cross-platform configuration agent that applies and maintains DSC
configurations on individual nodes. It supports both local file-based
configurations for disconnected scenarios and server-driven pull models for
centralized management, with automatic drift detection and remediation.

- [x] **Automatic Policy Enforcement** — Continuously monitor configuration
drift and automatically remediate divergence
- [x] **Local File Configuration** — Deploy and manage configurations locally
without requiring a pull server
- [x] **Server-Driven Configuration** — Automatically pull configurations and
parameters from the pull server
- [x] **Compliance Reporting** — Audit configuration compliance and access
centralized reports on pull server
- [x] **Windows Service Integration** — Runs as a native Windows Service with
secure mTLS certificate management and automatic rotation

### Pull Server

The pull server provides enterprise configuration management, centralized node
management, and compliance reporting through a REST API and Blazor web
dashboard. It consolidates configuration deployment, node registration, and
reporting into a unified platform with flexible parameter customization and
role-based access control.

- [x] **Configuration Management** — Upload, version, and promote configurations
safely through deployment workflows
- [x] **Composite Configuration** — Combine multiple configurations into
single deployments with controlled ordering
- [x] **Flexible Parameter Customization** — Override parameters by scope
(Default, Region, Environment, Node) for adaptable deployments
- [x] **Node Organization & Tagging** — Register and tag nodes for efficient
organization and conditional configuration targeting
- [x] **Web Administration Dashboard** — Full-featured Blazor UI for node
management, configuration assignment, compliance reports, and RBAC
administration
- [x] **Multi-Database Support** — SQLite for development, SQL Server and
PostgreSQL for production deployments
- [x] **REST API** — Complete API with interactive documentation for integration
and automation

## In Progress / Next Up

Work planned for the next 6–12 months to expand cross-platform support, improve
deployment options, and enhance authentication:

- [ ] **LCM services for Linux and macOS** — Native service integration for
Linux (systemd) and macOS (launchd) to match Windows Service capabilities
- [ ] **Linux and macOS distribution packages** — First-class installation via
native package managers (apt, yum, brew) for Resources, LCM, and Server
- [ ] **Native AOT for all resources** — Standalone execution support for all
built-in resources without runtime dependencies
- [ ] **Server Docker deployment** — Official Docker images and Compose
templates for rapid Server deployment and container orchestration
- [ ] **OpenID/OAuth integration** — Enterprise authentication for Server via
OAuth 2.0 and OpenID Connect providers
- [ ] **Expanded resource library** — Additional resources for Windows, macOS,
and Linux to provide comprehensive baseline coverage

## Future Directions

Longer-term capabilities under exploration:

- [ ] **Agentless configuration management** — Centralized push-initiated
deployments for scenarios that don't require resident agents on nodes
- [ ] **Configuration authoring UI** — Visual editor for composing and managing
DSC configurations without manual YAML or JSON authoring

[Microsoft DSC]: https://github.com/PowerShell/DSC
