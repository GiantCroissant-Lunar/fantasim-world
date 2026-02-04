# Workflow Schema

Universal workflow format for AI coding agents across platforms (Claude, Windsurf, Copilot, Cursor, etc.)

## Format

Workflows are defined in YAML format in `.agent/workflows/` as the **canonical source of truth**.

Platform-specific adapters in `.windsurf/workflows/`, `.claude/workflows/`, etc. reference or convert these canonical workflows.

## Schema

```yaml
name: string # Workflow identifier (kebab-case)
description: string # Brief description
version: string # Semantic version (e.g., "1.0.0")
platforms: string[] # Supported platforms: claude, windsurf, copilot, cursor

stages:
  - name: string # Stage name
    steps:
      - name: string # Step name
        type: command | manual | conditional | output
        command: string # Shell command (if type: command)
        description: string # Step description
        condition: string # Condition (if type: conditional)
        action: string # Action to take (if type: conditional)
        auto_fix: boolean # Auto-fix errors (default: false)
        template: string # Output template (if type: output)
```

## Step Types

### `command`

Execute a shell command.

```yaml
- name: Build application
  type: command
  command: task game:build-console
  description: Build the console app
  auto_fix: true # Agent should fix errors autonomously
```

### `manual`

Manual step requiring agent analysis or decision.

```yaml
- name: Analyze requirements
  type: manual
  description: |
    Review feature requirements
    Identify affected components
    Plan implementation approach
```

### `conditional`

Conditional action based on previous step results.

```yaml
- name: Fix build errors
  type: conditional
  condition: build_failed
  action: |
    Read error messages
    Identify issues
    Fix code
    Rebuild
```

### `output`

Format and display results.

```yaml
- name: Report results
  type: output
  template: |
    Build: {{ build_status }}
    Tests: {{ test_count }} passed
    Status: {{ final_status }}
```

## Variables

Workflows can use template variables:

- `{{ platform }}` - Current platform (claude, windsurf, etc.)
- `{{ project_root }}` - Project root directory
- `{{ git_branch }}` - Current git branch
- `{{ build_status }}` - Build result (success/failed)
- `{{ test_results }}` - Test results
- Custom variables defined in workflow

## Platform Adapters

Each platform has its own adapter format:

### Windsurf (Markdown)

`.windsurf/workflows/workflow-name.md` - Markdown with steps

### Claude (Markdown)

`.claude/workflows/workflow-name.md` - Markdown procedural format

### GitHub Copilot (Markdown)

`.github/copilot-workflows/workflow-name.md` - Copilot instruction format

### Cursor (Rules)

`.cursor/workflows/workflow-name.cursorrules` - Cursor rules format

## Conversion

Use `scripts/generate-workflows.py` to convert canonical YAML to platform-specific formats:

```bash
# Generate all platform workflows
python scripts/generate-workflows.py

# Generate for specific platform
python scripts/generate-workflows.py --platform windsurf

# Generate specific workflow
python scripts/generate-workflows.py --workflow build-and-test
```

## Best Practices

### 1. Single Source of Truth

Maintain workflows in `.agent/workflows/*.yaml` only. Generate platform formats from these.

### 2. Platform-Agnostic Commands

Use Task runner commands that work across platforms:

```yaml
command: task game:build-console    # ✅ Works everywhere
command: dotnet build ...           # ❌ Platform-specific path
```

### 3. Auto-Fix Flag

Use `auto_fix: true` for steps that agents should handle autonomously:

```yaml
- name: Build and fix
  type: command
  command: task game:build-console
  auto_fix: true # Agent fixes build errors
```

### 4. Clear Descriptions

Provide clear guidance for manual steps:

```yaml
- name: Review code
  type: manual
  description: |
    1. Read affected files
    2. Check for code quality
    3. Identify potential issues
```

### 5. Structured Reporting

Use output templates for consistent reporting:

```yaml
- name: Summary
  type: output
  template: |
    ✅ {{ success_count }} successful
    ❌ {{ failure_count }} failed
    Status: {{ status }}
```

## Example: Complete Workflow

```yaml
name: quick-fix
description: Quick bug fix workflow
version: 1.0.0
platforms:
  - claude
  - windsurf
  - copilot

stages:
  - name: Diagnose
    steps:
      - name: Build and run
        type: command
        command: task game:build-and-run-console

      - name: Identify issue
        type: manual
        description: Analyze errors and identify root cause

  - name: Fix
    steps:
      - name: Implement fix
        type: manual
        description: Make targeted code changes

      - name: Rebuild
        type: command
        command: task game:build-console
        auto_fix: true

  - name: Verify
    steps:
      - name: Test
        type: command
        command: task dotnet:test
        auto_fix: true

      - name: Report
        type: output
        template: |
          Fixed: {{ issue_description }}
          Status: {{ status }}
```

## Migration Guide

### From Platform-Specific to Canonical

1. **Identify common workflows** across platforms
2. **Create YAML definitions** in `.agent/workflows/`
3. **Generate platform formats** with conversion script
4. **Replace platform files** with generated versions
5. **Update only YAML** when workflows change

### Adding New Platform

1. **Create platform adapter** in `scripts/adapters/platform_name.py`
2. **Define conversion logic** from YAML to platform format
3. **Add platform directory** (e.g., `.platform/workflows/`)
4. **Generate workflows** with script
5. **Test with platform** tools

## Related

- **`.agent/workflows/README.md`** - Workflow overview
- **`.windsurf/workflows/README.md`** - Windsurf-specific docs
- **`scripts/generate-workflows.py`** - Conversion script
- **`AGENTS.md`** - Complete agent infrastructure
