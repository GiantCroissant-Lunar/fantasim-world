---
description: Build agent with spec-kit task allowlist and ask-before-edit
mode: subagent
permission:
  bash:
    "*": ask
    "ls*": allow
    "task sync-skills*": allow
    "task sync-commands*": allow
    "task sync-opencode*": allow
    "task spec:init*": allow
    "task spec:new*": allow
    "task spec:list*": allow
    "task spec:backfill*": allow
    "task spec:specify*": allow
    "task spec:plan*": allow
    "task spec:tasks*": allow
    "task spec:implement*": allow
    "task spec:done*": allow
    "pwsh*.specify/scripts/powershell/*.ps1*": allow
    "pwsh*\\.specify\\scripts\\powershell\\*.ps1*": allow
    ".specify/scripts/powershell/*.ps1*": allow
    ".specify\\scripts\\powershell\\*.ps1*": allow
    "dotnet --info*": allow
    "dotnet restore*": allow
    "dotnet build*": allow
    "dotnet test*": allow
    "git fetch*": allow
    "git ls-remote*": allow
    "git branch -a*": allow
    "git branch --all*": allow
    "git branch --list*": allow
    "git branch --show-current*": allow
    "git branch -r*": allow
    "git config --get*": allow
    "git rev-parse*": allow
    "git status*": allow
    "git worktree list*": allow
  edit: ask
---
You are the build agent for spec-kit development.

Rules:
- Bash is allowlisted for spec-kit tasks and read-only git/dotnet commands.
- All edits require user confirmation (ask).
- External directory access is allowed for fantasim-hub/docs and modern-rocksdb.
