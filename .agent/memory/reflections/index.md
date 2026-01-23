# Reflections Index

This index is auto-maintained by the `@reflect` skill. It provides a searchable summary of learnings captured after feature implementations.

## Recent Reflections

| Feature | Date | Tags | Key Learning |
|---------|------|------|--------------|
| *(no reflections yet)* | | | |

## By Tag

*(Tags will be added as reflections are created)*

---

## How to Use

### For Agents

Query reflections before starting related work:

```
@reflect --query "plates topology"
@reflect --query "rocksdb gotchas"
@reflect --list-tags
```

### Adding Reflections

Invoke `@reflect` after completing a feature:

```
@reflect plate-topology-snapshots
```

The skill will:
1. Gather context from specs, commits, and reviews
2. Generate a reflection document
3. Save to `.agent/memory/reflections/<feature>.md`
4. Update this index

### Manual Edits

You can manually edit reflection files. The index will be refreshed on the next `@reflect` invocation.
