# Versioning

The Pull Server uses semantic versioning for configurations and parameters.
Versioning provides
a clear history of changes and enables controlled rollouts.

## Semantic versioning

Configurations follow [Semantic Versioning 2.0.0][01] (`MAJOR.MINOR.PATCH`):

- **MAJOR** — breaking changes that require attention from node operators.
- **MINOR** — backward-compatible additions (new resources, new parameters).
- **PATCH** — backward-compatible fixes (corrected values, documentation
  updates).

## Version lifecycle

Each version goes through a lifecycle:

1. **Draft** — the version is uploaded but not available to nodes. It can be
   edited, validated, or
   discarded.
2. **Published** — the version is immutable and available for assignment to
   nodes. A published
   version can't be modified — create a new version instead.

## Retention policies

Over time, the Pull Server accumulates many versions. Retention policies control
how many versions
are kept:

- **Global retention** — applies to all configurations and parameters unless
  overridden.
- **Per-configuration retention** — overrides the global policy for a specific
  configuration.

Retention can be configured to:

- Keep a fixed number of the most recent versions.
- Retain only published versions (discard unpublished drafts).

For configuration, see [Configure retention policies][02].

[01]: https://semver.org/
[02]: ../../guides/retention-policies.md
