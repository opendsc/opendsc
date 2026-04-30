---
name: add-resource-reference-doc
description: "WORKFLOW SKILL — Add a new resource reference documentation page. USE FOR: creating a new OpenDsc.Windows, OpenDsc.SqlServer, or cross-platform resource reference doc (.md file). Guides through: doc file structure (Synopsis, Type, Capabilities, Properties, Examples, Exit codes), using content tabs (PowerShell/Shell), and updating mkdocs.yml and docs/reference/resources/index.md. INVOKES: file search, read, create, and edit tools. DO NOT USE FOR: modifying existing resource docs (just edit directly); creating the resource itself (use /create-dsc-resource)."
---

# Add Resource Reference Doc

## Overview

This skill creates a new resource reference documentation page in `docs/reference/resources/{category}/`. Each resource doc follows a consistent template with sections for synopsis, type name, capabilities, properties (with types and access levels), examples (with PowerShell/Shell tabs), and exit codes.

Reference implementations:
- [`docs/reference/resources/windows/environment.md`](../../../docs/reference/resources/windows/environment.md) — Windows resource with example
- [`docs/reference/resources/archive/zip/compress.md`](../../../docs/reference/resources/archive/zip/compress.md) — cross-platform with nested category
- [`docs/reference/resources/sqlserver/login.md`](../../../docs/reference/resources/sqlserver/login.md) — SQL Server resource

## Required Inputs

Before starting, determine:

1. **Resource ID** — `OpenDsc.{Area}/{Name}` (e.g., `OpenDsc.Windows/Environment`, `OpenDsc.Archive.Zip/Compress`)
2. **Category**: `windows`, `sqlserver`, `filesystem`, `json`, `xml`, `archive/zip`, `posix/filesystem`, etc.
3. **Subcategory** (if nested, e.g., `archive/zip` → `Archive > ZIP`)
4. **Synopsis** — one-sentence description of what the resource does
5. **Capabilities** — list of supported operations: `Get`, `Set`, `Delete`, `Export`, `Test`
6. **Properties** — complete list with:
   - Property name (camelCase)
   - Description
   - Type (string, bool, enum, etc.)
   - Required (Yes/No)
   - Access (Read/Write, Read-Only, Write-Only)
   - Default value
7. **Examples** — at least 1-3 examples with PowerShell and Shell (sh) tabs
8. **Exit codes** — error conditions and their numeric codes
9. **Notes/Warnings** — any privileges or special requirements

## File Organization

### Category Directory Structure

Resources are organized by area:

```
docs/reference/resources/
├── archive/
│   └── zip/              # nested: Archive > ZIP
│       ├── compress.md
│       └── expand.md
├── filesystem/           # cross-platform: File System
│       ├── directory.md
│       ├── file.md
│       └── symbolic-link.md
├── json/                 # cross-platform: JSON
│       └── value.md
├── posix/
│   └── filesystem/       # nested: POSIX > File System
│       └── permission.md
├── sqlserver/            # SQL Server
│       ├── agent-job.md
│       ├── database.md
│       └── ...
└── windows/              # Windows
        ├── environment.md
        ├── service.md
        ├── filesystem/   # nested: Windows > File System
        │   └── acl.md
        └── ...
```

## Step-by-Step Process

### Step 1 — Read reference documents

Read 2-3 existing resource docs depending on your resource type:

**For Windows resources:**
- [`docs/reference/resources/windows/environment.md`](../../../docs/reference/resources/windows/environment.md) — simple resource
- [`docs/reference/resources/windows/user.md`](../../../docs/reference/resources/windows/user.md) — complex resource with many properties

**For SQL Server resources:**
- [`docs/reference/resources/sqlserver/login.md`](../../../docs/reference/resources/sqlserver/login.md)
- [`docs/reference/resources/sqlserver/database.md`](../../../docs/reference/resources/sqlserver/database.md)

**For cross-platform resources:**
- [`docs/reference/resources/filesystem/file.md`](../../../docs/reference/resources/filesystem/file.md) — basic cross-platform
- [`docs/reference/resources/archive/zip/compress.md`](../../../docs/reference/resources/archive/zip/compress.md) — nested category

### Step 2 — Determine the file path

Based on your resource ID and area:

| Resource ID | Category | Path |
|------------|----------|------|
| `OpenDsc.Windows/Environment` | Windows | `docs/reference/resources/windows/environment.md` |
| `OpenDsc.Windows.FileSystem/AccessControlList` | Windows > File System | `docs/reference/resources/windows/filesystem/acl.md` |
| `OpenDsc.Archive.Zip/Compress` | Archive > ZIP | `docs/reference/resources/archive/zip/compress.md` |
| `OpenDsc.SqlServer/Login` | SQL Server | `docs/reference/resources/sqlserver/login.md` |
| `OpenDsc.FileSystem/File` | File System | `docs/reference/resources/filesystem/file.md` |
| `OpenDsc.Posix.FileSystem/Permission` | POSIX > File System | `docs/reference/resources/posix/filesystem/permission.md` |

Create the file at the determined path.

### Step 3 — Create the documentation file

Use this template with the resource's properties, capabilities, and examples:

```markdown
# {Full Resource Name}

## Synopsis

{One sentence describing what the resource does. End with a period.}

## Type

\`\`\`text
OpenDsc.{Area}/{Name}
\`\`\`

## Capabilities

- Get
- Set
- [Delete if applicable]
- [Test if applicable]
- [Export if applicable]

## Properties

### {propertyName}

{Description of the property.}

\`\`\`yaml
Type: {type}
Required: Yes|No
Access: Read/Write|Read-Only|Write-Only
Default value: {value or None}
\`\`\`

[Repeat property section for each property]

!!! note
    {Optional: Any special notes, privileges required, etc.}

## Examples

### Example 1 — {Action description}

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    \`\`\`powershell
    $resourceInput = @'
    {yaml input here}
    '@

    dsc resource {get|set|delete} -r OpenDsc.{Area}/{Name} --input $resourceInput
    \`\`\`

=== "Shell"

    \`\`\`sh
    resource_input=$(cat <<'EOF'
    {yaml input here}
    EOF
    )

    dsc resource {get|set|delete} -r OpenDsc.{Area}/{Name} --input "$resource_input"
    \`\`\`

<!-- markdownlint-enable MD046 -->

[Add Example 2, 3, etc. with different scenarios]

### Example {N} — Configuration document

\`\`\`yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: {Resource description}
    type: OpenDsc.{Area}/{Name}
    properties:
      {yaml properties here}
\`\`\`

## Exit codes

| Code | Description                |
| :--- | :------------------------- |
| 0    | Success                    |
| 1    | Error                      |
| {N}  | {Specific error condition} |
```

**Property sections checklist:**
- [ ] All required and optional properties documented
- [ ] Each property includes: description, YAML type, Required (Yes/No), Access level, Default value
- [ ] Properties related to resource state: `_exist` (Read/Write), `_purge` (Write-Only), `_inDesiredState` (Read-Only)
- [ ] Write-Only properties (passwords, _purge) documented with appropriate warnings

**Examples checklist:**
- [ ] At least 1 Get example
- [ ] At least 1 Set example
- [ ] At least 1 Delete example (if IDeletable)
- [ ] All examples include both PowerShell and Shell tabs using `=== "PowerShell"` and `=== "Shell"` syntax
- [ ] All examples include `<!-- markdownlint-disable MD046 -->` and `<!-- markdownlint-enable MD046 -->` to allow superfences
- [ ] One configuration document example (YAML with `$schema`)
- [ ] Example descriptions are action-oriented (e.g., "Create a user with a password")

### Step 4 — Update mkdocs.yml navigation

Read [`mkdocs.yml`](../../../mkdocs.yml) and find the appropriate section under the Resources hierarchy.

**Location in mkdocs.yml:**
- `- Resources:` at line ~126
- Then nested by category (Archive, File System, JSON, etc.)
- Then by subcategory if applicable (e.g., `ZIP:` under Archive)

**Add entry in sorted order:**

```yaml
- Resources:
    - reference/resources/index.md
    - Archive:
        ZIP:
          - Compress: reference/resources/archive/zip/compress.md
          - Expand: reference/resources/archive/zip/expand.md
          - YourResource: reference/resources/archive/zip/your-resource.md  # Add here
    - File System:
        ...
    - Windows:
        ...
```

For nested categories, create the intermediate heading if it doesn't exist:

```yaml
- Windows:
    - Environment: reference/resources/windows/environment.md
    - File System:           # Create this if adding first File System resource under Windows
        - Access Control List: reference/resources/windows/filesystem/acl.md
        - YourResource: reference/resources/windows/filesystem/your-resource.md
```

### Step 5 — Update docs/reference/resources/index.md

Read [`docs/reference/resources/index.md`](../../../docs/reference/resources/index.md) to find the appropriate resource table.

**Add a table row** in the correct section with:
- Resource link: `[`OpenDsc.Area/Name`](path/to/resource.md)`
- Description: one-line summary

**Example:**

To add `OpenDsc.Windows/Registry` to the Windows resources table:

```markdown
| [`OpenDsc.Windows/Registry`](windows/registry.md)                    | Manage Windows registry keys and values |
```

Keep rows alphabetically sorted within their table.

## Content Tabs and Superfences

Resource documentation uses **MkDocs Material's content tabs** feature to show examples in both PowerShell and Shell (sh).

### Tab Syntax

Wrap each example in tab markers. The `===` syntax creates tabs:

```markdown
=== "PowerShell"

    ```powershell
    # PowerShell code here
    ```

=== "Shell"

    ```sh
    # Shell code here
    ```
```

### Disabling Markdownlint for Superfences

Superfences (consecutive code blocks in tabs) trigger markdownlint rule MD046. Disable it around tab sections:

```markdown
<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    key: value
    '@

    dsc resource get -r OpenDsc.Category/Resource --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    key: value
    EOF
    )

    dsc resource get -r OpenDsc.Category/Resource --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->
```

### Best Practices for Examples

- **Indentation**: Each code block inside a tab must be indented with 4 spaces in the markdown source
- **YAML input**: Use multiline YAML with `@'` and `'@` in PowerShell; use `cat <<'EOF'` and `EOF` in Shell
- **Command format**: Always match `dsc resource {get|set|delete|test|export}` with the example goal
- **Consistency**: PowerShell example first, then Shell example (both using same YAML input for clarity)

## Template File

A complete template is available here: [`Resource.md.template`](./Resource.md.template)

Use it as a starting point for your resource documentation.

## Verification Checklist

Before completing the skill:

- [ ] Documentation file created at correct path
- [ ] All property sections completed with Type, Required, Access, Default value
- [ ] At least 3 examples provided (Get, Set, and one other operation)
- [ ] All examples have both PowerShell and Shell tabs
- [ ] All examples use correct YAML syntax and valid DSC resource input
- [ ] Markdownlint directives (`<!-- markdownlint-disable/enable MD046 -->`) wrap tab sections
- [ ] Configuration document example included
- [ ] Exit codes section completed
- [ ] mkdocs.yml entry added in alphabetical order within correct category
- [ ] docs/reference/resources/index.md table row added in alphabetical order
- [ ] Navigation hierarchy matches directory structure (Archive > ZIP, Windows > File System, etc.)

## Common Pitfalls

1. **Missing markdownlint directives** — Superfences will fail linting without `<!-- markdownlint-disable MD046 -->`
2. **Incorrect indentation in tabs** — Code blocks inside tabs must be indented 4 spaces
3. **mkdocs.yml not updated** — Navigation won't appear on site-generated docs
4. **Unsorted entries** — Keep both mkdocs.yml and index.md entries alphabetically sorted within sections
5. **Mismatched paths** — mkdocs.yml path must match actual file path: `reference/resources/windows/service.md` is correct, `docs/reference/resources/windows/service.md` is wrong (no `docs/` prefix)
6. **Missing YAML fence markers in examples** — Always include `@'...'@` or `cat <<'EOF'...EOF'` in YAML input sections
7. **Property access levels** — Read-Only properties should not appear in example inputs; Write-Only properties should appear only in Set operations and not in Get output examples

## See Also

- [MkDocs Material Content Tabs](https://squidfunk.github.io/mkdocs-material/reference/content-tabs/)
- [MkDocs Material Admonitions](https://squidfunk.github.io/mkdocs-material/reference/admonitions/) — for !!! note, warning, etc.
- [OpenDSC Copilot Instructions](../../../.github/copilot-instructions.md)
