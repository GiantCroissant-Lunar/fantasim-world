#!/usr/bin/env python3
"""
Sync rules from .agent/rules to tool-specific rules directories and files.

Generates:
- .kilocode/rules/*.md     (directory of markdown files)
- .windsurf/rules/*.md     (directory of markdown files)
- .cline/rules/*.md        (directory of markdown files)
- .cursor/rules/*.mdc      (directory of MDC files)
- CLAUDE.md                (single concatenated file)
- AGENTS.md                (single concatenated file with header)

Supports adapter overrides: if .agent/adapters/<tool>/rules/ exists,
those files take precedence over the canonical .agent/rules/ files.

Usage:
    python scripts/sync_rules.py
    task sync-rules
"""

import re
import shutil
from pathlib import Path
from typing import Any

SOURCE_DIR = Path(".agent/rules")
ADAPTERS_DIR = Path(".agent/adapters")

# Tools that use directory-based rules
DIRECTORY_TARGETS = {
    "kilocode": Path(".kilocode/rules"),
    "windsurf": Path(".windsurf/rules"),
    "cline": Path(".cline/rules"),
    "cursor": Path(".cursor/rules"),
}

# File extension per tool (most use .md, cursor uses .mdc)
TOOL_EXTENSIONS = {
    "kilocode": ".md",
    "windsurf": ".md",
    "cline": ".md",
    "cursor": ".mdc",
}

# Single-file targets (concatenated rules)
SINGLE_FILE_TARGETS = {
    "claude": Path("CLAUDE.md"),
    "agents": Path("AGENTS.md"),
}


def parse_frontmatter(content: str) -> tuple[dict[str, Any], str]:
    """Parse YAML frontmatter from markdown content."""
    match = re.match(r"^---\r?\n(.+?)\r?\n---\r?\n?", content, re.DOTALL)
    if not match:
        return {}, content

    frontmatter: dict[str, Any] = {}
    lines = match.group(1).split("\n")
    current_key = None
    current_list: list[str] | None = None

    for line in lines:
        stripped = line.strip()

        # Handle list items (indented lines starting with -)
        if stripped.startswith("- ") and current_key is not None:
            if current_list is None:
                current_list = []
            # Remove quotes if present
            item = stripped[2:].strip().strip('"').strip("'")
            current_list.append(item)
            continue

        # Save previous list if we're moving to a new key
        if current_key is not None and current_list is not None:
            frontmatter[current_key] = current_list
            current_list = None
            current_key = None

        # Handle key: value pairs
        if ":" in stripped and not stripped.startswith("-"):
            key, value = stripped.split(":", 1)
            key = key.strip()
            value = value.strip()

            # Empty value means a list follows
            if not value:
                current_key = key
                current_list = []
                continue

            # Handle boolean values
            if value.lower() == "true":
                value = True
            elif value.lower() == "false":
                value = False
            # Handle numeric values
            elif value.isdigit():
                value = int(value)

            frontmatter[key] = value

    # Don't forget the last list if file ends with one
    if current_key is not None and current_list is not None:
        frontmatter[current_key] = current_list

    body = content[match.end() :]
    return frontmatter, body


def read_rules(source_dir: Path) -> list[tuple[Path, dict[str, Any], str]]:
    """Read all rule files from a directory, sorted by order/name."""
    rules = []
    if not source_dir.exists():
        return rules

    for rule_file in sorted(source_dir.glob("*.md")):
        content = rule_file.read_text(encoding="utf-8")
        frontmatter, body = parse_frontmatter(content)
        rules.append((rule_file, frontmatter, body))

    # Sort by order field if present, then by filename
    rules.sort(key=lambda r: (r[1].get("order", 999), r[0].name))
    return rules


def collect_rules(tool_name: str) -> list[tuple[Path, dict[str, Any], str]]:
    """Collect rules for a tool, with adapter overrides."""
    # Start with canonical rules
    canonical_rules = {r[0].name: r for r in read_rules(SOURCE_DIR)}

    # Override with adapter-specific rules if they exist
    adapter_rules_dir = ADAPTERS_DIR / tool_name / "rules"
    if adapter_rules_dir.exists():
        for rule_file, frontmatter, body in read_rules(adapter_rules_dir):
            canonical_rules[rule_file.name] = (rule_file, frontmatter, body)

    # Return sorted list
    rules = list(canonical_rules.values())
    rules.sort(key=lambda r: (r[1].get("order", 999), r[0].name))
    return rules


def sync_directory_rules(tool_name: str, target_dir: Path) -> int:
    """Sync rules to a directory-based target."""
    target_dir.mkdir(parents=True, exist_ok=True)

    # Get file extension for this tool
    ext = TOOL_EXTENSIONS.get(tool_name, ".md")

    # Clear existing rules
    for existing in target_dir.glob(f"*{ext}"):
        if existing.is_file():
            existing.unlink()

    rules = collect_rules(tool_name)
    print(f"  -> {target_dir}")

    # Check for adapter overrides
    adapter_rules_dir = ADAPTERS_DIR / tool_name / "rules"
    if adapter_rules_dir.exists():
        print(f"     (with adapter overrides from {adapter_rules_dir})")

    for rule_file, frontmatter, body in rules:
        # Use original filename but with correct extension
        target_name = rule_file.stem + ext
        target_path = target_dir / target_name

        # Reconstruct content with frontmatter
        content = format_frontmatter(frontmatter) + body
        target_path.write_text(content, encoding="utf-8")
        print(f"     [OK] {target_name}")

    return len(rules)


def format_frontmatter(frontmatter: dict[str, Any]) -> str:
    """Format frontmatter dict back to YAML string."""
    if not frontmatter:
        return ""

    lines = ["---"]
    for key, value in frontmatter.items():
        if isinstance(value, bool):
            lines.append(f"{key}: {str(value).lower()}")
        elif isinstance(value, list):
            lines.append(f"{key}:")
            for item in value:
                lines.append(f'  - "{item}"')
        else:
            lines.append(f"{key}: {value}")
    lines.append("---")
    lines.append("")
    return "\n".join(lines)


def demote_headings(content: str) -> str:
    """Demote all headings by one level (# -> ##) for concatenated files."""
    lines = content.split("\n")
    result = []
    for line in lines:
        # Only demote lines that start with # (headings)
        if line.startswith("#") and not line.startswith("##"):
            result.append("#" + line)
        else:
            result.append(line)
    return "\n".join(result)


def sync_single_file(tool_name: str, target_path: Path) -> int:
    """Sync rules to a single concatenated file."""
    rules = collect_rules(tool_name)

    if not rules:
        print(f"  -> {target_path} (no rules to sync)")
        return 0

    print(f"  -> {target_path}")

    # Check for adapter overrides
    adapter_rules_dir = ADAPTERS_DIR / tool_name / "rules"
    if adapter_rules_dir.exists():
        print(f"     (with adapter overrides from {adapter_rules_dir})")

    # Build concatenated content
    sections = []

    # Add header for AGENTS.md
    if tool_name == "agents":
        sections.append("# Agent Guide for fantasim-world")

    for rule_file, frontmatter, body in rules:
        # For single files, demote headings and skip frontmatter
        demoted = demote_headings(body.strip())
        sections.append(demoted)
        print(f"     [OK] {rule_file.name}")

    content = "\n\n".join(sections) + "\n"
    target_path.write_text(content, encoding="utf-8")

    return len(rules)


def sync_rules() -> int:
    """Sync all rules from source to all targets."""
    if not SOURCE_DIR.exists():
        print(f"Source directory {SOURCE_DIR} does not exist, skipping")
        return 0

    print(f"Syncing rules from {SOURCE_DIR}...")

    total_count = 0

    # Sync to directory-based targets
    for tool_name, target_dir in DIRECTORY_TARGETS.items():
        count = sync_directory_rules(tool_name, target_dir)
        total_count += count

    # Sync to single-file targets
    for tool_name, target_path in SINGLE_FILE_TARGETS.items():
        count = sync_single_file(tool_name, target_path)
        total_count += count

    print(f"\nSync complete!")
    print(f"Total rule files processed: {total_count}")
    return total_count


if __name__ == "__main__":
    sync_rules()
