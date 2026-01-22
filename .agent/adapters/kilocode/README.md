# Kilo Code Adapter

This directory contains Kilo Code-specific overrides and additions.

## Structure

```
kilocode/
├── README.md          # This file
├── workflows/         # Kilo-specific workflow overrides
└── (future)           # Additional Kilo-specific content
```

## How Adapters Work

1. **Skills**: Synced from `.agent/skills/` to `.kilocode/skills/` (stubs referencing source)
2. **Workflows**: Synced from `.agent/workflows/` to `.kilocode/workflows/`
   - If a file exists in `kilocode/workflows/`, it overrides the canonical version

## Kilo Code Documentation

- Skills: https://kilo.ai/docs/agent-behavior/skills
- Workflows: https://kilo.ai/docs/agent-behavior/workflows

## Sync Commands

```bash
task sync-skills      # Sync skills to all tools including .kilocode
task sync-workflows   # Sync workflows to .kilocode
task sync-kilocode    # Full kilocode sync (skills + workflows)
```
