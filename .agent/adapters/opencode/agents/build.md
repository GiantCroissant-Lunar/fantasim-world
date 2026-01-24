---
description: Build agent with spec-kit task allowlist and ask-before-edit
permission:
  bash:
    "*": ask
    "ls *": allow
    "find *": allow
    "head *": allow
    "task *": allow
    "dotnet *": allow
    "git fetch*": allow
    "git ls-remote*": allow
    "git branch*": allow
    "git config --get*": allow
    "git rev-parse*": allow
    "git status*": allow
    "git worktree list*": allow
    "pwsh *.specify/scripts/powershell/*.ps1*": allow
    ".specify/scripts/powershell/*.ps1*": allow
  edit:
    "*": ask
    "specs/**": allow
---
You are the build agent for spec-kit development.

Rules:
- Permissions are defined in `.agent/adapters/opencode/` (source of truth).
- Run `task agent:sync-opencode` to regenerate `opencode.json`.
