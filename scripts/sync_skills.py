#!/usr/bin/env python3
"""
Sync skill stubs from .agent/skills to tool-specific skills directories.

Reads all SKILL.md files from .agent/skills/ and creates stub SKILL.md files
in .claude/skills/, .windsurf/skills/, and .cline/skills/ that reference
the shared source.

Usage:
    python scripts/sync_skills.py
    task sync-skills
"""

import re
import shutil
from pathlib import Path

SOURCE_DIR = Path(".agent/skills")
TARGET_DIRS = [
    Path(".claude/skills"),
    Path(".cline/skills"),
    Path(".kilocode/skills"),
    Path(".opencode/skills"),
    Path(".windsurf/skills"),
]


def extract_frontmatter(content: str) -> dict[str, str]:
    """Extract YAML frontmatter from SKILL.md content."""
    match = re.match(r"^---\r?\n(.+?)\r?\n---", content, re.DOTALL)
    if not match:
        return {}

    frontmatter = {}
    for line in match.group(1).split("\n"):
        if ":" in line:
            key, value = line.split(":", 1)
            frontmatter[key.strip()] = value.strip()
    return frontmatter


def to_title_case(name: str) -> str:
    """Convert skill-name to Title Case."""
    return " ".join(word.capitalize() for word in name.split("-"))


def create_stub(skill_name: str, name: str, description: str) -> str:
    """Create stub SKILL.md content."""
    title = to_title_case(name)
    relative_path = f"../../../.agent/skills/{skill_name}/SKILL.md"

    return f"""---
name: {name}
description: {description}
---

# {title}

**This is a stub that references the shared skill definition.**

Read the full skill instructions from: [{relative_path}]({relative_path})

## Quick Reference

- **Source**: `.agent/skills/{skill_name}/SKILL.md`
- **Description**: {description}

## Instructions

When this skill is invoked, read and follow the complete instructions at:
`.agent/skills/{skill_name}/SKILL.md`
"""


def sync_skills() -> int:
    """Sync all skills from source to target directories."""
    print(f"Syncing skills from {SOURCE_DIR}...")

    # Ensure target directories exist and clean them
    for target_dir in TARGET_DIRS:
        target_dir.mkdir(parents=True, exist_ok=True)
        # Remove existing skill directories
        for item in target_dir.iterdir():
            if item.is_dir():
                shutil.rmtree(item)
        print(f"  -> {target_dir}")

    # Process each skill directory
    skill_count = 0
    for skill_dir in sorted(SOURCE_DIR.iterdir()):
        if not skill_dir.is_dir():
            continue

        skill_name = skill_dir.name
        source_path = skill_dir / "SKILL.md"

        if not source_path.exists():
            print(f"  [SKIP] {skill_name} - no SKILL.md found")
            continue

        # Read and parse source
        content = source_path.read_text(encoding="utf-8")
        frontmatter = extract_frontmatter(content)

        name = frontmatter.get("name", "")
        description = frontmatter.get("description", "")

        if not name:
            print(f"  [SKIP] {skill_name} - no name in frontmatter")
            continue

        # Create stub content
        stub_content = create_stub(skill_name, name, description)

        # Write to each target directory
        for target_dir in TARGET_DIRS:
            target_skill_dir = target_dir / skill_name
            target_skill_dir.mkdir(parents=True, exist_ok=True)

            target_path = target_skill_dir / "SKILL.md"
            target_path.write_text(stub_content, encoding="utf-8")

            print(f"  [OK] {skill_name} -> {target_dir}")

        skill_count += 1

    print(f"\nSync complete!")
    print(f"Skills synced: {skill_count}")
    return skill_count


if __name__ == "__main__":
    sync_skills()
