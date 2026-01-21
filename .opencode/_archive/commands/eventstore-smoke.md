---
description: Smoke test the spec-kit worktree flow for eventstore-mvp (spec:init + spec:new only)
agent: build
---
Run a smoke test of the spec-kit + worktree workflow for `eventstore-mvp`.

Scope:
- Run ONLY:
  - `task spec:init`
  - `task spec:new -- eventstore-mvp`
- Do NOT implement the eventstore, do NOT create solution/projects, do NOT edit code.

Instructions:
1) In the main worktree (repo root), run:
   - `task spec:init`
2) Then run:
   - `task spec:new -- eventstore-mvp`
3) Stop.

Report:
- Confirm whether `.specify/` was created.
- Confirm whether a new worktree was created and its path (expected `../fantasim-world--eventstore-mvp`).
- Show the output of `git worktree list`.
- If you encounter a permission prompt or a failure, stop and report exactly what was requested/failed.
