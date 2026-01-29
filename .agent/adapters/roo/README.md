# Roo Code Adapter

Roo Code (formerly Roo Cline) is a VS Code extension for AI-assisted coding.

## Directory Structure

Roo Code uses a `.roo/` directory in the workspace root:

```text
.roo/
├── skills/                    # Skills (on-demand loading)
│   └── {skill-name}/
│       └── SKILL.md           # Required filename
├── rules/                     # Custom instructions (always loaded)
│   └── *.md                   # Markdown files, read alphabetically
└── commands/                  # Slash commands
    └── *.md                   # Filename becomes command name
```

## Skills

Roo uses **progressive disclosure** for skills:

1. **Discovery**: Only frontmatter (name/description) loaded at startup
2. **Instructions**: Full content loaded when request matches description
3. **Resources**: Bundled files accessed on-demand

### SKILL.md Format

```markdown
---
name: skill-name
description: Specific description (1-1024 chars)
---
# Detailed instructions follow
```

**Naming rules:**
- 1-64 characters, lowercase alphanumeric + hyphens only
- No leading/trailing/consecutive hyphens
- Must match directory name exactly

## Rules (Custom Instructions)

Rules are loaded alphabetically from `.roo/rules/`:

- Use numbered prefixes for ordering: `01-overview.md`, `02-patterns.md`
- Rules apply globally across all modes
- Mode-specific rules: `.roo/rules-{modeSlug}/`

## Slash Commands

Commands in `.roo/commands/` become available via `/command-name`:

```markdown
---
description: What this command does
argument-hint: <optional input format>
mode: code  # Optional: switch to this mode first
---
# Command content
```

## Mode-Specific Content

Roo supports mode-specific skills and rules:

- `.roo/skills-code/` - Code mode only
- `.roo/skills-architect/` - Architect mode only
- `.roo/rules-code/` - Rules for code mode
- `.roo/rules-architect/` - Rules for architect mode

## Override Priority

1. Project mode-specific (`.roo/skills-code/my-skill/`)
2. Project generic (`.roo/skills/my-skill/`)
3. Global mode-specific (`~/.roo/skills-code/my-skill/`)
4. Global generic (`~/.roo/skills/my-skill/`)

## Documentation

- Skills: https://docs.roocode.com/features/skills
- Custom Instructions: https://docs.roocode.com/features/custom-instructions
- Slash Commands: https://docs.roocode.com/features/slash-commands

## Sync Commands

```bash
task agent:sync           # Sync all (includes Roo)
```

## Differences from Other Tools

| Feature | Roo Code | Cline | Claude Code |
|---------|----------|-------|-------------|
| Skills location | `.roo/skills/` | `.cline/skills/` | `.claude/skills/` |
| Skill file | `SKILL.md` (required) | `SKILL.md` | Any `.md` |
| Rules location | `.roo/rules/` | `.clinerules/` | `CLAUDE.md` |
| Commands | `.roo/commands/` | `.clinerules/workflows/` | N/A |
| Mode-specific | Yes (`-{modeSlug}`) | No | No |
