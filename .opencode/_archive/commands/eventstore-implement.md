---
description: Execute the eventstore-mvp plan (bootstrap solution + implement RFC-V2-0004/0005) in small runnable steps
agent: build
---
Follow the plan in `.opencode/plans/eventstore-mvp.md` exactly.

Rules:
- Start by verifying prerequisites and current repo state.
- Execute in small, runnable increments. Keep the repo building at each step.
- Do not change truth/derived boundaries: topology truth is events; spatial substrates are derived.
- Persistence must be DB-first (RocksDB via modern-rocksdb) and canonical bytes must be deterministic MessagePack.

When unsure, stop and ask for clarification rather than inventing new schema.
