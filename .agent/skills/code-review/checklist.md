# Code Review Checklist

## Quick Reference

### Must Check

- [ ] **Spec alignment** - Does code match RFC?
- [ ] **Truth boundaries** - No derived→truth deps?
- [ ] **Contract stability** - IDs/schemas stable?
- [ ] **Encoding** - MessagePack for storage?
- [ ] **Graph encapsulation** - ModernSatsuma handles internal?

### Vocabulary Check

Governance axes: `Variant`, `Branch`, `L`, `R`, `M`
Pipeline layers: `Topology`, `Sampling`, `View/Product`

- [ ] No conflation between the two vocabularies

### Truth vs Derived

| Truth | Derived |
|-------|---------|
| Plate Topology | Voronoi meshes |
| Boundary graph | Cell meshes |
| Events | Spatial substrates |

- [ ] Derived never feeds back into truth

### Persistence

- [ ] RocksDB via `modern-rocksdb`
- [ ] MessagePack for canonical encoding
- [ ] JSON only for export/import
- [ ] Events immutable after append
- [ ] Snapshots rebuildable from events

### Implementation Quality

- [ ] Incremental changes (each commit runnable)
- [ ] Algorithms behind contracts
- [ ] No unnecessary complexity
- [ ] No premature optimization
