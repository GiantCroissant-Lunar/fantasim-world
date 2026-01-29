# Kilo Code Adapter

This directory contains Kilo Code-specific overrides and additions.

## Structure

```
kilocode/
├── README.md          # This file
├── config.yaml        # Adapter sync configuration
├── mcp.json           # MCP server configuration
└── workflows/         # Kilo-specific workflow overrides
```

## MCP Configuration

The `mcp.json` file defines Model Context Protocol servers for Kilo Code. Copy or symlink it to `.kilocode/mcp.json` for Kilo Code to use.

### Configured Servers

| Server | Purpose |
|--------|---------|
| `microsoft-learn` | Microsoft Learn documentation API |
| `filesystem` | File system access (parent directory) |
| `git` | Git operations across all Plate stack repos |
| `playwright` | Browser automation |

### Git Multi-Repo Support

The git MCP server is configured with multiple repositories for the full Plate stack:
- Current repo (`.`)
- fantasim-hub, plate-shared, time-dete
- plugin-archi, service-archi
- unify-* libraries (ecs, maths, topology, serialization, storage, build)

### Path Configuration

Paths use relative references:
- `.` = current project root (fantasim-world)
- `..` = parent directory (plate-projects)
- `../repo-name` = sibling repositories

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
