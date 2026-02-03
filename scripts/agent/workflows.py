#!/usr/bin/env python3
"""
Sync workflows from .agent/workflows to tool-specific workflows directories.

Supports adapter overrides: if .agent/adapters/<tool>/workflows/ exists,
those files take precedence over the canonical .agent/workflows/ files.

Usage:
    python scripts/sync_workflows.py
    task sync-workflows
"""

import shutil
from pathlib import Path

SOURCE_DIR = Path(".agent/workflows")
ADAPTERS_DIR = Path(".agent/adapters")

# Tools that support workflows (directory path relative to project root)
WORKFLOW_TARGETS = {
    "kilocode": Path(".kilocode/workflows"),
    # Add more tools as they add workflow support:
    # "claude": Path(".claude/workflows"),
    # "cline": Path(".cline/workflows"),
    # "windsurf": Path(".windsurf/workflows"),
}


def sync_workflows() -> int:
    """Sync all workflows from source to target directories."""
    if not SOURCE_DIR.exists():
        print(f"Source directory {SOURCE_DIR} does not exist, skipping")
        return 0

    print(f"Syncing workflows from {SOURCE_DIR}...")

    workflow_count = 0

    for tool_name, target_dir in WORKFLOW_TARGETS.items():
        # Ensure target directory exists
        target_dir.mkdir(parents=True, exist_ok=True)

        # Clear existing workflows
        for existing in target_dir.glob("*.md"):
            if existing.is_file():
                existing.unlink()

        print(f"  -> {target_dir}")

        # Check for adapter overrides
        adapter_workflows = ADAPTERS_DIR / tool_name / "workflows"

        # Collect workflow files: canonical first, then adapter overrides
        workflow_files: dict[str, Path] = {}

        # Add canonical workflows
        for src in SOURCE_DIR.glob("*.md"):
            workflow_files[src.name] = src

        # Override with adapter-specific workflows if they exist
        if adapter_workflows.exists():
            print(f"     (with adapter overrides from {adapter_workflows})")
            for src in adapter_workflows.glob("*.md"):
                workflow_files[src.name] = src

        # Copy collected workflows to target
        for filename, src_path in workflow_files.items():
            dst = target_dir / filename
            shutil.copy2(src_path, dst)
            print(f"     [OK] {filename}")
            workflow_count += 1

    print("\nSync complete!")
    print(f"Workflows synced: {workflow_count}")
    return workflow_count


if __name__ == "__main__":
    sync_workflows()
