---
description: >-
  Understand how the OpenDsc Pull Server validates parameters against configuration schemas and
  checks for breaking changes between versions.
title: Parameter validation
date: 2026-03-27
topic: concept
---

# Parameter validation

The Pull Server validates parameter files against JSON schemas defined by
configurations. This
prevents invalid or incompatible parameters from being deployed to managed
nodes.

## Schema-based validation

Configurations can define a parameter schema that describes the expected
structure and types of
parameter values. When parameter files are uploaded, the Pull Server validates
them against this
schema.

If validation fails, the server rejects the upload and reports the specific
validation errors.

## Version compatibility

When you update a configuration's parameter schema, the
`ParameterCompatibilityService` checks
all existing parameter files for compatibility with the new schema. This happens
before a new
version is activated.

The compatibility check identifies:

- **Missing required properties** — parameters that the new schema requires but
  existing files
  don't provide.
- **Type mismatches** — parameters with values that don't match the new schema's
  type constraints.
- **Removed properties** — parameters that existing files provide but the new
  schema no longer
  accepts.

In the web UI, the **Parameter Migration Dialog** surfaces these issues and
helps you resolve them
before publishing.

## See also

- [Parameter merging][01]
- [Configuration management][02]

<!-- Link references -->
[01]: parameter-merging.md
[02]: configuration-management.md
