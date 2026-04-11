---
name: add-content-tabs
description: "WORKFLOW SKILL — Create or enhance content tabs (tabbed code blocks and content groups) in OpenDSC documentation. USE FOR: grouping platform-specific examples (Windows/Linux/macOS); creating language or CLI variant tabs; multi-version instructions. Guides through: tab syntax (`=== \"Label\"`), icon/emoji conventions (platform badges), linking tabs across pages, nesting tabs in admonitions, and consistent naming per docs-material.instructions.md. INVOKES: file read/edit tools. DO NOT USE FOR: modifying non-documentation files; text content outside of tabs."
---

# Add Content Tabs

## Overview

Content tabs group related content under different labels. Users can click to switch between options. Material for MKDocs supports:

- **Multi-platform examples** — Windows, Linux, macOS variants
- **Language/CLI variants** — PowerShell, Shell, Python, etc.
- **Version-specific instructions** — Different setup for different versions

Tabs can contain code blocks, prose, admonitions, and even nested tabs.

## Required Inputs

Before starting, determine:

1. **Content groups** — What are the logical variants? (e.g., Windows, Linux, macOS)
2. **Tab labels** — What should each tab be called? (use consistent naming across docs)
3. **Icons** — Should tabs have platform icons? (recommended)
4. **Existing tabs?** — Are you creating new tabs or enhancing existing ones?
5. **Placement** — Where should tabs appear in the file?
6. **Linking** — Should tabs be linked across the page/site? (see `content.tabs.link` feature)

## Tab Naming Conventions

### Platform Tabs (Most Common)

**Format**: `=== ":icon: Label"`

Use when instructions, examples, or setup differ by operating system:

```markdown
=== ":fontawesome-brands-windows: Windows"

    Windows-specific content...

=== ":fontawesome-brands-linux: Linux"

    Linux-specific content...

=== ":fontawesome-brands-apple: macOS"

    macOS-specific content...
```

**Icon choices**:
- Windows: `:fontawesome-brands-windows:`
- Linux: `:fontawesome-brands-linux:`
- macOS: `:fontawesome-brands-apple:`

**Ordering**: Windows → Linux → macOS (by prevalence in OpenDSC)

---

### CLI/Language Tabs

**Format**: `=== "Label"` (optional icon if relevant)

Use when showing equivalent operations in different tools or languages:

```markdown
=== "PowerShell"

    ```powershell
    dsc resource get ...
    ```

=== "Shell"

    ```bash
    dsc resource get ...
    ```
```

**Naming conventions**:
- Use "PowerShell", "Shell", "Python" (not "ps1", "bash", "py")
- Be consistent: if one page uses "Shell", all pages use "Shell" (not "Bash")
- Capitalize first word only: `=== "PowerShell CLI"` not `=== "Powershell CLI"`

---

## Step-by-Step Process

### Step 1 — Identify Candidate Content

Look for content where **multiple variant paths exist**:

- Installation instructions (different per OS)
- Resource examples (different per OS)
- Code samples (different languages)

**Red flags for tabs**:
- Two similar paragraphs that differ only in OS details → Tab it
- "If Windows, do X. If Linux, do Y." → Tab it
- Separate code blocks with comments like `# Windows version` → Tab it

### Step 2 — Choose Tab Structure

Decide on the logical grouping:

| Scenario | Tab Labels | Icon? |
|----------|-----------|-------|
| Installation per OS | Windows, Linux, macOS | ✅ Yes (platform icons) |
| Setup per environment | Development, Staging, Production | ❌ No |
| Programming languages | PowerShell, Shell, Python | ✅ Optional |
| Version variants | v1, v2, v3 | ❌ No |

### Step 3 — Create Tab Structure

#### Basic Syntax

```markdown
=== "Tab 1 Label"

    Content for tab 1.
    Use paragraph breaks for multiple lines.

=== "Tab 2 Label"

    Content for tab 2.
```

#### With Platform Icons

```markdown
=== ":fontawesome-brands-windows: Windows"

    Windows-specific example:

    ```powershell
    Get-ChildItem
    ```

=== ":fontawesome-brands-linux: Linux"

    Linux-specific example:

    ```bash
    ls -la
    ```
```

#### Code Blocks Only

When all tabs contain code blocks, Material renders them without horizontal spacing (streamlined):

```markdown
=== "C"

    ``` c
    #include <stdio.h>
    int main() { printf("Hello\n"); }
    ```

=== "C++"

    ``` c++
    #include <iostream>
    int main() { std::cout << "Hello" << std::endl; }
    ```
```

#### Tabs with Mixed Content

When tabs contain prose + code, they render with horizontal spacing:

```markdown
=== "Local Configuration"

    Local configurations are stored in `%ProgramFiles%\DSC\Configuration`.

    Example:

    ```yaml
    name: LocalConfig
    resources: [...]
    ```

=== "Pull Server Configuration"

    Pull Server configurations are downloaded via REST API.

    Example:

    ```powershell
    Invoke-RestMethod http://pull-server/api/configuration/MyConfig
    ```
```

#### Nested Tabs (Advanced)

Tabs can be nested inside admonitions or other tabs:

```markdown
!!! example

    === ":fontawesome-brands-windows: Windows"

        First nested tab content.

    === ":fontawesome-brands-linux: Linux"

        Second nested tab content.
```

---

### Step 4 — Placement & Context

**Provide introductory text before tabs:**

```markdown
## Installation

Choose your operating system below:

=== ":fontawesome-brands-windows: Windows"

    [Windows installation...]

=== ":fontawesome-brands-linux: Linux"

    [Linux installation...]
```

**Avoid placing tabs immediately after a heading** without context.

---

### Step 5 — Common Patterns in OpenDSC Docs

#### Pattern 1 — Platform-Specific Resource Examples

Used in all resource reference docs:

```markdown
### Example — Get an environment variable

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    dsc resource get -r OpenDsc.Windows/Environment --input @{name = "PATH"}
    ```

=== ":fontawesome-brands-linux: Linux"

    ```bash
    dsc resource get -r OpenDsc.FileSystem/File --input "path: /etc/hosts"
    ```
```

#### Pattern 2 — Installation Instructions

Used in get-started guides:

```markdown
## Install

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    winget install OpenDsc.Resources
    ```

=== ":fontawesome-brands-linux: Linux"

    ```bash
    export PATH="$HOME/OpenDSC.Resources:$PATH"
    ```
```

#### Pattern 3 — Configuration Variants

```markdown
## Configure Parameter Merging

=== "Additive (Default)"

    Parameters from all scopes are merged.

    ```yaml
    mergeStrategy: additive
    ```

=== "Overwrite"

    Later scopes override earlier ones.

    ```yaml
    mergeStrategy: overwrite
    ```
```

---

### Step 6 — Linked Tabs (Site-Wide)

The `content.tabs.link` feature (enabled in `mkdocs.yml`) automatically synchronizes tabs with the same label across the entire site.

**How it works:**
- User selects "PowerShell" tab on one page
- All other pages automatically switch to "PowerShell" tabs if available
- Preference persists across page navigation

**To use**: Ensure tab labels are consistent across docs.

✅ **Good** (consistent):
```markdown
# guide1.md
=== "PowerShell"
    ...
=== "Shell"
    ...

# guide2.md
=== "PowerShell"
    ...
=== "Shell"
    ...
```

❌ **Bad** (inconsistent):
```markdown
# guide1.md
=== "PowerShell"
    ...
=== "Bash"
    ...

# guide2.md
=== "PowerShell"
    ...
=== "Shell"  # Different label!
    ...
```

---

### Step 7 — Validation Checklist

- [ ] Each tab has a clear, descriptive label
- [ ] Tab labels are consistent across docs (if using linked tabs feature)
- [ ] Content in each tab is complete and parallel in structure
- [ ] Platform icons used for OS-specific tabs
- [ ] No logical gaps (e.g., Windows instructions aren't in Linux tab)
- [ ] Code blocks inside tabs have correct syntax highlighting language
- [ ] All tabs render correctly in local preview

### Step 8 — Build and Preview

```powershell
mkdocs serve
```

Verify:
- Tabs appear and can be clicked
- Content switches smoothly
- Icons display correctly
- If using linked tabs: switching on one page affects other pages with same labels

## Real-World Examples

### Example 1 — Converting separate OS sections into tabs

**Before:**

```markdown
## Installation

### Windows

```powershell
winget install OpenDsc.Resources
```

If using Chocolatey:

```powershell
choco install opendsc-resources
```

### Linux

```bash
latest_tag=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest | grep -oP '"tag_name":\s*"\K(.*?)(?=")')
archive=OpenDSC.Resources.Linux.Portable-${latest_tag#v}.zip
mkdir -p "$HOME/OpenDSC.Resources"
curl -L -o "$archive" "https://github.com/opendsc/opendsc/releases/download/${latest_tag}/${archive}"
unzip -o "$archive" -d "$HOME/OpenDSC.Resources"
export PATH="$HOME/OpenDSC.Resources:$PATH"
```

### macOS

```bash
# Same as Linux, but with .zshrc instead of .bashrc
```
```

**After:**

```markdown
## Installation

Choose your operating system:

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    winget install OpenDsc.Resources
    ```

    Alternatively, using Chocolatey:

    ```powershell
    choco install opendsc-resources
    ```

=== ":fontawesome-brands-linux: Linux"

    ```bash
    latest_tag=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest | grep -oP '"tag_name":\s*"\K(.*?)(?=")')
    archive=OpenDSC.Resources.Linux.Portable-${latest_tag#v}.zip
    mkdir -p "$HOME/OpenDSC.Resources"
    curl -L -o "$archive" "https://github.com/opendsc/opendsc/releases/download/${latest_tag}/${archive}"
    unzip -o "$archive" -d "$HOME/OpenDSC.Resources"
    export PATH="$HOME/OpenDSC.Resources:$PATH"
    ```

=== ":fontawesome-brands-apple: macOS"

    ```bash
    latest_tag=$(curl -s https://api.github.io/repos/opendsc/opendsc/releases/latest | grep -oP '"tag_name":\s*"\K(.*?)(?=")')
    archive=OpenDSC.Resources.macOS.Portable-${latest_tag#v}.zip
    mkdir -p "$HOME/OpenDSC.Resources"
    curl -L -o "$archive" "https://github.com/opendsc/opendsc/releases/download/${latest_tag}/${archive}"
    unzip -o "$archive" -d "$HOME/OpenDSC.Resources"
    export PATH="$HOME/OpenDSC.Resources:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Resources:$PATH"' >> ~/.zshrc
    ```
```

### Example 2 — CLI variant tabs in a guide

**Before:**

```markdown
## Get a Configuration

Using PowerShell:

```powershell
dsc config get -n LabConfig
```

Using the dsccli tool:

```bash
dsccli config get --name LabConfig
```
```

**After:**

```markdown
## Get a Configuration

=== "PowerShell"

    ```powershell
    dsc config get -n LabConfig
    ```

=== "Shell"

    ```bash
    dsccli config get --name LabConfig
    ```
```

## Material for MKDocs References

- **Content Tabs**: https://squidfunk.github.io/mkdocs-material/reference/content-tabs/
- **Linked Tabs Feature**: https://squidfunk.github.io/mkdocs-material/reference/content-tabs/#linked-content-tabs

## Common Mistakes

❌ **Inconsistent label naming** (breaks linked tabs):
```markdown
# Page 1
=== "Powershell"

# Page 2
=== "PowerShell"  # Different capitalization!
```

✅ **Consistent labels:**
```markdown
# All pages
=== "PowerShell"
```

---

❌ **Missing introductory text:**
```markdown
## Installation

=== "Windows"
    [content...]
```

✅ **Provide context:**
```markdown
## Installation

Choose your operating system:

=== ":fontawesome-brands-windows: Windows"
    [content...]
```

---

❌ **Overusing tabs** (when a single universal example exists):
```markdown
=== "Windows"
    Example content
=== "Linux"
    Same example content
```

✅ **Use tabs only for genuine variance:**
Use tabs when content truly differs between options.
