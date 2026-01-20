---
name: spec-kit-bridge
description: Orchestrates spec-driven development using GitHub Spec Kit. Enforces one-feature-per-worktree, maps spec phases to domain skills, and ensures specs are structured for parallel agents.
---

# Spec Kit Bridge

## Purpose

This skill is **procedural glue** between GitHub Spec Kit and this project's domain skills. It does NOT replace spec-kit or contain domain logic—it coordinates the workflow.

## What This Skill Does

- Detects/clarifies current phase (Specify → Plan → Tasks → Implement)
- Explains which spec-kit interface to use (CLI vs slash commands)
- Routes agent to existing domain skills (`@spec-first`, `@topology-first`, etc.)
- Enforces the worktree + branch invariant
- Enforces task granularity rules for parallel agents

## What This Skill Does NOT Do

- Write specs for you (that's `@spec-first`)
- Replace spec-kit commands
- Contain domain logic (that's `@topology-first`, `@persistence`, etc.)

## Core Invariant

**One spec = one feature branch = one git worktree**

This is non-negotiable. Spec work is never done directly on `main`.

## Spec Kit Interfaces

Agents may use either interface to produce the same artifacts:

| Interface | When to Use |
|-----------|-------------|
| `specify` CLI | Preferred for local agents, automation, terminal-driven work |
| `/speckit.*` slash commands | When in chat environments that support them (GitHub Copilot, etc.) |

**Artifacts are the source of truth, not the interface used to create them.**

## Phase Workflow

See [phase-mapping.md](./phase-mapping.md) for detailed phase → skill routing.

### Quick Reference

```text
1. Constitution  → Shared, rarely edited (separate PR if changed)
2. Specify       → @spec-first → specs/<feature>/spec.md
3. Clarify       → @spec-first → refine spec.md
4. Plan          → @topology-first / @persistence → specs/<feature>/plan.md
5. Tasks         → @spec-kit-bridge → specs/<feature>/tasks.md
6. Implement     → @implement-feature + domain skills → code + tests
7. Review        → @code-review → PR approval
```

## Starting a New Spec

```bash
# 1. Create worktree + branch
task spec:new plate-merger

# 2. Enter the worktree
cd ../fantasim-world--plate-merger

# 3. Run spec-kit (choose one):
#    CLI:
task spec:specify FEATURE=plate-merger
#    Or slash command:
#    /speckit.specify

# 4. Continue through phases...
task spec:plan FEATURE=plate-merger
task spec:tasks FEATURE=plate-merger

# 5. Implement tasks, commit with task IDs

# 6. When done, merge and cleanup
task spec:done plate-merger
```

## Worktree Layout

See [worktree-playbook.md](./worktree-playbook.md) for full details.

```text
fantasim-world/                      # Main worktree
├── .specify/
│   └── memory/constitution.md       # Shared constitution
└── specs/
    └── completed/                   # Merged specs (archive)

../fantasim-world--<feature>/        # Feature worktree
├── .specify/                        # Inherited from branch
└── specs/<feature>/
    ├── spec.md                      # Requirements
    ├── plan.md                      # Technical plan
    └── tasks.md                     # Task list
```

## Task Granularity Rules

When generating tasks (`/speckit.tasks` or `specify tasks`):

1. **Agent-separable**: Each task must be independently completable
2. **Testable**: Each task should have clear success criteria
3. **Minimal coupling**: Avoid cross-task dependencies where possible
4. **Traceable**: Tasks reference spec sections; commits reference task IDs

## Slice Tags

During Specify phase, specs should include slice tags:

```markdown
## Slice Tags
- Geosphere.Topology
- Persistence
```

This enables automatic routing to the correct domain skills during Plan/Implement.

## Related Skills

| Skill | When to Use |
|-------|-------------|
| `@spec-first` | During Specify/Clarify phases |
| `@topology-first` | When spec involves plates/boundaries |
| `@persistence` | When spec involves storage/events |
| `@implement-feature` | During Implement phase |
| `@code-review` | During Review phase |
