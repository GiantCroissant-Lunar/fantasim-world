---
name: implement-feature
description: Guides incremental feature implementation with stable contracts and runnable state at each step
---

# Implement Feature Skill

## Purpose

This skill guides the implementation of new features following project conventions: incremental changes, stable contracts, and maintaining a runnable repo at each step.

## When to Invoke

- When adding new functionality
- When extending existing components
- When implementing RFC specifications

## Prerequisites

Before using this skill, invoke `@spec-first` to ensure you understand the design.

## Workflow

### Step 1: Define the Contract First

Before implementation, define stable interfaces:

```typescript
// Define the contract/interface FIRST
interface PlateEvent {
  readonly eventId: string;
  readonly timestamp: number;
  // ... stable fields
}
```

**Rules:**
- IDs and event schemas must be stable
- Algorithms go behind contracts, not in them
- Use readonly where immutability is expected

### Step 2: Incremental Implementation

Break work into small, runnable increments:

1. **Skeleton**: Types and interfaces only (compiles, no logic)
2. **Core Logic**: Implement the happy path
3. **Edge Cases**: Handle errors and boundaries
4. **Integration**: Connect to other components

Each increment must:
- Compile without errors
- Pass existing tests
- Not break other components

### Step 3: Respect Truth Boundaries

For the plates domain specifically:

| Truth (Authoritative) | Derived (Products) |
|-----------------------|--------------------|
| Plate Topology | Voronoi meshes |
| Boundary graph | Cell assignments |
| Events | Spatial substrates |

**Never** let derived data become a truth dependency.

### Step 4: Encoding and Persistence

When persisting data:

- **Canonical encoding**: MessagePack (for RocksDB storage)
- **Export/Import only**: JSON
- **In-memory graphs**: ModernSatsuma (internal detail, not exposed)

```typescript
// Good: MessagePack for storage
const encoded = msgpack.encode(event);
await db.put(key, encoded);

// Bad: JSON for storage
await db.put(key, JSON.stringify(event)); // NO!
```

## Checklist

- [ ] Contract/interface defined first
- [ ] Implementation behind contract, not in it
- [ ] Each commit is runnable
- [ ] No truth/derived violations
- [ ] MessagePack for persistence
- [ ] ModernSatsuma handles not leaked

## Templates

### Feature Implementation Template

```typescript
// 1. Contract (stable)
export interface FeatureContract {
  readonly id: string;
  // stable interface...
}

// 2. Implementation (can change)
class FeatureImpl implements FeatureContract {
  // algorithm details here...
}

// 3. Factory (hides implementation)
export function createFeature(): FeatureContract {
  return new FeatureImpl();
}
```
