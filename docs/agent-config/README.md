---
title: "Unified Agent Configuration System"
id: "readme"
description: ""
date: "2026-01-28"
tags: []
---
This document describes the unified agent configuration system that enables consistent AI assistant behavior across multiple tools from a single source of truth.

## Table of Contents

- [Overview](#overview)
- [Supported Tools](#supported-tools)
- [Directory Structure](#directory-structure)
- [Configuration Types](#configuration-types)
  - [Skills](#skills)
  - [Rules](#rules)
  - [Commands / Workflows](#commands--workflows)
  - [Hooks](#hooks)
- [Generated Files](#generated-files)
- [Memory and Reflections](#memory-and-reflections)
- [Sync Script Usage](#sync-script-usage)
- [Adding New Configuration](#adding-new-configuration)
- [Tool-Specific Notes](#tool-specific-notes)

---

## Overview

The `.agent/` directory serves as the **single source of truth** for all AI assistant configuration. A sync script generates tool-specific stub/pointer files that reference back to this shared source.

**Benefits:**

- **Consistency**: All tools receive the same instructions
- **Maintainability**: Edit once, apply everywhere
- **Version Control**: All configuration tracked in git
- **Living Documentation**: Reflections from completed work feed back into instructions

**Architecture:**

```
.agent/                          # Source of Truth
    ├── skills/                  # Agent capabilities
    ├── rules/                   # Project guidelines
    ├── commands/                # Slash commands / workflows
    ├── hooks/                   # Event-driven scripts
    └── memory/
        └── reflections/         # Learnings from past work
            │
            ▼
    ┌─────────────────────────────────────────────────────┐
    │              sync_skills.py                         │
    └─────────────────────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────────────────────────────┐
│  .claude/   .cline/   .codex/   .cursor/   .gemini/   etc.   │
│  CLAUDE.md  AGENTS.md  GEMINI.md                              │
└───────────────────────────────────────────────────────────────┘
```

---

## Supported Tools

| Tool | Skills | Rules | Commands | Hooks | Docs |
|------|--------|-------|----------|-------|------|
| Claude Code | `.claude/skills/` | `.claude/rules/` | — | `.claude/hooks/` | [Docs](https://code.claude.com/docs) |
| Cline | `.cline/skills/` | `.clinerules/` | `.clinerules/workflows/` | `.clinerules/hooks/` | [Docs](https://docs.cline.bot) |
| Codex | `.codex/skills/` | `AGENTS.md` | — | — | [Docs](https://developers.openai.com/codex) |
| Cursor | `.cursor/skills/` | `.cursor/rules/` | `.cursor/commands/` | `.cursor/hooks.json` | [Docs](https://cursor.com/docs) |
| Gemini CLI | `.gemini/skills/` | `GEMINI.md` | `.gemini/commands/` | — | [Docs](https://geminicli.com/docs) |
| Kilocode | `.kilocode/skills/` | — | — | — | — |
| OpenCode | `.opencode/skills/` | — | — | — | [Docs](https://opencode.ai/docs) |
| Windsurf | `.windsurf/skills/` | `.windsurf/rules/` | `.windsurf/workflows/` | — | [Docs](https://docs.windsurf.com) |

---

## Directory Structure

### Source (tracked in git)

```
.agent/
├── skills/                      # Agent capabilities
│   ├── SKILLS-INDEX.md          # Documentation and index
│   ├── code-review/
│   │   ├── SKILL.md             # Skill definition
│   │   └── checklist.md         # Supporting files
│   ├── implement-feature/
│   │   └── SKILL.md
│   ├── self-critique/
│   │   ├── SKILL.md
│   │   └── rubric.yaml          # Configurable evaluation criteria
│   ├── reflect/
│   │   └── SKILL.md
│   └── ...
│
├── rules/                       # Project guidelines
│   ├── 01-project-overview.md
│   ├── 02-vocabulary.md
│   ├── 03-topology-first.md
│   ├── 04-persistence.md
│   └── ...
│
├── commands/                    # Slash commands / workflows
│   ├── speckit.specify.md
│   ├── speckit.plan.md
│   └── ...
│
├── hooks/                       # Event-driven scripts (optional)
│   └── (hook scripts)
│
└── memory/
    └── reflections/             # Learnings from completed features
        ├── index.md             # Auto-maintained index
        └── (feature reflections)
```

### Generated (git-ignored)

```
# Tool-specific directories (all git-ignored)
.claude/skills/                  # Pointer stubs
.cline/skills/
.clinerules/                     # Rules + workflows/
.codex/skills/
.cursor/skills/
.cursor/rules/
.cursor/commands/
.gemini/skills/
.gemini/commands/
.kilocode/skills/
.opencode/skills/
.windsurf/skills/
.windsurf/rules/
.windsurf/workflows/

# Instruction files (git-ignored)
CLAUDE.md                        # Generated with @imports
AGENTS.md                        # Generated for Codex
GEMINI.md                        # Generated with @imports
```

---

## Configuration Types

### Skills

Skills are specialized capabilities that teach agents how to perform domain-specific tasks.

**Source:** `.agent/skills/<skill-name>/SKILL.md`

**Format:**

```yaml
---
name: skill-name
description: Brief explanation of what this skill does
---

# Skill Name

Detailed instructions for the agent...
```

**Required Fields:**

| Field | Description |
|-------|-------------|
| `name` | Lowercase identifier (letters, numbers, hyphens) |
| `description` | When to activate this skill (max 1024 chars) |

**Available Skills:**

| Skill | Purpose |
|-------|---------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration |
| `@spec-first` | Read specs before implementing |
| `@implement-feature` | Incremental feature implementation |
| `@self-critique` | Self-evaluation against specs and doctrine |
| `@code-review` | Architecture-focused code review |
| `@reflect` | Capture learnings after completion |
| `@persistence` | RocksDB + MessagePack patterns |
| `@topology-first` | Plates domain truth boundaries |

**Workflow:**

```
@spec-first → @implement-feature → @self-critique (loop) → @code-review → @reflect
```

---

### Rules

Rules provide persistent, reusable context at the prompt level—project guidelines, coding standards, and architectural constraints.

**Source:** `.agent/rules/<nn>-<name>.md`

**Format:**

```yaml
---
name: rule-name
description: What this rule covers
order: 1
---

# Rule Title

Rule content...
```

**Current Rules:**

| File | Purpose |
|------|---------|
| `01-project-overview.md` | Project basics and spec locations |
| `02-vocabulary.md` | Two vocabularies (governance vs pipeline) |
| `03-topology-first.md` | Topology-first doctrine for plates |
| `04-persistence.md` | RocksDB + MessagePack encoding |
| `05-graph-engine.md` | ModernSatsuma encapsulation |
| `06-core-expectations.md` | Incremental changes, commit often |
| `07-skills-workflow.md` | Skills architecture and usage |
| `08-spec-driven-dev.md` | GitHub Spec Kit integration |

**Sync Targets:**

| Target | Format |
|--------|--------|
| `.claude/rules/` | Individual pointer files |
| `.clinerules/` | Individual pointer files |
| `.cursor/rules/` | Individual pointer files |
| `.windsurf/rules/` | Individual pointer files |
| `CLAUDE.md` | Concatenated with `@imports` |
| `GEMINI.md` | Concatenated with `@imports` |
| `AGENTS.md` | Concatenated with inline pointers |

---

### Commands / Workflows

Commands are reusable prompts invoked via slash commands (e.g., `/speckit.specify`).

**Source:** `.agent/commands/<command-name>.md`

**Format:**

```yaml
---
description: What this command does
agent: build
handoffs:
  - label: Next Step
    agent: next-command
    prompt: Follow-up instruction
---

## User Input

$ARGUMENTS

## Instructions

Command instructions...
```

**Sync Targets:**

| Tool | Target | Format |
|------|--------|--------|
| Cline | `.clinerules/workflows/` | Markdown pointer |
| Cursor | `.cursor/commands/` | Markdown pointer |
| Gemini CLI | `.gemini/commands/` | TOML stub |
| Windsurf | `.windsurf/workflows/` | Markdown pointer |

**Invocation:**

| Tool | Syntax |
|------|--------|
| Cline | `/workflow-name.md` |
| Cursor | `/command-name` |
| Gemini CLI | `/command-name` |
| Windsurf | `/workflow-name` |

---

### Hooks

Hooks are scripts that trigger at specific events in the agent lifecycle.

**Source:** `.agent/hooks/<hook-script>`

**Supported Tools:**

| Tool | Location | Format |
|------|----------|--------|
| Cline | `.clinerules/hooks/` | Executable scripts |
| Cursor | `.cursor/hooks.json` | JSON configuration |
| Claude Code | `.claude/hooks/` | Executable scripts |

**Cline/Claude Code Hook Events:**

| Event | Trigger |
|-------|---------|
| `PreToolUse` | Before a tool executes |
| `PostToolUse` | After a tool completes |
| `UserPromptSubmit` | When user sends a message |
| `TaskStart` | When a task begins |
| `TaskCancel` | When a task is cancelled |

**Cursor Hook Events:**

| Event | Trigger |
|-------|---------|
| `beforeShellExecution` | Before running shell commands |
| `afterFileEdit` | After editing a file |
| `beforeReadFile` | Before reading a file |
| `sessionStart` | When a session begins |
| `stop` | When agent stops |

**Note:** Cursor hooks use JSON configuration (`hooks.json`) rather than script files, so they are not automatically synced and require manual setup.

---

## Generated Files

### CLAUDE.md / GEMINI.md (with @imports)

These files use the `@import` syntax supported by Claude Code and Gemini CLI:

```markdown
# Project Instructions

**This file is auto-generated. Edit sources in `.agent/` instead.**

Sources:
- Rules: `.agent/rules/`
- Reflections: `.agent/memory/reflections/`

## Rules

@.agent/rules/01-project-overview.md
@.agent/rules/02-vocabulary.md
...

## Learnings

@.agent/memory/reflections/feature-name.md
...
```

When the agent loads this file, it automatically imports and includes the referenced files.

### AGENTS.md (inline pointers)

Codex doesn't support `@imports`, so AGENTS.md uses inline pointers:

```markdown
# Project Instructions

## Rules

### Project Overview

Project basics and canonical spec locations

Read full rule: `.agent/rules/01-project-overview.md`

...
```

---

## Memory and Reflections

The reflection system captures learnings from completed features, creating "living documentation" that evolves with the project.

### How It Works

1. **Complete a feature** using `@implement-feature`
2. **Run `@self-critique`** to validate against specs
3. **Pass `@code-review`** and merge
4. **Invoke `@reflect`** to capture learnings

### Reflection Storage

**Location:** `.agent/memory/reflections/<feature-name>.md`

**Format:**

```markdown
# Reflection: feature-name

**Date:** 2026-01-24
**Spec:** `specs/<feature>/spec.md`
**Tags:** `plates`, `persistence`, `topology`

## Summary

One-paragraph description of what was built.

## What Worked Well

- **Pattern**: Why it worked

## Gotchas & Edge Cases

- **Issue**: How it was resolved

## Decisions Made

### Decision Title
- **Decision:** What was decided
- **Alternatives:** What was considered
- **Rationale:** Why this choice

## What I'd Do Differently

- Hindsight observation
```

### Index Auto-Maintenance

The `@reflect` skill auto-updates `.agent/memory/reflections/index.md`:

```markdown
# Reflections Index

| Feature | Date | Tags | Key Learning |
|---------|------|------|--------------|
| [plate-topology](./plate-topology.md) | 2026-01-24 | plates | RocksDB batch writes... |

## By Tag

### plates
- [plate-topology](./plate-topology.md)
```

### Feeding Back to Instructions

When `sync_skills.py` runs, it:

1. Reads all reflections from `.agent/memory/reflections/`
2. Extracts summaries
3. Includes them in CLAUDE.md, AGENTS.md, GEMINI.md

This means future agent sessions automatically see learnings from past work.

---

## Sync Script Usage

### Full Sync

```bash
python scripts/sync_skills.py
# or
task agent:sync
```

### Selective Sync

```bash
python scripts/sync_skills.py --skills   # Skills only
python scripts/sync_skills.py --rules    # Rules only
python scripts/sync_skills.py --commands # Commands/workflows only
python scripts/sync_skills.py --hooks    # Hooks only
```

### Pre-Commit Hook

The sync runs automatically on commit when files in `.agent/` change:

```yaml
# .pre-commit-config.yaml
- id: sync-agent
  name: Sync agent config
  entry: python scripts/sync_skills.py
  files: '^\.agent/(skills/.*SKILL\.md|rules/.*\.md|commands/.*\.md|hooks/.*)$'
```

---

## Adding New Configuration

### Adding a Skill

1. Create directory: `.agent/skills/<skill-name>/`
2. Create `SKILL.md` with frontmatter:

   ```yaml
   ---
   name: skill-name
   description: When to use this skill
   ---

   # Instructions...
   ```

3. Add supporting files if needed (checklists, templates, configs)
4. Update `.agent/skills/SKILLS-INDEX.md`
5. Run `python scripts/sync_skills.py --skills`

### Adding a Rule

1. Create `.agent/rules/<nn>-<name>.md`:

   ```yaml
   ---
   name: rule-name
   description: What this rule covers
   order: <nn>
   ---

   # Rule content...
   ```

2. Run `python scripts/sync_skills.py --rules`

### Adding a Command

1. Create `.agent/commands/<command-name>.md`:

   ```yaml
   ---
   description: What this command does
   ---

   ## Instructions...
   ```

2. Run `python scripts/sync_skills.py --commands`

### Adding a Hook (Cline)

1. Create executable script in `.agent/hooks/`
2. Script receives JSON via stdin, returns JSON via stdout
3. Run `python scripts/sync_skills.py --hooks`

---

## Tool-Specific Notes

### Claude Code

- Supports `@imports` in CLAUDE.md
- Hooks in `.claude/hooks/` (script-based)
- Rules in `.claude/rules/` with optional `paths:` frontmatter for file-specific rules

### Cline

- Rules in `.clinerules/` directory
- Workflows in `.clinerules/workflows/` (invoked via `/name.md`)
- Hooks in `.clinerules/hooks/` (script-based)
- Also supports `AGENTS.md` as fallback

### Codex

- Uses `AGENTS.md` for instructions (no @imports support)
- Skills in `.codex/skills/`
- No native hooks support

### Cursor

- Rules in `.cursor/rules/` (`.md` or `.mdc` format)
- Commands in `.cursor/commands/` (slash commands)
- Hooks via `.cursor/hooks.json` (JSON config, not synced)
- Deprecated `.cursorrules` still works but not recommended

### Gemini CLI

- Supports `@imports` in GEMINI.md
- Commands in `.gemini/commands/` (TOML format)
- Custom file names configurable in settings

### Windsurf

- Rules in `.windsurf/rules/`
- Workflows in `.windsurf/workflows/`
- Deprecated `.windsurfrules` not used

---

## Troubleshooting

### Sync Not Running

1. Check Python is available: `python --version`
2. Run manually: `python scripts/sync_skills.py`
3. Check for errors in output

### Changes Not Appearing

1. Ensure source files have correct frontmatter
2. Run sync with specific flag: `--skills`, `--rules`, etc.
3. Check generated files exist in tool directories

### Pre-Commit Hook Skipped

The hook only triggers when files matching the pattern are staged:
- `.agent/skills/**/SKILL.md`
- `.agent/rules/*.md`
- `.agent/commands/*.md`
- `.agent/hooks/*`

### Tool Not Seeing Instructions

1. Verify tool directory exists (e.g., `.claude/skills/`)
2. Check file permissions
3. Restart the tool/IDE
4. Some tools cache on startup—may need full restart

---

## References

- [Agent Skills Open Standard](https://github.com/agent-skills/spec)
- [Claude Code Memory](https://code.claude.com/docs/en/memory)
- [Cline Rules](https://docs.cline.bot/features/cline-rules)
- [Cursor Rules](https://cursor.com/docs/context/rules)
- [Gemini CLI Context](https://geminicli.com/docs/cli/gemini-md/)
- [Codex AGENTS.md](https://developers.openai.com/codex/guides/agents-md)
