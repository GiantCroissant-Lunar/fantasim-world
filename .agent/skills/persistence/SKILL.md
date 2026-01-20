---
name: persistence
description: Guides RocksDB persistence and MessagePack encoding following DB-first doctrine
---

# Persistence Skill

## Purpose

This skill guides all persistence operations following the DB-first doctrine: RocksDB storage with MessagePack canonical encoding.

## When to Invoke

- When implementing event storage
- When creating snapshots
- When designing data schemas
- When importing/exporting data

## Core Principles

### DB-First Doctrine

```
RocksDB (via modern-rocksdb)
    └── MessagePack (canonical encoding)
            └── JSON (export/import ONLY)
```

- **Truth lives in RocksDB**, not in files
- **MessagePack is the canonical format**, not JSON
- **JSON is for human interchange only**

## Implementation Guide

### Setting Up RocksDB

Use `modern-rocksdb` from .NET and follow the persistence RFCs:

- RFC-V2-0004: column families and key schema
- RFC-V2-0005: MessagePack canonical encoding rules

The recommended column families are:

- `events`
- `stream_meta`
- `snapshots`
- `read_models`

### MessagePack Encoding

Use MessagePack for .NET (MessagePack-CSharp) with explicit keys for determinism:

- Use `[MessagePackObject]` + `[Key(n)]` style schemas for event envelope/payload
- Avoid map/dictionary types in hash-critical payloads (RFC-V2-0005)

### Key Design

Design keys for efficient range queries:

Keys MUST match the persistence RFC (RFC-V2-0004):

- Stream prefix: `S:{variant}:{branch}:L{l}:{domain}:M{m}:`
- Event key: `S:{...}:E:{seq}` where `{seq}` is uint64 big-endian fixed-width 8 bytes
- Stream head metadata: `S:{...}:Head`
- Snapshot key: `S:{...}:Snap:{tick}`

### Event Store Pattern

```typescript
interface EventStore {
  // Append event (immutable)
  append(event: PlateEvent): Promise<void>;

  // Read events for entity
  getEvents(plateId: string, after?: number): AsyncIterable<PlateEvent>;

  // Get latest snapshot
  getSnapshot(plateId: string): Promise<PlateSnapshot | null>;

  // Save snapshot
  saveSnapshot(snapshot: PlateSnapshot): Promise<void>;
}
```

### Snapshots

Snapshots are optimization, not truth:

```typescript
interface PlateSnapshot {
  readonly plateId: string;
  readonly version: number;
  readonly lastEventTimestamp: number;
  readonly state: PlateState;  // Materialized from events
}
```

Rebuild capability must be preserved:
```typescript
// Must always be possible to rebuild from events
async function rebuildState(plateId: string): Promise<PlateState> {
  let state = initialState();
  for await (const event of store.getEvents(plateId)) {
    state = applyEvent(state, event);
  }
  return state;
}
```

## Export/Import (JSON)

JSON is ONLY for human interchange:

```typescript
// Export to JSON (for external tools)
async function exportToJson(plateId: string): Promise<string> {
  const events = [];
  for await (const event of store.getEvents(plateId)) {
    events.push(event);
  }
  return JSON.stringify(events, null, 2);
}

// Import from JSON (from external sources)
async function importFromJson(json: string): Promise<void> {
  const events = JSON.parse(json) as PlateEvent[];
  for (const event of events) {
    await store.append(event);  // Stored as MessagePack
  }
}
```

## Checklist

- [ ] Using `modern-rocksdb` for storage
- [ ] MessagePack for all canonical encoding
- [ ] JSON only for export/import
- [ ] Keys designed for range queries
- [ ] Snapshots are rebuildable from events
- [ ] Events are immutable after append

## Anti-Patterns

```typescript
// BAD: JSON as storage format
await db.put(key, JSON.stringify(event));

// BAD: File-based truth
fs.writeFileSync('topology.json', JSON.stringify(topology));

// BAD: Mutable events
event.status = 'processed';  // NO!

// BAD: Snapshot as truth
const state = snapshot.state;  // Should verify against events
```

## Reference Docs

- ADR-0005: Use modern-rocksdb for DB-first persistence
- ADR-0006: Use MessagePack for canonical event encoding
- RFC-V2-0004: RocksDB EventStore and Snapshots
- RFC-V2-0005: MessagePack Canonical Encoding
