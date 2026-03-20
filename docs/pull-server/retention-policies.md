# Retention Policies

The Pull Server automatically manages the accumulation of historical versions
and records by applying retention policies. Retention policies determine which
versions are kept and which can be purged.

## Overview

When configurations, parameters, and composite configurations are updated
frequently, the server accumulates many historical versions. Retention policies
keep storage usage bounded by automatically removing versions that are no longer
needed while protecting versions that are actively in use.

Compliance reports and LCM status events also accumulate continuously — reports
on every DSC cycle and status events on every LCM heartbeat. These are covered
by separate record-based retention settings.

## Retention Strategies

Four strategies work together (AND semantics) to determine which versions to
keep:

| Strategy | Default | Description |
|---|---|---|
| **Count-based** | 10 versions | Keep the N most-recent non-draft versions |
| **Age-based** | 90 days | Keep all versions newer than N days |
| **Release pinning** | Enabled | Always keep non-prerelease (release) versions |
| **Archive pinning** | Enabled | Always keep archived versions |

A version is deleted only when it fails **all** applicable strategies — i.e., it
is outside the count window AND older than the age limit AND (if release pinning
is on) it is a prerelease version AND (if archive pinning is on) it is not
archived.

Draft versions are always protected and are never eligible for deletion.

Versions actively assigned to nodes are always protected regardless of the
policy.

## Global Settings

Configure global defaults from the **Settings → Retention Policies** tab in the
web UI, or via the API:

```http
GET /api/v1/settings/retention
PUT /api/v1/settings/retention
```

### Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `enabled` | bool | `false` | Enable the scheduled background cleanup job |
| `keepVersions` | int | `10` | Number of most-recent non-draft versions to keep |
| `keepDays` | int | `90` | Keep all versions newer than this many days |
| `keepReleaseVersions` | bool | `true` | Always keep release (non-prerelease) versions |
| `keepArchivedVersions` | bool | `true` | Always keep archived versions |
| `scheduleIntervalHours` | int | `24` | How often (in hours) the background job runs |
| `reportKeepCount` | int | `1000` | Maximum compliance reports to keep per node |
| `reportKeepDays` | int | `30` | Delete reports older than this many days |
| `statusEventKeepCount` | int | `200` | Maximum LCM status events to keep per node |
| `statusEventKeepDays` | int | `7` | Delete status events older than this many days |

### Example

```http
PUT /api/v1/settings/retention
Content-Type: application/json

{
  "enabled": true,
  "keepVersions": 10,
  "keepDays": 90,
  "keepReleaseVersions": true,
  "keepArchivedVersions": true,
  "scheduleIntervalHours": 24,
  "reportKeepCount": 1000,
  "reportKeepDays": 30,
  "statusEventKeepCount": 200,
  "statusEventKeepDays": 7
}
```

## Per-Configuration Overrides

Individual configurations can override the global policy. Overrides are applied
only when set; unset fields fall back to the global defaults.

```http
GET    /api/v1/configurations/{name}/settings/retention
PUT    /api/v1/configurations/{name}/settings/retention
DELETE /api/v1/configurations/{name}/settings/retention
```

### Example

```http
PUT /api/v1/configurations/my-app/settings/retention
Content-Type: application/json

{
  "keepVersions": 5,
  "keepDays": 30
}
```

A `DELETE` to the endpoint removes the overrides and reverts the configuration
to global defaults.

> **Note:** Per-configuration overrides apply to both that configuration's
> versions and its parameter files.

## Manual Cleanup

Trigger an immediate cleanup via the API or the **Manual Cleanup** section in
the web UI.

### Version Cleanup

```http
POST /api/v1/retention/configurations/cleanup
POST /api/v1/retention/parameters/cleanup
POST /api/v1/retention/composite-configurations/cleanup
```

**Request body:**

```json
{
  "keepVersions": 10,
  "keepDays": 90,
  "keepReleaseVersions": true,
  "keepArchivedVersions": true,
  "dryRun": true
}
```

### Record Cleanup (Reports & Status Events)

```http
POST /api/v1/retention/reports/cleanup
POST /api/v1/retention/status-events/cleanup
```

Record cleanup operates per-node: each node's records are trimmed independently
so that a busy node cannot crowd out records from quieter nodes.

**Request body:**

```json
{
  "keepCount": 100,
  "keepDays": 30,
  "dryRun": true
}
```

Set `dryRun: true` on any endpoint to preview what would be deleted without
actually removing anything.

### Response

```json
{
  "deletedCount": 3,
  "keptCount": 10,
  "isDryRun": true,
  "deletedVersions": [
    {
      "id": "...",
      "version": "1.0.0-pre.1",
      "name": "my-app",
      "createdAt": "2024-01-15T10:00:00Z",
      "reason": "Outside count window (3 > 2) and older than 90 days"
    }
  ]
}
```

## Scheduled Background Cleanup

When `enabled` is set to `true`, a background job runs every
`scheduleIntervalHours` hours and applies the global policy (with
per-configuration overrides) to all version types:

1. Configuration versions
2. Parameter file versions
3. Composite configuration versions
4. Compliance reports (global settings, per-node bounds)
5. LCM status events (global settings, per-node bounds)

The background job records each run in the run history.

## Run History

View past cleanup runs via the API or the **Run History** table in the web UI:

```http
GET /api/v1/retention/runs?limit=50
```

Each record includes:

| Field | Description |
|---|---|
| `id` | Unique run identifier |
| `startedAt` | When the run started |
| `completedAt` | When the run finished (null if still running or failed) |
| `versionType` | `Configuration`, `Parameter`, `CompositeConfiguration`, `Report`, or `NodeStatusEvent` |
| `isScheduled` | `true` for background runs, `false` for manual |
| `isDryRun` | `true` if no versions were actually deleted |
| `deletedCount` | Number of versions deleted (or would-be deleted in dry run) |
| `keptCount` | Number of versions retained |
| `error` | Error message if the run failed |

## Web UI

The **Settings → Retention Policies** tab provides:

- **Global Retention Policy** — Configure version retention defaults with a save
  button
- **Reports & LCM Status Events** — Configure per-node count and age limits for
  reports and status events
- **Manual Cleanup** — Dry Run and Apply Now buttons with an inline results
  summary covering all types including reports and status events
- **Run History** — Paginated table of the last 50 runs with status, trigger
  type, and duration

Per-configuration overrides can be configured on each configuration's
**Settings** tab (navigate to **Configurations → \[name\] → Settings**) or via
the API.

## Security

All retention endpoints require the `retention.manage` permission:

```http
GET/PUT  /api/v1/settings/retention          → server-settings.write
POST     /api/v1/retention/*/cleanup          → retention.manage
GET      /api/v1/retention/runs               → retention.manage
GET/PUT/DELETE /api/v1/configurations/{name}/settings/retention → retention.manage
```
