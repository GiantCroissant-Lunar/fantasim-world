# Spec Kit + Worktrees Workflow

**This is reference material, not a skill.** For skill guidance, see `@spec-kit-bridge`.

## Quick Start

```bash
# 1. Start new spec
task spec:new my-feature

# 2. Enter worktree
cd ../fantasim-world--my-feature

# 3. Run spec phases (CLI or slash commands)
task spec:specify FEATURE=my-feature   # or /speckit.specify
task spec:plan FEATURE=my-feature      # or /speckit.plan
task spec:tasks FEATURE=my-feature     # or /speckit.tasks

# 4. Implement (follow task list)
# ... make commits referencing task IDs ...

# 5. Create PR, get review, merge

# 6. Cleanup
task spec:done my-feature
```

## Available Task Commands

| Command | Description |
|---------|-------------|
| `task spec:new <name>` | Create branch + worktree for new spec |
| `task spec:list` | List active spec worktrees |
| `task spec:specify FEATURE=<name>` | Run specify phase |
| `task spec:plan FEATURE=<name>` | Run plan phase |
| `task spec:tasks FEATURE=<name>` | Run tasks phase |
| `task spec:done <name>` | Cleanup worktree after merge |

## Spec Kit CLI vs Slash Commands

Both interfaces produce identical artifacts:

| Action | CLI | Slash Command |
|--------|-----|---------------|
| Define requirements | `specify specify` | `/speckit.specify` |
| Clarify ambiguities | `specify clarify` | `/speckit.clarify` |
| Create plan | `specify plan` | `/speckit.plan` |
| Generate tasks | `specify tasks` | `/speckit.tasks` |
| Analyze consistency | `specify analyze` | `/speckit.analyze` |
| Execute implementation | `specify implement` | `/speckit.implement` |
| Quality checklist | `specify checklist` | `/speckit.checklist` |

**Rule: Artifacts are truth. Interface is interchangeable.**

## File Locations

```text
.specify/
└── memory/
    └── constitution.md      # Project principles (shared)

specs/<feature>/
├── spec.md                  # Requirements (Specify phase)
├── plan.md                  # Technical plan (Plan phase)
└── tasks.md                 # Task list (Tasks phase)
```

## Commit Message Convention

Reference task IDs in commits:

```text
feat(plates): implement plate merger algorithm

Task: 2.3
Spec: plate-merger
Refs: RFC-V2-0001

- Add PlateMerger class
- Implement boundary consolidation
- Add unit tests
```

## PR Description Template

```markdown
## Spec: <feature-name>

### Summary
Brief description of what this spec implements.

### Tasks Completed
- [x] Task 1.1: Define interfaces
- [x] Task 1.2: Implement core logic
- [x] Task 2.1: Add tests

### Spec Artifacts
- `specs/<feature>/spec.md` - Requirements
- `specs/<feature>/plan.md` - Technical plan
- `specs/<feature>/tasks.md` - Task breakdown

### RFC Alignment
- RFC-V2-XXXX: <title>
```

## Parallel Work Example

```text
Terminal 1 (Agent A):
  cd ../fantasim-world--plate-merger
  /speckit.implement

Terminal 2 (Agent B):
  cd ../fantasim-world--event-sourcing
  /speckit.plan

Terminal 3 (Agent C):
  cd ../fantasim-world
  # Reviewing PRs, updating docs
```

No conflicts. Each agent has isolated state.

## See Also

- `.agent/skills/spec-kit-bridge/SKILL.md` - Orchestration skill
- `.agent/skills/spec-kit-bridge/phase-mapping.md` - Phase details
- `.agent/skills/spec-kit-bridge/worktree-playbook.md` - Worktree details
- `AGENTS.md` - Project guidelines
