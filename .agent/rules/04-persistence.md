---
name: persistence
description: DB-first persistence and canonical encoding rules
order: 4
globs:
  - "**/persistence/**"
  - "**/storage/**"
  - "**/events/**"
---

# DB-First Persistence + Canonical Encoding

- Persistence backend: **RocksDB** via `modern-rocksdb`.
- Canonical event encoding: **MessagePack** (DB-first; JSON is export/import only).

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0005-use-modern-rocksdb-for-db-first-persistence.md`
- ADR: `../fantasim-hub/docs/adrs/ADR-0006-use-messagepack-for-canonical-event-encoding.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/persistence/RFC-V2-0004-rocksdb-eventstore-and-snapshots.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/persistence/RFC-V2-0005-messagepack-canonical-encoding.md`
