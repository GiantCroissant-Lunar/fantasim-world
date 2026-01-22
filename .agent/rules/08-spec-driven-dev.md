---
name: spec-driven-dev
description: Spec-driven development with GitHub Spec Kit
order: 8
---

# Spec-Driven Development

This repo uses **GitHub Spec Kit** for spec-driven development.

## Core Invariant

**One spec = one feature branch = one git worktree**

Spec work is never done directly on `main`.

## Interfaces

Agents may use either interface (both produce the same artifacts):

| Interface | When to Use |
|-----------|-------------|
| `specify` CLI | Preferred for local agents, automation |
| `/speckit.*` slash commands | Chat environments (GitHub Copilot, etc.) |

## Quick Start

```bash
# 1. Create worktree for new spec
task spec:new -- plate-merger

# 2. Enter worktree
cd ../fantasim-world--plate-merger

# 3. Run spec phases
task spec:specify FEATURE=plate-merger   # or /speckit.specify
task spec:plan FEATURE=plate-merger      # or /speckit.plan
task spec:tasks FEATURE=plate-merger     # or /speckit.tasks

# 4. Implement tasks, commit with task IDs

# 5. Create PR, merge, cleanup
task spec:done -- plate-merger
```

## Phase → Skill Mapping

| Spec Kit Phase | Primary Skill |
|----------------|---------------|
| Constitution | (rarely edited) |
| Specify / Clarify | `@spec-first` |
| Plan | `@topology-first`, `@persistence` |
| Tasks | `@spec-kit-bridge` |
| Implement | `@implement-feature` |
| Review | `@code-review` |

See `.agent/skills/spec-kit-bridge/` for detailed phase mapping and worktree playbook.
