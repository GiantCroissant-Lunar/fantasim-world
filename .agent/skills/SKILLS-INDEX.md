# Skills Index

This directory contains shared skills for the fantasim-world project, compatible with multiple AI coding assistants (Windsurf Cascade, Cline, etc.). Skills are structured guides that help agents handle complex, multi-step tasks correctly.

## Supported Tools

| Tool | Skills Location | Docs |
|------|-----------------|------|
| Claude Code | `.claude/skills/` | [Claude Code Skills](https://docs.anthropic.com/en/docs/claude-code/skills) |
| Cline | `.cline/skills/` | [Cline Skills](https://docs.cline.bot/features/skills) |
| Gemini CLI | `.gemini/skills/` | [Gemini CLI Skills](https://geminicli.com/docs/cli/skills/) |
| OpenCode | `.opencode/skills/` | [OpenCode Skills](https://opencode.ai/docs/skills/) |
| Windsurf | `.windsurf/skills/` | [Cascade Skills](https://docs.windsurf.com/windsurf/cascade/skills) |

Each tool has stub SKILL.md files that reference the full definitions in `.agent/skills/`.

**Note**: Tool-specific directories (`.claude/`, `.windsurf/`, `.cline/`) are git-ignored.

**After cloning or pulling**, run:
```bash
task sync-skills
```

## Available Skills

| Skill | Description | When to Use |
|-------|-------------|-------------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration | When starting or progressing a spec |
| `@spec-first` | Read specs before implementing | Before any implementation work |
| `@implement-feature` | Incremental feature implementation | When adding new functionality |
| `@self-critique` | Self-evaluation against specs and doctrine | After implementation, before marking complete |
| `@code-review` | Architecture-focused code review | When reviewing PRs or code |
| `@reflect` | Capture learnings after completion | After feature is merged |
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

```text
@spec-kit-bridge
        ↓
@spec-first
        ↓
@implement-feature ←→ @topology-first
        ↓                      ↓
@persistence          @persistence
        ↓                      ↓
@self-critique ←───────────────┘
        ↓
        ↓ (loop until pass)
        ↓
@code-review
        ↓
@reflect → .agent/memory/reflections/
```

**Recommended workflow:**
1. Start with `@spec-kit-bridge` for new specs (creates worktree)
2. Use `@spec-first` during Specify/Clarify phases
3. Use domain skills (`@topology-first`, `@persistence`) during Plan/Implement
4. Run `@self-critique` to self-evaluate before requesting review
5. Finish with `@code-review`
6. Capture learnings with `@reflect` after merge

## Spec Kit Integration

This project uses **GitHub Spec Kit** for spec-driven development.

### Interfaces

Spec Kit provides two interfaces (both produce the same artifacts):

| Interface | When to Use |
|-----------|-------------|
| `specify` CLI | Preferred for local agents, automation |
| `/speckit.*` slash commands | Chat environments (GitHub Copilot, etc.) |

**Artifacts are the source of truth, not the interface used to create them.**

### Phase → Skill Mapping

| Spec Kit Phase | Primary Skill | Output |
|----------------|---------------|--------|
| Constitution | — | `.specify/memory/constitution.md` |
| Specify / Clarify | `@spec-first` | `specs/<feature>/spec.md` |
| Plan | `@topology-first`, `@persistence` | `specs/<feature>/plan.md` |
| Tasks | `@spec-kit-bridge` | `specs/<feature>/tasks.md` |
| Implement | `@implement-feature` | code + tests |
| Self-Critique | `@self-critique` | critique report (iterates until pass) |
| Review | `@code-review` | PR approval |
| Reflect | `@reflect` | `.agent/memory/reflections/<feature>.md` |

### Worktree Rule

**One spec = one feature branch = one git worktree**

Spec work is never done directly on `main`. See `.agent/workflows/spec-kit-worktrees.md` for details.

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

```text
.agent/
├── skills/                      # Shared source of truth
│   ├── SKILLS-INDEX.md          # This file
│   ├── spec-kit-bridge/         # Workflow orchestration
│   │   ├── SKILL.md
│   │   ├── phase-mapping.md
│   │   └── worktree-playbook.md
│   ├── spec-first/
│   │   ├── SKILL.md
│   │   └── checklist.md
│   ├── implement-feature/
│   │   └── SKILL.md
│   ├── self-critique/           # Self-evaluation loop
│   │   ├── SKILL.md
│   │   └── rubric.yaml          # Configurable evaluation criteria
│   ├── code-review/
│   │   ├── SKILL.md
│   │   └── checklist.md
│   ├── reflect/                 # Learning capture
│   │   └── SKILL.md
│   ├── persistence/
│   │   └── SKILL.md
│   └── topology-first/
│       └── SKILL.md
├── memory/                      # Agent memory storage
│   └── reflections/             # Feature learnings
│       └── index.md             # Auto-maintained index
└── workflows/                   # Reference docs (not skills)
    └── spec-kit-worktrees.md

.claude/skills/                  # Stub files → .agent/skills
.cline/skills/                   # Stub files → .agent/skills
.gemini/skills/                  # Stub files → .agent/skills
.kilocode/skills/                # Stub files → .agent/skills
.opencode/skills/                # Stub files → .agent/skills
.windsurf/skills/                # Stub files → .agent/skills

scripts/
└── sync_skills.py               # Cross-platform sync script
```
