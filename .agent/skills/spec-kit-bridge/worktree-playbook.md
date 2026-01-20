# Worktree Playbook

This document defines the git worktree conventions for parallel spec development.

## Core Invariant

**One spec = one feature branch = one git worktree**

This prevents:
- Agents clobbering each other's branch state
- Lock file conflicts
- Race conditions on shared files

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Feature name | `kebab-case` | `plate-merger` |
| Branch | `spec/<feature>` | `spec/plate-merger` |
| Worktree directory | `../<repo>--<feature>` | `../fantasim-world--plate-merger` |
| Spec folder | `specs/<feature>/` | `specs/plate-merger/` |

## Directory Layout

```text
D:\lunar-snake\personal-work\plate-projects\
├── fantasim-world/                    # Main worktree (main branch)
│   ├── .specify/
│   │   └── memory/
│   │       └── constitution.md        # Shared constitution
│   ├── specs/
│   │   └── completed/                 # Archive of merged specs
│   ├── .agent/
│   │   └── skills/
│   └── ...
│
├── fantasim-world--plate-merger/      # Feature worktree
│   ├── .specify/                      # Inherited (tracked in git)
│   ├── specs/
│   │   └── plate-merger/
│   │       ├── spec.md
│   │       ├── plan.md
│   │       └── tasks.md
│   └── ...
│
└── fantasim-world--persistence-v2/    # Another feature worktree
    └── ...
```

## Workflow Commands

### Create New Spec Worktree

```bash
# Using Taskfile (recommended)
task spec:new plate-merger

# Manual equivalent
git worktree add ../fantasim-world--plate-merger -b spec/plate-merger
cd ../fantasim-world--plate-merger
mkdir -p specs/plate-merger
```

### List Active Worktrees

```bash
# Using Taskfile
task spec:list

# Manual equivalent
git worktree list
```

### Enter Worktree

```bash
cd ../fantasim-world--plate-merger
```

### Complete and Cleanup

```bash
# Using Taskfile (recommended)
task spec:done plate-merger

# Manual steps:
# 1. Ensure all changes committed and pushed
# 2. Create PR, get review, merge
# 3. Return to main worktree
cd ../fantasim-world
git checkout main
git pull
# 4. Remove worktree
git worktree remove ../fantasim-world--plate-merger
# 5. Optionally delete remote branch
git push origin --delete spec/plate-merger
```

## Worktree Lifecycle

```text
┌─────────────────────────────────────────────────────────────┐
│                        MAIN WORKTREE                        │
│                     (fantasim-world/)                       │
│                                                             │
│  Constitution lives here. Completed specs archived here.    │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ task spec:new <feature>
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     FEATURE WORKTREE                        │
│              (fantasim-world--<feature>/)                   │
│                                                             │
│  1. /speckit.specify → spec.md                              │
│  2. /speckit.plan    → plan.md                              │
│  3. /speckit.tasks   → tasks.md                             │
│  4. Implement tasks  → code + tests                         │
│  5. Create PR                                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ PR merged + task spec:done
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        MAIN WORKTREE                        │
│                                                             │
│  Feature merged. Worktree removed. Spec in completed/.      │
└─────────────────────────────────────────────────────────────┘
```

## Constitution Sharing

The constitution file (`.specify/memory/constitution.md`) is:
- Tracked in git
- Inherited by all worktrees automatically
- Rarely edited (changes require dedicated PR to main)

No symlinks needed. Git handles this naturally.

## Parallel Agent Execution

With worktrees, multiple agents can work simultaneously:

```text
Agent A: ../fantasim-world--plate-merger/
         └── Working on plate merger spec

Agent B: ../fantasim-world--persistence-v2/
         └── Working on persistence upgrade spec

Agent C: ../fantasim-world/
         └── Working on constitution update (rare)
```

Each agent has:
- Own working directory
- Own branch HEAD
- Own file locks
- No interference with others

## Environment Variable

If not using git branches (rare), set `SPECIFY_FEATURE` to override feature detection:

```bash
export SPECIFY_FEATURE=plate-merger
specify specify  # Will use plate-merger as feature name
```

Normally not needed when following the worktree convention.

## Troubleshooting

### "Worktree already exists"

```bash
# Check existing worktrees
git worktree list

# Remove stale worktree reference
git worktree prune
```

### "Branch already exists"

```bash
# If branch exists but worktree doesn't, create worktree for existing branch
git worktree add ../fantasim-world--plate-merger spec/plate-merger
```

### "Cannot delete worktree with uncommitted changes"

```bash
# Either commit/stash changes, or force remove
git worktree remove --force ../fantasim-world--plate-merger
```
