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
- @.opencode/commands/speckit.specify.md
- @.opencode/commands/speckit.plan.md
- @.opencode/commands/speckit.tasks.md
- @.opencode/agents/plan-hi.md

Rules:
- Read-only. Do not run bash. Do not edit or write any files.
- Your goal is to verify that the OpenCode workflow is set up correctly and identify any remaining gaps.

Audit targets:
- OpenCode configuration: `opencode.json`
- Spec-kit tasks: `Taskfile.yml`
- Commands: `.opencode/commands/*`
- Agents: `.opencode/agents/*`

Checks to perform:
1) Permissions sanity:
   - plan agent: bash denied; edits only `specs/*/plan.md`.
   - build agent: bash allowlisted for spec-kit tasks and read-only commands; edits are ask.
   - deep-thinker: read-only (bash/edit denied) and uses high reasoning effort.
   - external_directory allowlists for fantasim-hub docs and modern-rocksdb.
2) Worktree flow sanity:
   - Confirm `task spec:init` is non-interactive (includes `--ai opencode --script ps --ignore-agent-tools`).
   - Confirm `task spec:new` copies `.specify/`, `.opencode/`, and `opencode.json` into the new worktree.
   - Identify that existing worktrees created before this change may need backfill.
3) Command surface:
   - Identify which commands are safe to run without prompts and which will prompt.
   - Confirm `task sync-commands` exists and syncs `.agent/commands/*.md` into `.opencode/commands/*.md`.
   - Recommend any additional allowlist entries (only if necessary), and call out risks.

Output:
- A concise report with:
  - PASS/FAIL per check
  - specific file/line references
  - recommended next steps to fully validate the flow end-to-end (including how to validate subagent invocation without regenerating plans)
