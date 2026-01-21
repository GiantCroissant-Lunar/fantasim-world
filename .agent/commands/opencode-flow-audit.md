---
description: Audit OpenCode + spec-kit dev flow (read-only) using deep-thinker subagent
agent: deep-thinker
subtask: true
---
Audit the OpenCode dev flow setup for this repo.

You MUST output the completed audit report in this single run.
Do not say you are waiting for anything. Do not defer results to a later message.

Context files:
- @opencode.json
- @Taskfile.yml
- @.gitignore
- @scripts/spec_worktree.ps1
- @scripts/sync_opencode.py
- @.agent/commands/speckit.specify.md
- @.agent/commands/speckit.plan.md
- @.agent/commands/speckit.tasks.md
- @.agent/adapters/opencode/agents/plan-hi.md

Rules:
- Read-only. Do not run bash. Do not edit or write any files.
- Your goal is to verify that the OpenCode workflow is set up correctly and identify any remaining gaps.

Audit targets:
- OpenCode configuration: `opencode.json`
- Spec-kit tasks: `Taskfile.yml`
- Canonical commands: `.agent/commands/*`
- Generated OpenCode stubs: `.opencode/*` (should be generated, not source-controlled)

Checks to perform:
1) Permissions sanity:
   - plan agent: bash denied; edits only `specs/*/plan.md`.
   - build agent: bash allowlisted for spec-kit tasks and sync tasks; edits are ask.
   - deep-thinker: read-only (bash/edit denied) and uses high reasoning effort.
   - external_directory allowlists for fantasim-hub docs and modern-rocksdb.
2) Worktree flow sanity:
   - Confirm `task spec:init` is non-interactive (includes `--ai opencode --script ps --ignore-agent-tools`).
   - Confirm `task spec:new` and `task spec:backfill` generate `.opencode/` via `.agent/adapters/opencode` + sync scripts (not by copying a tracked `.opencode`).
   - Identify that existing worktrees may need `task spec:backfill`.
3) Generated `.opencode` policy:
   - Confirm `.gitignore` ignores `.opencode/`.
   - Recommend ensuring git does not track `.opencode/**` (e.g., `git rm -r --cached .opencode`).
4) Command surface:
   - Confirm `task sync-opencode` exists and produces `.opencode/commands` + `.opencode/skills`.
   - Confirm `task sync-commands` exists and syncs `.agent/commands/*.md` into `.opencode/commands/*.md`.
   - Call out risks (e.g., dual agent definitions drift between `opencode.json` and `.agent/adapters/opencode/agents/*.md`).

Output:
- A concise report with:
  - PASS/FAIL per check
  - specific file references
  - recommended next steps to validate end-to-end (including validating subagent invocation)
