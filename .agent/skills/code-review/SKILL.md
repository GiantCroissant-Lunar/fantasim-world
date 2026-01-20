---
name: code-review
description: Guides code review process with focus on architecture compliance, truth boundaries, and contract stability
---

# Code Review Skill

## Purpose

This skill guides code reviews with focus on project-specific concerns: architecture compliance, truth/derived boundaries, contract stability, and proper encoding.

## When to Invoke

- When reviewing pull requests
- When self-reviewing before commit
- When auditing existing code

## Review Dimensions

### 1. Spec Alignment

- [ ] Does the code match the relevant RFC?
- [ ] Are ADR decisions respected?
- [ ] Is v2 preferred over v1 where available?

### 2. Vocabulary Correctness

Check for conflation of two distinct vocabularies:

**Governance/Identity Axes:**
- `Variant`, `Branch`, `L`, `R`, `M`

**Pipeline Layering:**
- `Topology`, `Sampling`, `View/Product`

```typescript
// BAD: Conflating vocabularies
interface PlateVariantSampling { ... } // Mixed!

// GOOD: Keep separate
interface PlateVariant { ... }        // Governance
interface PlateSampling { ... }       // Pipeline
```

### 3. Truth Boundary Violations

**Red Flags:**
- Derived data being used as input to truth computation
- Spatial substrates (Voronoi, cells) treated as authoritative
- Events depending on sampling products

```typescript
// BAD: Truth depends on derived
function updateTopology(voronoiMesh: VoronoiMesh) {
  topology.boundaries = voronoiMesh.edges; // NO!
}

// GOOD: Derived depends on truth
function generateMesh(topology: PlateTopology): VoronoiMesh {
  return sample(topology); // Correct direction
}
```

### 4. Contract Stability

- [ ] Are IDs stable and immutable?
- [ ] Are event schemas backwards-compatible?
- [ ] Are algorithms behind contracts, not in them?

### 5. Persistence Correctness

- [ ] MessagePack for canonical encoding?
- [ ] JSON only for export/import?
- [ ] RocksDB via `modern-rocksdb`?

### 6. Graph Engine Encapsulation

- [ ] ModernSatsuma handles not leaked externally?
- [ ] Node/arc handles not in events or persisted state?

## Review Checklist Template

```markdown
## Code Review: [PR/Change Name]

### Spec Alignment
- [ ] Matches RFC: [RFC number]
- [ ] ADR compliance: [ADR numbers]

### Architecture
- [ ] No vocabulary conflation
- [ ] Truth boundaries respected
- [ ] Contracts stable

### Implementation
- [ ] MessagePack encoding
- [ ] ModernSatsuma encapsulated
- [ ] Incremental and runnable

### Issues Found
1. [Issue description]
   - Location: [file:line]
   - Severity: [Critical/Major/Minor]
   - Recommendation: [fix]
```

## Severity Levels

| Level | Description | Action |
|-------|-------------|--------|
| Critical | Truth boundary violation, contract break | Block merge |
| Major | Vocabulary conflation, wrong encoding | Request changes |
| Minor | Style, naming, documentation | Suggest improvement |
