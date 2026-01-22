---
name: skills-workflow
description: Agent skills system and workflow
order: 7
---

# Agent Workflow (Skills)

This repo uses a shared skills system compatible with multiple AI coding assistants.

## Skills Architecture

Skills are stored in a shared location with tool-specific stub files:

```text
.agent/skills/          # Source of truth (tracked in git)
.claude/skills/         # Generated stubs
.cline/skills/          # Generated stubs
.kilocode/skills/       # Generated stubs
.opencode/skills/       # Generated stubs
.windsurf/skills/       # Generated stubs
```

**After cloning or pulling**, run:

```bash
task sync-skills
```

This regenerates the tool-specific stubs from `.agent/skills/`.

## Available Skills

See `.agent/skills/SKILLS-INDEX.md` for full documentation.

| Skill | Description |
|-------|-------------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration |
| `@spec-first` | Read and understand specs before implementing |
| `@implement-feature` | Incremental feature implementation with stable contracts |
| `@code-review` | Architecture-focused code review |
| `@persistence` | RocksDB + MessagePack patterns |
| `@topology-first` | Plates domain truth boundary enforcement |
