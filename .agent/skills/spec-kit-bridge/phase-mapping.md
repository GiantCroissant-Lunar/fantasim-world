# Phase Mapping

This document maps Spec Kit phases to fantasim-world skills and artifacts.

## Phase Overview

| Phase | Spec Kit Command | Primary Skill | Output Artifact |
|-------|------------------|---------------|-----------------|
| Constitution | `/speckit.constitution` | — | `.specify/memory/constitution.md` |
| Specify | `/speckit.specify` | `@spec-first` | `specs/<feature>/spec.md` |
| Clarify | `/speckit.clarify` | `@spec-first` | Updated `spec.md` |
| Plan | `/speckit.plan` | `@topology-first`, `@persistence` | `specs/<feature>/plan.md` |
| Tasks | `/speckit.tasks` | `@spec-kit-bridge` | `specs/<feature>/tasks.md` |
| Analyze | `/speckit.analyze` | `@spec-kit-bridge` | Consistency report |
| Implement | `/speckit.implement` | `@implement-feature` | Code + tests |
| Checklist | `/speckit.checklist` | `@code-review` | Quality checklist |

## Phase Details

### Constitution

**Rarely edited. Changes require dedicated PR.**

The constitution defines project principles. It lives in `.specify/memory/constitution.md` and should align with:
- `AGENTS.md` (agent guidelines)
- `../fantasim-hub/docs/rfcs/` (authoritative specs)

### Specify / Clarify

**Primary skill: `@spec-first`**

Requirements definition phase. The spec should include:

```markdown
# Feature: <name>

## Slice Tags
- Geosphere.Topology    # If it touches plates/boundaries
- Persistence           # If it touches storage/events
- Noosphere.Calendar    # If it touches time/scheduling

## Requirements
...

## User Stories
...

## Acceptance Criteria
...
```

Invoke `@spec-first` to ensure:
- Relevant RFCs are read first
- Vocabulary is correct (governance vs pipeline)
- Truth/derived boundaries are understood

### Plan

**Primary skill: Depends on dominant slice**

| Dominant Slice | Primary Skill |
|----------------|---------------|
| Topology | `@topology-first` |
| Persistence | `@persistence` |
| Mixed | Both, in order |

The plan should include:
- Proposed contracts/interfaces
- Projects/modules touched
- Integration points
- RFC alignment notes

### Tasks

**Primary skill: `@spec-kit-bridge`**

Task generation phase. Enforce these rules:

1. **Agent-separable**: Each task completable independently
2. **Testable**: Clear success criteria per task
3. **Minimal coupling**: Reduce cross-task dependencies
4. **Traceable**: Reference spec sections

Example task format:
```markdown
## Task 1.1: Define PlateEvent interface
- Spec ref: Requirements §2.1
- Success: Interface compiles, matches RFC-V2-0001
- Dependencies: None
```

### Implement

**Primary skill: `@implement-feature`**

Execute tasks in order. For each task:

1. Check which domain skill applies:
   - Topology work → `@topology-first`
   - Storage work → `@persistence`

2. Follow `@implement-feature` patterns:
   - Contract first
   - Incremental commits
   - Each commit runnable

3. Reference task IDs in commits:
   ```
   feat(plates): add PlateEvent interface

   Task: 1.1
   Spec: plate-merger
   ```

### Review

**Primary skill: `@code-review`**

Before PR merge:
- Run `@code-review` checklist
- Verify spec alignment
- Check truth/derived boundaries
- Validate encoding (MessagePack)

## CLI vs Slash Command Equivalents

| Phase | CLI Command | Slash Command |
|-------|-------------|---------------|
| Specify | `specify specify` | `/speckit.specify` |
| Clarify | `specify clarify` | `/speckit.clarify` |
| Plan | `specify plan` | `/speckit.plan` |
| Tasks | `specify tasks` | `/speckit.tasks` |
| Analyze | `specify analyze` | `/speckit.analyze` |
| Implement | `specify implement` | `/speckit.implement` |
| Checklist | `specify checklist` | `/speckit.checklist` |

Both produce the same artifacts. Use whichever your environment supports.
