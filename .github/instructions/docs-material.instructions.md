---
name: docs-material
description: "DOCUMENTATION INSTRUCTIONS — Material for MKDocs conventions for OpenDSC docs (excluding pull-server). Quick reference: admonition types, icon conventions, tab patterns, code block practices, consistency rules. For detailed workflows, see skills: /add-admonitions and /add-content-tabs."
applyTo: "docs/**/*.md,!docs/pull-server/**"
---

# Material for MKDocs Documentation Standards

Concise reference for Material for MKDocs usage in OpenDSC documentation. **For detailed workflows, examples, and step-by-step guidance, see the skills** (use `/add-admonitions` and `/add-content-tabs`).

## Admonition Types

| Type | When to Use | Common Examples |
|------|-----------|-----------------|
| `!!! note` | Neutral info, clarifications | Scope definitions, optional parameters |
| `!!! tip` | Best practices, recommendations | Performance tips, workflow shortcuts |
| `!!! warning` | Important cautions, prerequisites | Privilege requirements, breaking changes |
| `!!! danger` | Destructive/high-risk operations | Delete operations, data loss, production changes |
| `!!! example` | Concrete use cases, workflows | Real-world scenarios, sample configurations |
| `!!! abstract` | Key takeaways, summaries | Learning objectives, section introductions |
| `!!! quote` | External references, citations | Official definitions, quotes from standards |

## Icon Conventions

| Use | Shortcode | Platforms/Status |
|-----|-----------|-----------------|
| Windows | `:fontawesome-brands-windows:` | Platform indicator, OS-specific tabs |
| Linux | `:fontawesome-brands-linux:` | Platform indicator, OS-specific tabs |
| macOS | `:fontawesome-brands-apple:` | Platform indicator, OS-specific tabs |

## Content Tab Patterns

**Multi-platform examples** (Windows/Linux/macOS): Use icons, order by prevalence
```
=== ":fontawesome-brands-windows: Windows"
    [content]
```

**Language/CLI variants** (PowerShell/Shell/Python): No icons needed, capitalize labels
```
=== "PowerShell"
    [content]
```

**Tab naming rules**: Be consistent (always "Shell", never "Bash"), capitalize first word, use icon prefix for platforms.

## Code Block Best Practices

- Always specify language: `powershell`, `yaml`, `json`, `bash`, `sh`, etc.
- Add title for context: `title="appsettings.json"` or `title="PowerShell"`
- Use line numbers for blocks 10+ lines or when referencing specific lines
- Annotate non-obvious logic with `# (1)` comment markers

## General Consistency Rules

- **Headings**: `#` page title (one per page), `##` major sections, `###` subsections
- **Lists**: Ordered for step-by-step procedures, unordered for options/collections
- **Internal links**: `[section name](#section-name)`
- **Images**: Always include alt text; store in `media/` subdirectories
- **Inline code**: Commands, property/variable names, filenames (use backticks)
- **Code blocks**: Multi-line code, configs, examples (use triple-backticks with language)

---

**See [Material for MKDocs](https://squidfunk.github.io/mkdocs-material/reference/) for full icon/feature reference.**
