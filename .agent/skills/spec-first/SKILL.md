---
name: spec-first
description: Ensures agents read and understand specifications before implementing any feature or change. Always start here.
---

# Spec-First Skill

## Purpose

This skill enforces the discipline of reading and understanding specifications before writing any implementation code. It prevents drift from authoritative designs and ensures alignment with project architecture.

## When to Invoke

- Before implementing any new feature
- Before making architectural changes
- When unsure about design decisions
- When starting work on a new component

## Workflow

### Step 1: Identify Relevant Specs

Check the canonical spec locations in order of priority:

1. **V2 RFCs (Primary)**: `../fantasim-hub/docs/rfcs/v2/`
   - Start with `RFC-INDEX.md` for the topology-first spine
   - Look for domain-specific RFCs (plates, persistence, etc.)

2. **ADRs**: `../fantasim-hub/docs/adrs/`
   - Architecture Decision Records explain the "why" behind choices

3. **V1 RFCs (Historical)**: `../fantasim-hub/docs/rfcs/`
   - Only reference if explicitly reaffirmed or no v2 exists

### Step 2: Read and Summarize

Before implementing, create a brief summary:

```markdown
## Spec Summary for [Feature Name]

**Relevant Specs:**
- RFC-V2-XXXX: [title]
- ADR-XXXX: [title]

**Key Constraints:**
- [constraint 1]
- [constraint 2]

**Interfaces/Contracts:**
- [interface 1]
- [interface 2]

**Open Questions:**
- [question 1]
```

### Step 3: Verify Alignment

Before writing code, verify:

- [ ] Implementation matches spec'd interfaces
- [ ] No truth/derived boundary violations
- [ ] Vocabulary matches governance vs pipeline layers
- [ ] Persistence follows DB-first doctrine

## Checklist

- [ ] Read the relevant RFC(s) completely
- [ ] Read any referenced ADRs
- [ ] Identify contracts/IDs that must remain stable
- [ ] Note any "derived" components that must not become truth
- [ ] Document any spec gaps or ambiguities

## Anti-Patterns

- Starting to code before reading specs
- Assuming you know the design from memory
- Conflating governance vocabulary with pipeline vocabulary
- Treating derived data as authoritative truth
