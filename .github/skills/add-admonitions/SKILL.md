---
name: add-admonitions
description: "WORKFLOW SKILL — Add or enhance admonition blocks (notes, warnings, tips, dangers) in OpenDSC documentation. USE FOR: adding call-outs to guides, tutorials, or reference docs; replacing plain text emphasis with structured admonitions; upgrading existing plain text warnings/cautions; nesting admonitions in complex sections. Guides through: choosing correct admonition type based on content (note/tip/warning/danger/example/abstract), syntax patterns, collapsible blocks, and inline positioning per docs-material.instructions.md standards. INVOKES: file read/edit tools. DO NOT USE FOR: modifying non-documentation content; creating entirely new doc files (use other skills for that)."
---

# Add Admonitions

## Overview

Admonitions are Material for MKDocs call-outs that emphasize, warn, or highlight content. Each type serves a specific purpose in OpenDSC documentation:

- **`note`** — Neutral information and clarifications
- **`tip`** — Best practices and helpful advice
- **`warning`** — Important cautions and prerequisites
- **`danger`** — Critical or destructive operations
- **`example`** — Concrete use cases and workflows
- **`abstract`** — Key takeaways and summaries
- **`quote`** — External references or citations

## Required Inputs

Before starting, determine:

1. **File to enhance** — Full path to the `.md` file
2. **Content type** — What kind of information are you emphasizing? (caution, best practice, definition, etc.)
3. **Admonition type** — Which type best matches the content? (note/tip/warning/danger/example/abstract)
4. **Placement** — Where should the admonition appear? (before a section, inline with code, after a paragraph)
5. **Title** — Should the admonition have a custom title? (optional; defaults to type name)
6. **Collapsible?** — Should users expand/collapse it? (use `???` instead of `!!!`)

## Decision Tree — Choose the Right Admonition Type

```
Is this content...

├─ A caution or critical requirement?
│  ├─ Destructive or high-risk? → DANGER (red)
│  └─ Important but recoverable? → WARNING (orange)
│
├─ Helpful advice or best practice?
│  └─ TIP (blue)
│
├─ Neutral information or clarification?
│  └─ NOTE (blue)
│
├─ An example or real-world scenario?
│  └─ EXAMPLE (purple)
│
├─ A key takeaway or summary?
│  └─ ABSTRACT (blue)
│
└─ A reference or external quote?
   └─ QUOTE (blue)
```

## Step-by-Step Process

### Step 1 — Identify Admonition-Worthy Content

Scan the file for content that should be emphasized:

- **Prerequisites** (e.g., "requires admin privileges") → `warning`
- **Best practices** (e.g., "use service accounts for automation") → `tip`
- **Destructive actions** (e.g., "deleting a config is permanent") → `danger`
- **Additional context** (e.g., "scope is either User or Machine") → `note`
- **Example scenarios** (e.g., "here's how you'd use this in production") → `example`
- **Learning objectives** (e.g., "every resource supports Get/Set/Delete") → `abstract`

### Step 2 — Choose Admonition Type

Use the decision tree above. Reference [docs-material.instructions.md](../../instructions/docs-material.instructions.md) for detailed type definitions.

### Step 3 — Write the Admonition

#### Basic Syntax

```markdown
!!! note
    This is a note admonition. Single paragraph, indented by four spaces.
```

#### With Custom Title

```markdown
!!! warning "Privilege Alert"
    Setting `scope: Machine` requires administrator privileges.
```

#### Collapsible (Expandable/Collapsible)

```markdown
??? tip "Advanced: Performance Optimization"
    This section can be collapsed. Click to expand.

    Use this for advanced or optional information.
```

#### Collapsible Expanded by Default

```markdown
???+ note "Important Prerequisites"
    Appears expanded by default. Users can collapse it.
```

#### Multi-Paragraph or Nested Content

Indent continuation content by four spaces:

```markdown
!!! example "Full Workflow Example"

    First paragraph of the example.

    ```powershell
    # Code block inside admonition—must be indented
    dsc resource get ...
    ```

    Final paragraph with explanation.
```

#### Nested Admonitions

Indent nested admonitions by eight spaces (four more than parent):

```markdown
!!! warning "Destructive Operation"

    This will delete the configuration.

    !!! danger "Unrecoverable"
        This cannot be undone. Ensure you have backups.
```

### Step 4 — Placement Guidelines

**Before a section heading** (emphasizes what follows):

```markdown
!!! warning
    The following steps require administrator privileges.

## Configure Machine-Scoped Variables

[Instructions...]
```

**After an introductory sentence** (clarifies a specific point):

```markdown
The scope parameter controls where the variable is stored.

!!! note
    User scope stores variables in `HKEY_CURRENT_USER\Environment`.
    Machine scope requires admin privileges.

## Scope Options

[Details about scope options...]
```

**Inline with code blocks** (explains side effects or requirements):

````markdown
```powershell
Set-DscLocalConfigurationManager -Force
```

!!! warning
    Using `-Force` will restart the LCM service. Running DSC operations may be interrupted.
````

**At the end of a section** (summary or call-to-action):

```markdown
## Important Notes

[Content...]

!!! tip
    Use Service Principal accounts for secure automated deployments.
```

### Step 5 — Common Patterns in OpenDSC Docs

#### Pattern 1 — Privilege Requirements (in resource docs)

```markdown
!!! warning
    Setting `scope: Machine` requires administrator privileges.
```

#### Pattern 2 — Destructive Operations (in guides)

```markdown
!!! danger
    Deleting a configuration will permanently remove all versions and associated parameters. This cannot be undone.
```

#### Pattern 3 — Best Practices (in guides and tutorials)

```markdown
!!! tip
    Use Service Principal accounts for automated deployments to reduce credential exposure.
```

#### Pattern 4 — Optional/Advanced Content (collapsible)

```markdown
??? tip "Advanced: Custom Icons"
    You can customize admonition icons by editing your `mkdocs.yml`:

    ```yaml
    theme:
      icon:
        admonition:
          note: fontawesome/solid/note-sticky
    ```
```

#### Pattern 5 — Learning Objectives (at section start)

```markdown
!!! abstract "What You'll Learn"

    - DSC resource fundamentals
    - How to invoke Get/Set/Delete operations
    - Multi-platform resource usage
```

### Step 6 — Validation Checklist

Before finalizing:

- [ ] Admonition type matches content (refer to decision tree in Step 2)
- [ ] Custom title (if used) is descriptive and follows title case
- [ ] Content is indented correctly (4 spaces for direct content, 8 for nested)
- [ ] Code blocks inside admonitions have proper indentation
- [ ] Admonition is placed logically (before/after relevant section)
- [ ] No more than 3 levels of nesting (admonition → admonition → admonition)
- [ ] Collapsible blocks (`???`) used only for optional/advanced content
- [ ] No spelling or grammar errors

### Step 7 — Build and Preview

Build the docs locally to verify appearance:

```powershell
mkdocs serve
```

Navigate to the affected page and verify:
- Admonition renders with correct color and icon
- Text is readable and properly indented
- Links and code blocks inside admonitions display correctly
- Collapsible blocks expand/collapse smoothly

## Real-World Examples

### Example 1 — Adding a warning to a guide

**Before:**

```markdown
## Configure Global Retention

1. Navigate to **Settings → Retention**.
2. Set the **Maximum versions to keep** value.
3. Choose whether to retain published versions, all versions, or only the latest.
4. Click **Save**.

Note: Regular cleanup will help manage disk usage.
```

**After:**

```markdown
## Configure Global Retention

!!! warning
    This action should be done during low-usage periods. The Pull Server will be briefly unavailable during cleanup.

1. Navigate to **Settings → Retention**.
2. Set the **Maximum versions to keep** value.
3. Choose whether to retain published versions, all versions, or only the latest.
4. Click **Save**.

!!! tip
    Start with a conservative value (5-10 versions) and adjust based on your storage and rollback needs.
```

### Example 2 — Adding context to a resource property

**Before:**

```markdown
### scope

The scope. Accepts `User` or `Machine`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: User
```
```

**After:**

```markdown
### scope

The scope. Accepts `User` or `Machine`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: User
```

!!! warning
    Setting `scope: Machine` requires administrator privileges. Non-admin users will receive an "Access denied" error.
```

### Example 3 — Advanced collapsible section

**Before:**

```markdown
## Configuration Priority

The LCM evaluates configurations in this order: Local > Composite > Pull Server.
Details on each type are available in the LCM concepts guide.
```

**After:**

```markdown
## Configuration Priority

The LCM evaluates configurations in this order: Local > Composite > Pull Server.

??? abstract "Configuration Type Details"

    **Local Configuration**
    : Stored in `%ProgramFiles%\DSC\Configuration`.

    **Composite Configuration**
    : Built from component configurations and parameters.

    **Pull Server Configuration**
    : Downloaded from the Pull Server REST API.
```

## Material for MKDocs References

- **Admonitions**: https://squidfunk.github.io/mkdocs-material/reference/admonitions/
- **Collapsible Blocks**: https://squidfunk.github.io/mkdocs-material/reference/admonitions/#collapsible-blocks
- **Nested Admonitions**: https://squidfunk.github.io/mkdocs-material/reference/admonitions/#nested-admonitions

## Common Mistakes to Avoid

❌ **Don't mix content types in one admonition:**
```markdown
!!! warning
    Requires admin. Also, here's a tip: use Service Principals.
```

✅ **Use separate admonitions:**
```markdown
!!! warning
    Requires administrator privileges.

!!! tip
    Use Service Principals for secure automation.
```

---

❌ **Don't indent admonitions with tabs:**
```markdown
	!!! note
	    This is indented with tabs (wrong).
```

✅ **Use four spaces:**
```markdown
    !!! note
        This is indented with four spaces (correct).
```

---

❌ **Don't overuse collapsible blocks:**
```markdown
??? note
    Always collapsed, even for critical info.
```

✅ **Reserve `???` for optional/advanced content:**
```markdown
!!! warning
    Critical: This is destructive.

??? tip "Advanced: Optional feature"
    This is nice-to-know but not required.
```
