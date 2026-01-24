# Cline Adapter

Cline has a unique directory structure that differs from other tools.

## Directory Structure

Unlike other tools that use a single `.toolname/` directory, Cline uses two:

```text
.cline/                    # Skills only
└── skills/                # Skill definitions

.clinerules/               # Rules, workflows, hooks
├── *.md                   # Rules (at root, not in subdirectory)
├── workflows/             # Workflow definitions
└── hooks/                 # Hook scripts
```

## Why Two Directories?

Cline evolved from different features:

- **`.clinerules/`** - Original rules system (like `.cursorrules`)
- **`.cline/`** - Newer skills system (recommended for skills)

Both are supported and serve different purposes.

## What Goes Where

| Content | Location | Notes |
|---------|----------|-------|
| Skills | `.cline/skills/` | Recommended location per Cline docs |
| Rules | `.clinerules/*.md` | Files at root, not in subdirectory |
| Workflows | `.clinerules/workflows/` | Workflow stubs |
| Hooks | `.clinerules/hooks/` | Hook scripts |

## Cline Documentation

- Skills: https://docs.cline.bot/features/skills
- Rules: https://docs.cline.bot/features/cline-rules
- Workflows: https://docs.cline.bot/features/workflows

## Sync Commands

```bash
task agent:sync           # Sync all (includes Cline)
python scripts/sync_skills.py  # Direct script
```

## How Sync Works

The `sync_skills.py` script handles Cline's unique structure:

1. Skills → `.cline/skills/` (not `.clinerules/skills/`)
2. Rules → `.clinerules/*.md` (at root, not in subdirectory)
3. Workflows → `.clinerules/workflows/`
4. Hooks → `.clinerules/hooks/`
