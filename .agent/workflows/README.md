# Agent Workflows

This directory contains **canonical workflow definitions** that orchestrate multiple agent actions across all platforms.

## Overview

Workflows are the **single source of truth** for all platform-specific workflow implementations.

### Architecture

```
.agent/workflows/*.yaml          → Canonical definitions (edit these)
                                 ↓
scripts/generate-workflows.py    → Conversion script
                                 ↓
.windsurf/workflows/*.md         → Platform-specific (auto-generated)
.claude/workflows/*.md           → Platform-specific (auto-generated)
.github/copilot-workflows/*.md   → Platform-specific (auto-generated)
.cursor/workflows/*.cursorrules  → Platform-specific (auto-generated)
```

## Workflow Files

### Canonical Workflows (YAML)

- **`build-and-test.yaml`** - Build, test, and fix workflow
- **`fix-bug.yaml`** - Bug investigation and fixing
- **`commit.yaml`** - Autonomous commit with verification
- **`feature-development.yaml`** - Complete feature workflow
- **`SCHEMA.md`** - Workflow format specification

### Usage

**DO NOT edit platform-specific workflow files directly.** They are auto-generated.

Instead:

1. Edit YAML files in `.agent/workflows/`
2. Run generation script: `python scripts/generate-workflows.py`
3. Platform-specific files are automatically updated

## Generating Workflows

```bash
# Generate for all platforms
python scripts/generate-workflows.py

# Generate for specific platform
python scripts/generate-workflows.py --platform windsurf

# Generate specific workflow
python scripts/generate-workflows.py --workflow build-and-test
```

## Workflow Format

See [`SCHEMA.md`](SCHEMA.md) for complete format specification.

### Basic Structure

```yaml
name: workflow-name
description: Brief description
version: 1.0.0
platforms:
  - claude
  - windsurf
  - copilot
  - cursor

stages:
  - name: Stage Name
    steps:
      - name: Step name
        type: command | manual | conditional | output
        command: task command
        description: What this step does
```

### Step Types

- **`command`** - Execute shell command
- **`manual`** - Agent analyzes and decides
- **`conditional`** - Execute if condition met
- **`output`** - Format and display results

## Platform-Specific Formats

Each platform has its own workflow format:

| Platform | Format   | Location                          | Invocation       |
| -------- | -------- | --------------------------------- | ---------------- |
| Windsurf | Markdown | `.windsurf/workflows/*.md`        | `/workflow-name` |
| Claude   | Markdown | `.claude/workflows/*.md`          | `@workflow-name` |
| Copilot  | Markdown | `.github/copilot-workflows/*.md`  | `/workflow-name` |
| Cursor   | Rules    | `.cursor/workflows/*.cursorrules` | `@workflow-name` |

## Creating New Workflows

1. **Create YAML file** in `.agent/workflows/new-workflow.yaml`
2. **Define stages and steps** following the schema
3. **Generate platform formats**:

   ```bash
   python scripts/generate-workflows.py --workflow new-workflow
   ```

4. **Test on each platform** with invocation command

Example:

```yaml
name: quick-test
description: Quick test workflow
version: 1.0.0
platforms:
  - windsurf
  - claude

stages:
  - name: Test
    steps:
      - name: Run tests
        type: command
        command: task dotnet:test
        auto_fix: true
```

## Workflow Best Practices

### 1. Platform-Agnostic Commands

Use Task runner commands that work everywhere:

```yaml
✅ command: task game:build-console
❌ command: cd projects/dungeon && dotnet build
```

### 2. Auto-Fix for Autonomous Steps

```yaml
- name: Build
  type: command
  command: task game:build-console
  auto_fix: true # Agent fixes errors autonomously
```

### 3. Clear Manual Step Descriptions

```yaml
- name: Review code
  type: manual
  description: |
    1. Read the changed files
    2. Check for code quality issues
    3. Verify security implications
```

### 4. Consistent Reporting

```yaml
- name: Summary
  type: output
  template: |
    Status: {{ status }}
    Tests: {{ test_count }} passed
```

## Multi-Platform Support

All workflows automatically support multiple platforms:

```yaml
platforms:
  - claude # Works in Claude Code
  - windsurf # Works in Windsurf
  - copilot # Works in GitHub Copilot
  - cursor # Works in Cursor
```

The generator creates platform-specific versions from the canonical YAML.

## Example Workflows

### Build and Test

Invoke: `/build-and-test` (Windsurf), `@build-and-test` (Claude/Cursor)

- Builds console app
- Fixes build errors autonomously
- Runs tests
- Fixes test failures
- Verifies app runs
- Reports results

### Fix Bug

Invoke: `/fix-bug`

- Reproduces bug
- Investigates root cause
- Implements fix
- Adds regression test
- Verifies fix
- Commits changes

### Commit

Invoke: `/commit`

- Formats code
- Runs pre-commit hooks
- Builds and tests
- Creates commit message
- Commits with verification

## Related Documentation

- **Workflow Schema**: [`.agent/workflows/SCHEMA.md`](SCHEMA.md)
- **Platform Adapters**: [`.agent/adapters/README.md`](../adapters/README.md)
- **Provider Configs**: [`.agent/providers/README.md`](../providers/README.md)
- **Generation Script**: [`scripts/generate-workflows.py`](../../scripts/generate-workflows.py)
- **Windsurf Workflows**: [`.windsurf/workflows/README.md`](../../.windsurf/workflows/README.md)
