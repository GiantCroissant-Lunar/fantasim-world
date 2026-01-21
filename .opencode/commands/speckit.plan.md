---
description: Execute the implementation planning workflow using the plan template to generate design artifacts.
agent: plan-hi
handoffs:
  - label: Create Tasks
    agent: speckit.tasks
    prompt: Break the plan into tasks
    send: true
  - label: Create Checklist
    agent: speckit.checklist
    prompt: Create a checklist for the following domain...
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Locate plan file**: Use the canonical plan file at `specs/<feature>/plan.md`.
   - If it does not exist yet, create it directly.
   - Use `.specify/templates/plan-template.md` as the base structure when available.

2. **Load context**: Read `specs/<feature>/spec.md` and `.specify/memory/constitution.md`.

3. **Write plan.md**: Update the canonical plan file at `specs/<feature>/plan.md`.
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Keep the plan actionable and implementation-oriented

4. **Stop and report**: Command ends after writing `plan.md`. Report the plan path.

## Key rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
