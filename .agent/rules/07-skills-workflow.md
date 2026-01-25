---
name: skills-workflow
description: Agent skills system and workflow
order: 7
---

# Agent Workflow (Skills)

This repo uses a shared skills system compatible with multiple AI coding assistants.

## Source of Truth

All agent configuration lives in `.agent/`:

```text
.agent/
├── skills/             # Skill definitions
├── rules/              # Project rules
├── commands/           # Commands/workflows
├── hooks/              # Hook scripts
├── memory/             # Agent memory (reflections)
└── adapters/           # Tool-specific overrides
```

## Generated Outputs

Run `task agent:sync` to generate tool-specific files:

| Tool | Skills | Rules | Workflows |
|------|--------|-------|-----------|
| Claude | `.claude/skills/` | `CLAUDE.md` (@imports) | - |
| Cline | `.cline/skills/` | `.clinerules/*.md` | `.clinerules/workflows/` |
| Cursor | `.cursor/skills/` | `.cursor/rules/` | `.cursor/commands/` |
| OpenCode | `.opencode/skills/` | - | `.opencode/commands/` |
| Windsurf | `.windsurf/skills/` | `.windsurf/rules/` | `.windsurf/workflows/` |
| Kilocode | `.kilocode/skills/` | `.kilocode/rules/` | `.kilocode/workflows/` |

**Note:** Each tool has an adapter config in `.agent/adapters/<tool>/config.yaml`.

## Adapters

Tool-specific overrides live in `.agent/adapters/<tool>/`:

- **OpenCode**: Agent definitions, permissions (`opencode.json` generation)
- **Cline**: Documents unique directory structure
- **Kilocode**: Workflow overrides

## Sync Commands

```bash
task agent:sync           # Sync everything
task agent:sync-skills    # Skills only
task agent:sync-rules     # Rules only
task agent:sync-opencode  # OpenCode (generates opencode.json)
```

## Available Skills

See `.agent/skills/SKILLS-INDEX.md` for full documentation.

| Skill | Description |
|-------|-------------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration |
| `@spec-first` | Read and understand specs before implementing |
| `@implement-feature` | Incremental feature implementation with stable contracts |
| `@code-review` | Architecture-focused code review |
| `@reflect` | Capture learnings after feature completion |
| `@self-critique` | Self-evaluate implementation against specs |

**Note:** Domain-specific guidance (topology-first, persistence) lives in rules, not skills.
