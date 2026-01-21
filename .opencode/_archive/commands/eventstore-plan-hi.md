---
description: Refresh the eventstore-mvp plan using the high-depth planning subagent
agent: plan-hi
subtask: true
---
Update the plan file at `.opencode/plans/eventstore-mvp.md`.

You MUST overwrite the entire contents of `.opencode/plans/eventstore-mvp.md` using file tools (write/patch/edit).
Do not print the full plan to stdout; after writing, output only a brief confirmation.

Constraints:
- You are planning only: do not create/modify code files outside `.opencode/plans/*.md`.
- Do not use bash. Do not run any shell commands.
- Do not ask for review first. Write the full plan into the plan file.
- The plan MUST be aligned to fantasim-hub:
  - docs/rfcs/v2/persistence/RFC-V2-0004-rocksdb-eventstore-and-snapshots.md
  - docs/rfcs/v2/persistence/RFC-V2-0005-messagepack-canonical-encoding.md

Deliverables:
- A phased, numbered checklist that can be executed in build mode.
- Include exact command lines (PowerShell where relevant) and expected outputs/artifacts.
- Include acceptance tests:
  - seq key encoding: uint64 big-endian 8 bytes ordering
  - MessagePack determinism rules (no maps for hashed payloads, explicit integer keys, no floats)
  - hash-chain validation (previousHash linkage)
- Include a “Stop/ask human” section for any ambiguous decisions.
