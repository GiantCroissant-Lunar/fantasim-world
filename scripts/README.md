# Scripts

This directory contains utility and automation scripts used by the project. The intent is to keep short, focused scripts grouped by purpose (agent tooling, documentation helpers, developer utilities, lightweight helpers, and generated output). Treat the scripts directory as a collection of runnable helpers — not as a stable public API.

## Directory Structure

The following subsections describe each subdirectory and list the contained scripts.

### agent/
Contains scripts that operate the project's agent workflows, synchronisation and high-level command wrappers.

Files:
- sync.py
- opencode.py
- rules.py
- workflows.py
- commands.py
- kilocode.py

Purpose: these scripts are used to start, orchestrate, or inspect agent behaviour and rule/workflow definitions. Run them when you need to perform agent-related automation or to debug agent flows.

### docs/
Helpers for organizing and cleaning project documentation and inboxes.

Files:
- organize_inbox.py
- organize_docs.py
- analyze_duplicates.py
- merge_duplicates.py
- cleanup_inbox.py
- README-cleanup-inbox.md

Purpose: tools in this directory perform document deduplication, reorganization, and cleanup tasks. Use these to keep the docs and inbox folders tidy and to merge or analyze duplicate content.

### dev/
Developer-facing utility scripts used during development and spec worktree maintenance.

Files:
- combine_cs.py
- fix_shared.py
- spec_worktree.ps1

Purpose: small utilities that help with code or spec maintenance. For example, combine_cs.py produces combined C# files for analysis, fix_shared.py applies targeted fixes, and spec_worktree.ps1 contains PowerShell helpers for worktree management.

### utils/
General-purpose Python modules consumed by scripts.

Files:
- __init__.py
- common.py

Purpose of common.py: shared helper functions and small utilities used across scripts (argument parsing helpers, simple file I/O helpers, logging setup, and other reusable utilities). Keep code here framework-agnostic and well-documented so multiple scripts can import common functionality.

### output/
Generated files produced by scripts or build helpers. This folder is an artifact location and should not be edited manually unless you know the generation step.

Files:
- __init__.py
- combined_all_cs.cs
- Rfc0041PolygonizationTests.export.cs

Purpose: holds generated outputs (combined sources, exported files, reports). These files are created/overwritten by scripts and are useful for inspection or CI artifacts.

## Usage

Examples of running scripts from the repository root. Adjust the python command to match your environment (python / python3 / venv path).

- Show help for an agent script:

  python scripts/agent/sync.py --help

- Run a docs organizer in dry-run mode (if the script supports a dry-run flag):

  python scripts/docs/organize_docs.py --dry-run

- Combine C# sources using the dev helper:

  python scripts/dev/combine_cs.py

- Use a utilities module from the interpreter for debugging:

  python -c "import importlib, sys; sys.path.append('scripts'); import scripts.utils.common as common; print(common.__doc__)"

Notes:
- Many scripts implement a --help flag. If a script does not expose CLI parsing, open the file to see available functions and call them as needed.
- Run scripts from the repository root so relative paths resolve consistently.

## Python Best Practices

- Keep scripts small and focused: one behavior per script makes them easier to maintain and test.
- Use the __main__ guard in runnable scripts:

  if __name__ == '__main__':
      main()

- Prefer explicit argument parsing (argparse) over ad-hoc sys.argv handling.
- Put reusable helpers in scripts/utils/common.py and keep them well-documented and typed when possible.
- Use virtual environments and pin dependencies in a requirements file when a script needs external packages.
- Add docstrings and a short usage example at the top of each script to make them self-documenting.
- Keep generated files in scripts/output/ and do not commit regenerated artifacts unless they are intentionally part of the repository history.
- Run linters (flake8, ruff) and type checkers (mypy) on changed scripts to maintain code quality.

If you need to add or reorganize scripts, follow the directory semantics above and update this README accordingly.
