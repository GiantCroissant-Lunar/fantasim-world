# Agent Adapters

This directory contains tool-specific configurations for syncing `.agent/` content to each AI coding assistant.

## Structure

```
adapters/
├── README.md           # This file
├── claude/
│   ├── config.yaml     # Sync configuration
│   └── README.md       # Tool-specific docs
├── cline/
│   ├── config.yaml
│   └── README.md
├── cursor/
│   └── config.yaml
├── gemini/
│   └── config.yaml
├── kilocode/
│   ├── config.yaml
│   └── README.md
├── opencode/
│   ├── config.yaml
│   ├── agents/         # OpenCode agent definitions
│   └── permissions.yaml
├── windsurf/
│   └── config.yaml
├── codex/
│   └── config.yaml
└── roo/
    ├── config.yaml
    └── README.md
```

## Config Schema

Each adapter has a `config.yaml` that defines how to sync content:

```yaml
tool: <tool-name>
display_name: <Human Readable Name>

rules:
  strategy: import | directory | concatenate | none
  target: <path>              # Target file or directory
  directory_target: <path>    # Secondary directory (optional)
  extension: .md | .mdc       # File extension (default: .md)

skills:
  strategy: copy_full | stub
  target: <path>
  source: .agent/skills

commands:
  enabled: true | false
  target: <path>
  format: md                   # Output format

workflows:
  enabled: true | false
  target: <path>

hooks:
  enabled: true | false
  target: <path>

cleanup:
  - <path-to-remove>
  - <another-path>
```

## Strategies

### Rules Strategies

| Strategy | Description |
|----------|-------------|
| `import` | Use `@` imports in a single file (CLAUDE.md, GEMINI.md) |
| `directory` | Create stub files in a directory (.cursor/rules/) |
| `concatenate` | Combine rules into single file without imports |
| `none` | Skip rules sync (tool uses different mechanism) |

### Skills Strategies

| Strategy | Description |
|----------|-------------|
| `copy_full` | Copy complete skill content (saves context tokens) |
| `stub` | Create pointer files that reference source |

## Adding a New Tool

1. Create `adapters/<tool>/config.yaml`
2. Define strategies for rules, skills, commands, etc.
3. Optionally add `README.md` for tool-specific quirks
4. Run `task agent:sync` to test

## Sync Commands

```bash
task agent:sync           # Sync all tools
python scripts/sync_skills.py --skills   # Skills only
python scripts/sync_skills.py --rules    # Rules only
python scripts/sync_skills.py --cleanup  # Run cleanups only
```

## Tool-Specific Notes

### Claude

- Uses `@` imports (most efficient)
- Full skill content (no stubs)
- No `.claude/rules/` needed

### Cline

- Unique structure: `.cline/` for skills, `.clinerules/` for rules
- Rules at root of `.clinerules/`, not in subdirectory

### Cursor

- Uses `.mdc` extension for rules
- Hooks via JSON config (not file-based)

### OpenCode

- Uses `opencode.json` for configuration
- Has separate agents and permissions files

### Gemini

- Supports `@` imports like Claude
- Commands in MD format

### Roo

- Uses `.roo/` directory structure
- Skills require `SKILL.md` filename with name/description frontmatter
- Supports mode-specific content via `-{modeSlug}` suffix directories
- Commands become slash commands (filename = command name)
