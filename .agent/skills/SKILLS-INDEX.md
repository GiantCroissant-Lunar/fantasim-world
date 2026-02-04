# Skills Index

This directory contains shared skills for the lunar-horse-agent-hub, compatible with multiple AI coding assistants. Skills are structured guides that help agents handle complex, multi-step tasks correctly.

## Supported Tools

| Tool | Skills Location | Docs |
|------|-----------------|------|
| Claude Code | `.claude/skills/` | [Claude Code Skills](https://docs.anthropic.com/en/docs/claude-code/skills) |
| Cline | `.cline/skills/` | [Cline Skills](https://docs.cline.bot/features/skills) |
| Codex | `.codex/skills/` | [Codex Skills](https://developers.openai.com/codex/skills/) |
| Cursor | `.cursor/skills/` | [Cursor Skills](https://cursor.com/en-US/docs/context/skills) |
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

### Workflow Skills (10)

| Skill | Description | When to Use |
|-------|-------------|-------------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration | When starting or progressing a spec |
| `@spec-first` | Read specs before implementing | Before any implementation work |
| `@implement-feature` | Incremental feature implementation | When adding new functionality |
| `@self-critique` | Self-evaluation against specs and doctrine | After implementation, before marking complete |
| `@code-review` | Architecture-focused code review | When reviewing PRs or code |
| `@reflect` | Capture learnings after completion | After feature is merged |
| `@build` | Nuke build commands (auto-invoked) | When building, testing, or compiling |
| `@changelog-generator` | Create user-friendly changelogs from git commits | When preparing release notes |
| `@file-organizer` | Organize files and folders intelligently | When cleaning up directories |
| `@skill-creator` | Guide for creating new skills | When building new agent skills |

### Technical Skills (5)

| Skill | Description | When to Use |
|-------|-------------|-------------|
| `@code-analyze` | Static analysis, security scan, dependency check | When analyzing code quality |
| `@code-format` | Code formatting (dotnet, prettier) | When formatting code |
| `@dotnet-build` | .NET build and restore | When building .NET projects |
| `@dotnet-test` | .NET testing with coverage | When running tests |
| `@nuke-build` | Nuke build system targets | When using Nuke build system |

## Skill Invocation

### Automatic Invocation

Agents analyze task requests and automatically invoke matching skills based on their descriptions.

### Manual Invocation

Use `@skill-name` syntax to explicitly invoke a skill:

```
@spec-first - Read the relevant RFCs before I implement the feature
@code-review - Review this code for architecture compliance
```

## Skill Dependencies

```text
@spec-kit-bridge
        |
@spec-first
        |
@implement-feature
        |
@self-critique <--- (loop until pass)
        |
@code-review
        |
@reflect --> .agent/memory/reflections/
```

**Recommended workflow:**
1. Start with `@spec-kit-bridge` for new specs (creates worktree)
2. Use `@spec-first` during Specify/Clarify phases
3. Use `@implement-feature` during implementation
4. Run `@self-critique` to self-evaluate before requesting review
5. Finish with `@code-review`
6. Capture learnings with `@reflect` after merge

## Spec Kit Integration

This hub supports **GitHub Spec Kit** for spec-driven development.

### Interfaces

| Interface | When to Use |
|-----------|-------------|
| `specify` CLI | Preferred for local agents, automation |
| `/speckit.*` slash commands | Chat environments (GitHub Copilot, etc.) |

**Artifacts are the source of truth, not the interface used to create them.**

### Phase -> Skill Mapping

| Spec Kit Phase | Primary Skill | Output |
|----------------|---------------|--------|
| Constitution | -- | `.specify/memory/constitution.md` |
| Specify / Clarify | `@spec-first` | `specs/<feature>/spec.md` |
| Plan | `@implement-feature` | `specs/<feature>/plan.md` |
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

The skill will automatically be available to all supported tools via the sync scripts.

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
│   │   └── rubric.yaml
│   ├── code-review/
│   │   ├── SKILL.md
│   │   └── checklist.md
│   ├── reflect/
│   │   └── SKILL.md
│   ├── build/
│   │   └── SKILL.md
│   ├── changelog-generator/
│   │   └── SKILL.md
│   ├── file-organizer/
│   │   └── SKILL.md
│   ├── skill-creator/
│   │   ├── SKILL.md
│   │   ├── LICENSE.txt
│   │   └── scripts/
│   ├── code-analyze/            # Technical skills
│   │   ├── SKILL.md
│   │   ├── scripts/
│   │   └── references/
│   ├── code-format/
│   │   ├── SKILL.md
│   │   ├── scripts/
│   │   └── references/
│   ├── dotnet-build/
│   │   ├── SKILL.md
│   │   ├── scripts/
│   │   └── references/
│   ├── dotnet-test/
│   │   ├── SKILL.md
│   │   ├── scripts/
│   │   └── references/
│   └── nuke-build/
│       ├── SKILL.md
│       └── references/
├── memory/
│   └── reflections/
│       └── index.md
└── workflows/
    └── spec-kit-worktrees.md

scripts/
└── sync_skills.py               # Unified sync script
```
