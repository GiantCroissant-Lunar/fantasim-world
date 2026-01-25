# Claude Code Adapter

Configuration for syncing `.agent/` content to Claude Code's expected locations.

## How Claude Code Loads Content

Claude Code has fixed paths it reads from:

| Content Type | Location | Auto-discovered |
|--------------|----------|-----------------|
| Project memory | `CLAUDE.md` | Yes |
| Rules | `.claude/rules/*.md` | Yes |
| Skills | `.claude/skills/*/SKILL.md` | Yes (for `/skill-name` commands) |

## Our Strategy

### Rules: Use `@` Imports (No Duplication)

Instead of copying rules to `.claude/rules/`, we use `@` imports in `CLAUDE.md`:

```markdown
## Rules

@.agent/rules/01-project-overview.md
@.agent/rules/02-vocabulary.md
```

**Benefits:**
- Single source of truth (`.agent/rules/`)
- No duplicate files consuming context
- Claude Code resolves imports automatically

**Config:**
```yaml
rules:
  strategy: import
  target: CLAUDE.md
  directory_target: null  # Skip .claude/rules/
```

### Skills: Copy Full Content

Skills must be in `.claude/skills/` for `/skill-name` slash commands to work.
We copy the full content (not stubs) to avoid wasting context on pointers.

**Config:**
```yaml
skills:
  strategy: copy_full
  target: .claude/skills
```

## Directory Structure

After sync:

```text
CLAUDE.md                     # Generated with @imports to .agent/rules/
.claude/
└── skills/                   # Full skill content copied here
    ├── spec-kit-bridge/
    │   └── SKILL.md          # Full content, not a stub
    └── ...

.agent/                       # Source of truth (not modified by sync)
├── rules/
├── skills/
└── adapters/claude/
    ├── config.yaml           # This adapter's configuration
    └── README.md             # This file
```

## Sync Commands

```bash
task agent:sync           # Sync all tools
task agent:sync-claude    # Sync Claude Code only (if available)
```

## Cleanup

The sync script will remove legacy paths listed in `config.yaml`:

```yaml
cleanup:
  - .claude/rules  # Not needed when using import strategy
```

## Cross-Platform Compatibility

This adapter avoids symlinks for cross-platform compatibility:
- Windows: No admin/Developer Mode required
- Linux/macOS: Works identically
- Git: No symlink handling issues
