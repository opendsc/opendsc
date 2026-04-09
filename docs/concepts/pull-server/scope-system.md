# Scope system

The scope system is the foundation of the Pull Server's hierarchical parameter
merging. It defines
how parameter values are organized, prioritized, and associated with managed
nodes.

## Scope types

A **scope type** is a category in your organizational hierarchy. Each scope type
represents a
dimension along which configuration can vary — geography, deployment stage,
team, or any other
grouping that makes sense for your organization.

Every scope type has:

- A **name** that identifies the category (e.g., `Region`, `Environment`).
- A **precedence** value that determines its position in the merge order. Higher
  values are
  applied later and take priority.

### Built-in scope types

| Scope type | Precedence | Description                         |
| :--------- | :--------- | :---------------------------------- |
| Default    | 0          | Global baseline, always the first   |
| Node       | ∞          | Per-node overrides, always the last |

### Custom scope types

Create custom scope types to match your organizational structure. Common
examples:

| Scope type  | Precedence | Example values                   |
| :---------- | :--------- | :------------------------------- |
| Region      | 10         | US-West, US-East, EU-Central     |
| Environment | 20         | Development, Staging, Production |
| Team        | 30         | Platform, Security, Application  |

## Scope values

A **scope value** is a specific instance of a scope type. For the scope type
"Region", scope
values might be "US-West" and "EU-Central".

Scope values are the identifiers that parameter files are organized under. The
Pull Server stores
parameter files in a directory structure:

```
parameters/{ConfigurationName}/{ScopeType}/{ScopeValue}/parameters.yaml
```

## Node tags

**Node tags** associate a managed node with specific scope values. Each node can
have at most one
tag per scope type.

For example, a web server in the US West production environment might have:

| Scope type  | Scope value |
| :---------- | :---------- |
| Region      | US-West     |
| Environment | Production  |

When the Pull Server merges parameters for this node, it looks up the parameter
files for each
tagged scope value and applies them in precedence order.

## Designing your scope hierarchy

When designing your scope hierarchy, consider:

- **Breadth vs. depth** — fewer scope types with more values is simpler to
  manage than many nested
  scope types.
- **Precedence order** — arrange scope types so that more specific overrides
  have higher
  precedence.
- **Team ownership** — align scope types with the teams that manage each layer
  of configuration.

## See also

- [Parameter merging][01]
- [How to: Set up parameter merging][02]

<!-- Link references -->
[01]: parameter-merging.md
[02]: ../../guides/parameter-merging.md
