# Agent Guide for fantasim-world

This repo is a clean restart of **fantasim-world**.

## Canonical Specs (Source of Truth)

Authoritative architecture and doctrine live in:

- `../fantasim-hub/docs/rfcs/`
- v2 topology-first spine:
  - `../fantasim-hub/docs/rfcs/v2/RFC-INDEX.md`

When implementing, prefer v2 RFCs where available, and treat v1 RFCs as historical unless explicitly reaffirmed.

## Two Vocabularies (Do Not Conflate)

- **Governance / identity axes**: `Variant`, `Branch`, `L`, `R`, `M`
- **Pipeline layering**: `Topology`, `Sampling`, `View/Product`

See:

- `../fantasim-hub/docs/TERMINOLOGY.md` → "Governance Axes vs Pipeline Layers"

## Topology-First Doctrine (Plates)

For the plates domain:

- **Authoritative truth** is **Plate Topology** (boundary graph + events).
- **Spatial substrates** (Voronoi/DGGS/cell meshes, cell-to-plate assignment) are **derived sampling products**.

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0003-topology-first-truth-policy-plates.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/plates/RFC-V2-0001-plate-topology-truth-slice.md`

## DB-First Persistence + Canonical Encoding

- Persistence backend: **RocksDB** via `modern-rocksdb`.
- Canonical event encoding: **MessagePack** (DB-first; JSON is export/import only).

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0005-use-modern-rocksdb-for-db-first-persistence.md`
- ADR: `../fantasim-hub/docs/adrs/ADR-0006-use-messagepack-for-canonical-event-encoding.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/persistence/RFC-V2-0004-rocksdb-eventstore-and-snapshots.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/persistence/RFC-V2-0005-messagepack-canonical-encoding.md`

## In-Memory Graph Engine

- In-memory graph engine for topology materialization: **Plate.ModernSatsuma**.
- This is an implementation detail: do not leak ModernSatsuma node/arc handles into truth events or persisted state.

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0004-use-modern-satsuma-for-topology-graph-engine.md`

## Agent Workflow (Skills)

This repo uses a shared skills system compatible with multiple AI coding assistants:

- Claude Code: https://docs.anthropic.com/en/docs/claude-code/skills
- Windsurf Cascade: https://docs.windsurf.com/windsurf/cascade/skills
- Cline: https://docs.cline.bot/features/skills

### Skills Architecture

Skills are stored in a shared location with tool-specific stub files:

```text
.agent/skills/          # Source of truth (tracked in git)
.claude/skills/         # Generated stubs (git-ignored)
.windsurf/skills/       # Generated stubs (git-ignored)
.cline/skills/          # Generated stubs (git-ignored)
```

**After cloning or pulling**, run:

```bash
task sync-skills
```

This regenerates the tool-specific stubs from `.agent/skills/`.

This ensures all agents follow the same guidelines without duplication.

### Available Skills

See `.agent/skills/SKILLS-INDEX.md` for full documentation.

| Skill | Description |
|-------|-------------|
| `@spec-kit-bridge` | Spec-kit workflow orchestration |
| `@spec-first` | Read and understand specs before implementing |
| `@implement-feature` | Incremental feature implementation with stable contracts |
| `@code-review` | Architecture-focused code review |
| `@persistence` | RocksDB + MessagePack patterns |
| `@topology-first` | Plates domain truth boundary enforcement |

### Core Expectations

- Prefer reading specs first (the v2 spine + referenced governance RFCs) before implementing.
- Keep contracts/IDs/event schemas stable; put algorithms and solvers behind those contracts.
- Enforce "derived stays derived": sampling/products must never become truth dependencies.
- Make changes incrementally and keep the repo runnable at each step.

## Spec-Driven Development

This repo uses **GitHub Spec Kit** for spec-driven development.

### Core Invariant

**One spec = one feature branch = one git worktree**

Spec work is never done directly on `main`.

### Interfaces

Agents may use either interface (both produce the same artifacts):

| Interface | When to Use |
|-----------|-------------|
| `specify` CLI | Preferred for local agents, automation |
| `/speckit.*` slash commands | Chat environments (GitHub Copilot, etc.) |

### Quick Start

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

### Phase → Skill Mapping

| Spec Kit Phase | Primary Skill |
|----------------|---------------|
| Constitution | (rarely edited) |
| Specify / Clarify | `@spec-first` |
| Plan | `@topology-first`, `@persistence` |
| Tasks | `@spec-kit-bridge` |
| Implement | `@implement-feature` |
| Review | `@code-review` |

See `.agent/skills/spec-kit-bridge/` for detailed phase mapping and worktree playbook.
