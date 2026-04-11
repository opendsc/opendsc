# Prerelease vs Feature Flags

OpenDSC provides two complementary mechanisms for progressive rollout.
Understanding when to use each prevents misuse and leads to simpler, safer
canary strategies.

**Prerelease configuration versions** (e.g., `2.1.0-rc.1`) let you ship a new
version of the configuration YAML to a controlled subset of nodes. Nodes opt in
by setting their `PrereleaseChannel` to a matching label (e.g., `rc`). Nodes
without a matching channel continue receiving the current stable version
unchanged.

**Feature flags** (implemented as boolean parameters) let you enable or disable
specific resource blocks within a single, stable configuration version,
controlled by scope-layered parameters. All nodes run the same configuration
version; only the parameter values differ.

These mechanisms are **not interchangeable**:

| Concern                 | Prerelease Version                                        | Feature Flag (Parameter)                             |
| :---------------------- | :-------------------------------------------------------- | :--------------------------------------------------- |
| What changes            | The configuration YAML itself                             | Behavior of existing resource blocks                 |
| Node opt-in             | Explicit — node must set `PrereleaseChannel`              | Implicit — scope tags determine flag values          |
| Simultaneous versions   | Nodes run different versions                              | All nodes run the same version                       |
| Rollback                | Promote nothing; stable version stays                     | Revert the parameter file for the canary scope       |
| Best for                | Structural changes, new resource types, schema migrations | Behavioral toggles, incremental feature enablement   |

## When to Use Prerelease

Use prerelease versions when the configuration YAML itself changes and needs
validation before broad distribution:

- Adding new resource types that may not be supported on all nodes.
- Restructuring the configuration document or changing the DSC schema in a way
  that could fail on a subset of nodes.
- Making changes that must be validated end-to-end before all nodes receive
  them.

A prerelease version is a distinct artifact from the stable release. It is never
implicitly distributed — nodes only receive it if they have opted in via
`PrereleaseChannel`. Once promoted to stable, the prerelease version is
superseded by the stable release and the `PrereleaseChannel` setting on the node
no longer has any effect on which version it receives.

## When to Use Feature Flags

Use feature flags when the configuration YAML structure is stable and the change
is purely behavioral:

- Rolling a new application feature to a canary environment before production.
- A/B testing different configuration settings across node groups.
- Enabling opt-in behaviors that require phased adoption (e.g., enabling HTTPS,
  installing a WAF module).

Feature flags have no version promotion step. You enable the flag at a narrow
scope (a canary environment or a dedicated canary scope value), observe
compliance reports, and then widen the scope or update the `Default`
parameters when ready. There is no prerelease or stable concept — the flag is
simply `true` or `false` at each scope level.

See the [Feature Flags guide] for the full pattern including how to declare
flags, structure scope-layered parameter files, and implement two common canary
strategies.

## Combining Both Approaches

The two mechanisms work best together when a new configuration version
introduces resource blocks that start as feature-flagged:

1. **Configuration version change** — Add the new resource block gated by
   `enableNewFeature: false` (dormant everywhere). Publish this as a stable
   version; all nodes receive it with the feature off.
2. **Feature flag progression** — Enable `enableNewFeature: true` in canary
   scope parameters. Validate compliance reports. Promote to production
   parameters (or `Default`) when ready.

This separates two distinct concerns: verifying that the new configuration
structure works (a configuration version concern) from determining when the new
behavior should be active (a parameter/flag concern). Prerelease versions are
left for cases where the structure itself needs canary validation before any
nodes should run the new YAML at all.

[Feature Flags guide]: feature-flags.md
