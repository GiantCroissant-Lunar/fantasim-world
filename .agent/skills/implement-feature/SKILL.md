---
name: implement-feature
version: 0.1.0
kind: composite
description: "Guides incremental feature implementation with stable contracts and runnable state at each step"
contracts:
  success: "Feature implemented incrementally with stable contracts and passing tests"
  failure: "Implementation incomplete, tests failing, or contracts broken"
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
interface DomainEvent {
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

### Step 2.5: Commit Often

**Commit after each logical change.** Do not batch unrelated changes.

**When to commit:**
- After defining a new interface/contract
- After implementing a single function or method
- After adding tests for a component
- After fixing a bug
- After any change that compiles and passes tests

**Commit message format:**
```text
<type>(<scope>): <description>

Task: <task-id>           # If working from spec-kit tasks
Refs: RFC-V2-XXXX         # If implementing an RFC

<optional body>

Co-Authored-By: <agent>
```

**Types:** `feat`, `fix`, `refactor`, `test`, `docs`, `chore`

**Why commit often:**
- Easier to review and understand changes
- Easier to bisect if bugs are introduced
- Safer to revert if needed
- Shows progress to other agents/humans
- Prevents loss of work

### Step 3: Respect Architecture Boundaries

Follow your project's architecture doctrine:

- Keep contracts and interfaces stable
- Don't let implementation details leak into public APIs
- Derived/computed data should not become source-of-truth dependencies

### Step 4: Follow Project Conventions

When persisting data or choosing patterns, follow your project's established conventions for:

- Serialization format
- Storage backend
- Naming conventions
- Error handling patterns

## Checklist

- [ ] Contract/interface defined first
- [ ] Implementation behind contract, not in it
- [ ] Committing after each logical change (not batching)
- [ ] Each commit is runnable
- [ ] Commit messages reference task IDs (if applicable)
- [ ] Architecture boundaries respected
- [ ] Project conventions followed

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
