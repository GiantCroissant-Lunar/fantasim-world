---
name: reflect
version: 0.1.0
kind: composite
description: "Captures learnings after feature completion for future agent reference"
contracts:
  success: "Learnings captured in reflections directory with actionable insights"
  failure: "Unable to identify or document meaningful learnings"
---

# Reflect Skill

## Purpose

This skill captures learnings, patterns, gotchas, and decisions after a feature is complete. Reflections are stored in `.agent/memory/reflections/` and indexed for future agent queries.

Based on the [Reflection Loop](https://arxiv.org/abs/2303.11366) pattern for iterative improvement through self-feedback.

## When to Invoke

- After `@code-review` approves the implementation
- After a feature is merged
- After resolving a significant bug or issue
- When explicitly asked to document learnings

## Prerequisites

- Feature implementation is complete
- Code review has passed
- Spec exists at `specs/<feature>/spec.md`

## Workflow

### Step 1: Gather Context

Collect information about the completed feature:

1. Read the spec: `specs/<feature>/spec.md`
2. Read the plan: `specs/<feature>/plan.md`
3. Read self-critique report (if exists)
4. Review git commits for the feature
5. Note any issues or discussions

### Step 2: Generate Reflection

Create a reflection document answering:

1. **What was built?** (1-2 sentence summary)
2. **What worked well?** (patterns, approaches)
3. **What were the gotchas?** (unexpected issues)
4. **What decisions were made?** (and why)
5. **What would you do differently?** (hindsight)

### Step 3: Extract Tags

Identify tags for indexing:
- Domain tags: `plates`, `topology`, `persistence`
- Pattern tags: `event-sourcing`, `snapshot`, `encoding`
- Tool tags: `rocksdb`, `messagepack`, `modernsatsuma`

### Step 4: Save Reflection

Write to `.agent/memory/reflections/<feature-name>.md`

### Step 5: Update Index

Auto-update `.agent/memory/reflections/index.md` with:
- New entry in the table
- Tags added to tag sections
- Key learnings summary

## Reflection Template

```markdown
# Reflection: <feature-name>

**Date:** YYYY-MM-DD
**Spec:** `specs/<feature>/spec.md`
**Tags:** `tag1`, `tag2`, `tag3`

## Summary

[1-2 sentence description of what was built]

## What Worked Well

- **[Pattern/Approach]**: [Why it worked]
- **[Pattern/Approach]**: [Why it worked]

## Gotchas & Edge Cases

- **[Issue]**: [What happened and how it was resolved]
- **[Issue]**: [What happened and how it was resolved]

## Decisions Made

### [Decision Title]
- **Decision:** [What was decided]
- **Alternatives:** [What was considered]
- **Rationale:** [Why this choice]

## What I'd Do Differently

- [Hindsight observation]

## Related

- RFC: [link]
- ADR: [link]
- Related reflections: [links]
```

## Index Format

The index at `.agent/memory/reflections/index.md` is auto-maintained:

```markdown
# Reflections Index

## Recent Reflections

| Feature | Date | Tags | Key Learning |
|---------|------|------|--------------|
| [feature](./feature.md) | 2026-01-23 | tags | One-liner |

## By Tag

### plates
- [feature-1](./feature-1.md) - summary
- [feature-2](./feature-2.md) - summary

### persistence
- [feature-1](./feature-1.md) - summary
```

## Querying Reflections

Future agents can query reflections:

```
@reflect --query "plates topology"
@reflect --query "rocksdb gotchas"
@reflect --list-tags
```

The skill will search the index and return relevant reflections.

## Example Reflection

```markdown
# Reflection: plate-topology-snapshots

**Date:** 2026-01-20
**Spec:** `specs/plate-topology-snapshots/spec.md`
**Tags:** `plates`, `persistence`, `snapshots`, `rocksdb`

## Summary

Implemented head-only snapshot persistence for plate topology using RocksDB,
storing the materializer state as MessagePack-encoded blobs.

## What Worked Well

- **Separation of snapshot vs event storage**: Keeping snapshots in a
  separate column family made compaction independent and queries fast.
- **MessagePack for large blobs**: Significantly smaller than JSON,
  faster to encode/decode for topology graphs.

## Gotchas & Edge Cases

- **ModernSatsuma handle serialization**: Initially tried to serialize
  node handles directly. Had to create stable ID mapping first, then
  serialize the ID-based representation.
- **Batch size limits**: RocksDB write batches over 100MB caused memory
  pressure. Added chunking for large snapshots.

## Decisions Made

### Head-only vs Full History
- **Decision:** Store only head snapshot, not full history
- **Alternatives:** Store all snapshots, store periodic checkpoints
- **Rationale:** Events provide full history; snapshots are optimization
  for fast reload. Head-only is simplest and sufficient.

## What I'd Do Differently

- Would have designed the ID mapping layer first before touching
  ModernSatsuma serialization.

## Related

- RFC: RFC-V2-0004-rocksdb-eventstore-and-snapshots.md
- ADR: ADR-0005-use-modern-rocksdb-for-db-first-persistence.md
```

## Checklist

- [ ] Reflection captures all key learnings
- [ ] Tags are relevant and consistent with existing tags
- [ ] Index is updated
- [ ] Related specs/RFCs are linked
- [ ] Gotchas are actionable for future agents
