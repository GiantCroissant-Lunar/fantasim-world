---
name: self-critique
description: Self-evaluation loop that reviews implementation against specs and doctrine before marking complete
---

# Self-Critique Skill

## Purpose

This skill implements a self-evaluation loop where the agent reviews its own implementation against the spec, project doctrine, and quality criteria. It runs iteratively until the implementation passes or max iterations are reached.

Based on the [Self-Critique Evaluator Loop](https://arxiv.org/abs/2408.02666) and [Reflection Loop](https://arxiv.org/abs/2303.11366) patterns.

## When to Invoke

- After completing implementation, before marking task as done
- Before requesting code review
- When unsure if implementation meets requirements

## Prerequisites

- Implementation code exists
- Spec is available at `specs/<feature>/spec.md`
- `@implement-feature` workflow was followed

## Configuration

### Project Defaults

See `rubric.yaml` in this directory for default criteria and weights.

### Feature Override

Create `specs/<feature>/critique.yaml` to override defaults:

```yaml
extends: default
threshold: 0.9  # Stricter for this feature
criteria:
  truth_boundary:
    weight: 0.35  # Higher weight for plates domain
```

### Runtime Override

Invoke with arguments:
```
@self-critique --threshold=0.9 --max-iterations=5
```

## Evaluation Loop

```
for attempt in range(max_iterations):
    score, violations = evaluate(implementation, spec, rubric)

    if has_blockers(violations):
        fix(blockers)
        continue

    if score >= threshold:
        return PASS

    fix(highest_weight_violations)

if score < threshold:
    escalate_to_human()
```

## Evaluation Process

### Step 1: Load Context

1. Read the spec: `specs/<feature>/spec.md`
2. Read the plan: `specs/<feature>/plan.md`
3. Load rubric: `rubric.yaml` + feature override if exists
4. Identify implementation files from plan/tasks

### Step 2: Evaluate Each Criterion

For each criterion in the rubric:

1. **Spec Alignment** (default 30%)
   - Compare implementation against spec requirements
   - Check all acceptance criteria are met
   - Verify edge cases are handled

2. **Truth Boundary Compliance** (default 25%, blocker)
   - No derived data used as truth input
   - Spatial substrates are products, not sources
   - Events don't depend on sampling products

3. **Contract Stability** (default 20%)
   - IDs are stable and immutable
   - Event schemas are backwards-compatible
   - Algorithms are behind contracts, not in them

4. **Encoding Correctness** (default 15%)
   - MessagePack for RocksDB storage
   - JSON only for export/import
   - ModernSatsuma handles not leaked

5. **Runnable State** (default 10%, blocker)
   - Code compiles without errors
   - Existing tests pass
   - No broken imports/dependencies

### Step 3: Score and Decide

```
total_score = sum(criterion.score * criterion.weight)

if any(blocker.failed):
    return MUST_FIX, blocker_violations

if total_score >= threshold:
    return PASS, []

return NEEDS_IMPROVEMENT, sorted_violations_by_impact
```

### Step 4: Fix or Escalate

- **PASS**: Proceed to `@code-review`
- **MUST_FIX**: Fix blockers, re-evaluate
- **NEEDS_IMPROVEMENT**: Fix top violations, re-evaluate
- **Max iterations reached**: Escalate to human with summary

## Output Format

```markdown
## Self-Critique Report: <feature-name>

### Attempt: 1/3

| Criterion | Score | Weight | Weighted | Status |
|-----------|-------|--------|----------|--------|
| Spec Alignment | 0.85 | 0.30 | 0.255 | PASS |
| Truth Boundary | 1.00 | 0.25 | 0.250 | PASS |
| Contract Stability | 0.70 | 0.20 | 0.140 | NEEDS WORK |
| Encoding | 1.00 | 0.15 | 0.150 | PASS |
| Runnable | 1.00 | 0.10 | 0.100 | PASS |

**Total Score: 0.895** (threshold: 0.80)

### Violations Found
1. [Contract Stability] Event schema missing `readonly` on `eventId`
   - Location: `src/plates/events.ts:45`
   - Fix: Add `readonly` modifier

### Result: PASS
```

## Checklist

Before marking complete:

- [ ] All blockers resolved (truth boundary, runnable)
- [ ] Total score >= threshold
- [ ] Violations documented (even if minor)
- [ ] Report saved for code review reference

## Integration with Workflow

```
@implement-feature
       ↓
  [implementation complete]
       ↓
@self-critique  ←──┐
       ↓           │
   evaluate        │
       ↓           │
  score < threshold? ──→ fix → re-evaluate
       ↓
   PASS
       ↓
@code-review
       ↓
@reflect
```
