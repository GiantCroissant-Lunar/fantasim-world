# Skills Index

This directory contains shared skills for the fantasim-world project, compatible with multiple AI coding assistants (Windsurf Cascade, Cline, etc.). Skills are structured guides that help agents handle complex, multi-step tasks correctly.

## Supported Tools

| Tool | Skills Location | Docs |
|------|-----------------|------|
| Claude Code | `.claude/skills/` | [Claude Code Skills](https://docs.anthropic.com/en/docs/claude-code/skills) |
| Windsurf | `.windsurf/skills/` | [Cascade Skills](https://docs.windsurf.com/windsurf/cascade/skills) |
| Cline | `.cline/skills/` | [Cline Skills](https://docs.cline.bot/features/skills) |

Each tool has stub SKILL.md files that reference the full definitions in `.agent/skills/`.

**Note**: Tool-specific directories (`.claude/`, `.windsurf/`, `.cline/`) are git-ignored.

**After cloning or pulling**, run:
```bash
task sync-skills
```

## Available Skills

| Skill | Description | When to Use |
|-------|-------------|-------------|
| `@spec-first` | Read specs before implementing | Before any implementation work |
| `@implement-feature` | Incremental feature implementation | When adding new functionality |
| `@code-review` | Architecture-focused code review | When reviewing PRs or code |
| `@persistence` | RocksDB + MessagePack patterns | When working with storage |
| `@topology-first` | Plates domain truth boundaries | When working on plates code |

## Skill Invocation

### Automatic Invocation

Agents analyze task requests and automatically invoke matching skills based on their descriptions.

### Manual Invocation

Use `@skill-name` syntax to explicitly invoke a skill:

```
@spec-first - Read the relevant RFCs before I implement the plate merger
@topology-first - Review this plates code for truth boundary violations
```

## Skill Dependencies

```
@spec-first
    ↓
@implement-feature ←→ @topology-first
    ↓                      ↓
@persistence          @persistence
    ↓                      ↓
@code-review ←─────────────┘
```

**Recommended workflow:**
1. Always start with `@spec-first`
2. Use domain skills (`@topology-first`) during implementation
3. Use `@persistence` when adding storage
4. Finish with `@code-review`

## Adding New Skills

1. Create a new directory: `.agent/skills/<skill-name>/`
2. Add `SKILL.md` with required frontmatter:
   ```yaml
   ---
   name: skill-name
   description: Brief description for agent matching (max 1024 chars)
   ---
   ```
3. Add supporting files (templates, checklists, scripts)
4. Update this index

The skill will automatically be available to all supported tools via the junctions.

## Skill Structure

```
.agent/skills/               # Shared source of truth
├── SKILLS-INDEX.md          # This file
├── spec-first/
│   ├── SKILL.md
│   └── checklist.md
├── implement-feature/
│   └── SKILL.md
├── code-review/
│   ├── SKILL.md
│   └── checklist.md
├── persistence/
│   └── SKILL.md
└── topology-first/
    └── SKILL.md

.claude/skills/              # Stub files → .agent/skills
.windsurf/skills/            # Stub files → .agent/skills
.cline/skills/               # Stub files → .agent/skills

scripts/
└── sync_skills.py           # Cross-platform sync script
```
