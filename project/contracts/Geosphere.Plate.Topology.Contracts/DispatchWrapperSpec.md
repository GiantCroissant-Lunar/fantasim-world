# Polymorphic Dispatch Wrapper Specification

This document describes the internal polymorphic dispatch wrapper format used by `MessagePackEventSerializer` for type-safe deserialization of plate topology events.

## Overview

The polymorphic dispatch wrapper is an **internal** envelope that wraps serialized event payloads to enable polymorphic deserialization. It is distinct from and wraps the RFC-V2-0002 event envelope.

## Format

The wrapper is encoded as a MessagePack **array(2)**:

```
[externalEventTypeId:string, payloadBytes:binary]
```

### Index 0: `externalEventTypeId` (MessagePack **str**)

- The event type discriminator string
- Encoded as `str8`, `str16`, or `str32` (auto-selected by MessagePack based on length)
- Examples: `"PlateCreatedEvent"`, `"BoundaryCreatedEvent"`, `"JunctionRetiredEvent"`
- Must match a key in `EventTypeRegistry.IdToType` mapping

### Index 1: `payloadBytes` (MessagePack **bin**)

- The serialized event payload
- Encoded as `bin8`, `bin16`, or `bin32` (auto-selected by MessagePack based on length)
- Contains the full event object with RFC-V2-0002 envelope fields:
  - `EventId` (Guid)
  - `Tick` (CanonicalTick/Int64)
  - `Sequence` (long)
  - `StreamIdentity` (TruthStreamIdentity tuple)
  - `PreviousHash` (ReadOnlyMemory<byte>)
  - `Hash` (ReadOnlyMemory<byte>)
  - Event-specific payload fields
- Serialized using `TopologySerializationOptions.Options`

## MessagePack Encoding Details

### str Encoding

- ASCII-compatible strings use `str8` (0xC9) or `str16` (0xDA)
- UTF-8 strings with multi-byte characters use `str16` (0xDA) or `str32` (0xDB)
- Format selection is automatic based on byte length

### bin Encoding

- `< 2^8` bytes: `bin8` (0xC4)
- `< 2^16` bytes: `bin16` (0xC5)
- `< 2^32` bytes: `bin32` (0xC6)
- Format selection is automatic based on byte length

## Example

### Serialized Wrapper (hex)

```
96 92 50 6C 61 74 65 43 72 65 61 74 65 64 45 76 65 6E 74 C4 2A 81 ...
```

Breakdown:
- `96` = array(2) header
- `92` = str8 header, length = 18 bytes
- `50 6C 61 74 65 43 72 65 61 74 65 64 45 76 65 6E 74` = "PlateCreatedEvent" (UTF-8)
- `C4 2A` = bin8 header, length = 42 bytes
- `81 ...` = payload bytes (first byte indicates map(1))

### Event Payload Structure

The payload (inside `bin`) contains the full event with RFC envelope fields:

```
{
  "EventId": <uuid>,
  "PlateId": <plate-id>,
  "Tick": <canonical-tick>,
  "Sequence": <sequence>,
  "StreamIdentity": {
    "VariantId": <string>,
    "BranchId": <string>,
    "L": <int>,
    "Domain": <domain>,
    "M": <string>
  },
  "PreviousHash": <bytes>,
  "Hash": <bytes>
}
```

## Type ID Scheme

Event type IDs are stable string identifiers registered in `EventTypeRegistry`:

| Type ID | CLR Type | Category |
|----------|-----------|----------|
| `PlateCreatedEvent` | `PlateCreatedEvent` | Creation |
| `PlateRetiredEvent` | `PlateRetiredEvent` | Lifecycle |
| `BoundaryCreatedEvent` | `BoundaryCreatedEvent` | Creation |
| `BoundaryTypeChangedEvent` | `BoundaryTypeChangedEvent` | State Change |
| `BoundaryGeometryUpdatedEvent` | `BoundaryGeometryUpdatedEvent` | State Change |
| `BoundaryRetiredEvent` | `BoundaryRetiredEvent` | Lifecycle |
| `JunctionCreatedEvent` | `JunctionCreatedEvent` | Creation |
| `JunctionUpdatedEvent` | `JunctionUpdatedEvent` | State Change |
| `JunctionRetiredEvent` | `JunctionRetiredEvent` | Lifecycle |

### Validation

- Type IDs are validated at startup in `EventTypeRegistry` static constructor
- Duplicate IDs throw `InvalidOperationException`
- Unregistered types throw `UnregisteredEventTypeException`
- Unknown type IDs during deserialization throw `UnknownEventTypeException`

## Relationship to RFC-V2-0002

The dispatch wrapper is **nested** within or **wraps** the RFC-V2-0002 event envelope:

```
[DispatchWrapper: [eventId, payloadBytes]]  // Internal polymorphic dispatch
  └─> payloadBytes: {
         "EventId": <uuid>,           // RFC-V2-0002 field
         "Tick": <canonical-tick>,     // RFC-V2-0002 field
         "Sequence": <sequence>,         // RFC-V2-0002 field
         "StreamIdentity": { ... },      // RFC-V2-0002 field
         "PreviousHash": <bytes>,       // RFC-V2-0002 field
         "Hash": <bytes>              // RFC-V2-0002 field
       }
```

The dispatch wrapper is an **implementation detail** of the serialization layer, not part of the RFC specification. It exists solely to enable polymorphic deserialization through MessagePack's type system.

## Determinism Requirements

For deterministic serialization and byte-for-byte identical output:

1. **Frozen Options**: Use `TopologySerializationOptions.Options` exclusively
2. **Type ID Stability**: Event type IDs must never change (use class names)
3. **Array Order**: Dispatch wrapper is always `[eventId, payload]` (index 0, 1)
4. **Encoding Selection**: Let MessagePack auto-select str/bin formats based on length
5. **No Map Keys**: Wrapper uses array format, not map format

## Golden Test Validation

Golden tests verify:

1. **Read Compatibility**: Old serialized bytes deserialize successfully
2. **Write Identicality**: Re-serializing produces SHA256-matching bytes
3. **State Hash**: Materialized topology state hash is identical
4. **Unknown Event**: Unknown event ID throws `UnknownEventTypeException` with ID and stream position

## Migration Path

The dispatch wrapper format is **stable** and must be preserved during:

- Legacy → ECS migration (Phase 2.3 → 3.2)
- Event type additions (append to registry)
- Event type deprecations (keep registry entries, mark obsolete)

Breaking changes to the wrapper format require coordinated migration across all event stores and materializers.
