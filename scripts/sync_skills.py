#!/usr/bin/env python3
"""
Sync agent configuration from .agent/ to tool-specific directories.

Reads all configuration from .agent/ and creates pointer/stub files
in tool-specific directories that reference the shared source.

Syncs:
- Skills: .agent/skills/ → .claude/skills/, .cline/skills/, .codex/skills/, .cursor/skills/, etc.
- Rules: .agent/rules/ → .claude/rules/, .clinerules/, .cursor/rules/, .windsurf/rules/, AGENTS.md, GEMINI.md
- Commands/Workflows: .agent/commands/ → .cursor/commands/, .gemini/commands/, .windsurf/workflows/, .clinerules/workflows/
- Hooks: .agent/hooks/ → .clinerules/hooks/ (Cline hooks)

Note: Cursor hooks use JSON config (.cursor/hooks.json) - not synced automatically.

Usage:
    python scripts/sync_skills.py            # Sync everything
    python scripts/sync_skills.py --skills   # Sync skills only
    python scripts/sync_skills.py --rules    # Sync rules only
    python scripts/sync_skills.py --commands # Sync commands/workflows only
    python scripts/sync_skills.py --hooks    # Sync hooks only
    task agent:sync                          # Via Taskfile
"""

import argparse
import re
from pathlib import Path

# =============================================================================
# Configuration
# =============================================================================

AGENT_DIR = Path(".agent")

# Skills targets
SKILLS_SOURCE = AGENT_DIR / "skills"
SKILLS_TARGETS = [
    Path(".claude/skills"),
    Path(".cline/skills"),
    Path(".codex/skills"),
    Path(".cursor/skills"),
    Path(".gemini/skills"),
    Path(".kilocode/skills"),
    Path(".opencode/skills"),
    Path(".windsurf/skills"),
]

# Rules targets (Cline uses .clinerules/ root, others use subdirectories)
RULES_SOURCE = AGENT_DIR / "rules"
RULES_TARGETS = {
    "claude": Path(".claude/rules"),
    "cline": Path(".clinerules"),
    "cursor": Path(".cursor/rules"),
    "windsurf": Path(".windsurf/rules"),
}
# These get concatenated pointer files (with @imports where supported)
RULES_CONCAT_TARGETS = {
    "claude": Path("CLAUDE.md"),
    "codex": Path("AGENTS.md"),
    "gemini": Path("GEMINI.md"),
}

# Memory/reflections source
MEMORY_SOURCE = AGENT_DIR / "memory" / "reflections"

# Commands/Workflows targets
COMMANDS_SOURCE = AGENT_DIR / "commands"
COMMANDS_TARGETS = {
    "cline": Path(".clinerules/workflows"),
    "cursor": Path(".cursor/commands"),
    "gemini": Path(".gemini/commands"),
    "windsurf": Path(".windsurf/workflows"),
}

# Hooks targets
HOOKS_SOURCE = AGENT_DIR / "hooks"
HOOKS_TARGETS = {
    "cline": Path(".clinerules/hooks"),
}


# =============================================================================
# Utilities
# =============================================================================

def extract_frontmatter(content: str) -> dict[str, str]:
    """Extract YAML frontmatter from markdown content."""
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


def ensure_dir(path: Path) -> None:
    """Ensure directory exists."""
    path.mkdir(parents=True, exist_ok=True)


def clean_dir(path: Path) -> None:
    """Remove all contents from directory."""
    if not path.exists():
        return
    import shutil
    for item in path.iterdir():
        if item.is_dir():
            shutil.rmtree(item)
        else:
            item.unlink()


# =============================================================================
# Skills Sync
# =============================================================================

def create_skill_stub(skill_name: str, name: str, description: str) -> str:
    """Create stub SKILL.md content for skills."""
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
    print(f"Syncing skills from {SKILLS_SOURCE}...")

    if not SKILLS_SOURCE.exists():
        print(f"  [SKIP] Source directory not found: {SKILLS_SOURCE}")
        return 0

    # Ensure target directories exist and clean them
    for target_dir in SKILLS_TARGETS:
        ensure_dir(target_dir)
        clean_dir(target_dir)
        print(f"  -> {target_dir}")

    # Process each skill directory
    skill_count = 0
    for skill_dir in sorted(SKILLS_SOURCE.iterdir()):
        if not skill_dir.is_dir():
            continue

        skill_name = skill_dir.name
        source_path = skill_dir / "SKILL.md"

        if not source_path.exists():
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
        stub_content = create_skill_stub(skill_name, name, description)

        # Write to each target directory
        for target_dir in SKILLS_TARGETS:
            target_skill_dir = target_dir / skill_name
            ensure_dir(target_skill_dir)

            target_path = target_skill_dir / "SKILL.md"
            target_path.write_text(stub_content, encoding="utf-8")

            print(f"  [OK] {skill_name} -> {target_dir}")

        skill_count += 1

    return skill_count


# =============================================================================
# Rules Sync
# =============================================================================

def create_rule_stub(rule_name: str, name: str, description: str, depth: int = 3) -> str:
    """Create stub rule content that points to source."""
    relative_prefix = "../" * depth
    relative_path = f"{relative_prefix}.agent/rules/{rule_name}"

    return f"""---
name: {name}
description: {description}
source: .agent/rules/{rule_name}
---

# {to_title_case(name.replace('-', ' '))}

**This is a pointer file. Read the full rule from the source.**

Source: [{relative_path}]({relative_path})

When applying this rule, read and follow the complete instructions at:
`.agent/rules/{rule_name}`
"""


def create_concat_rules_stub(
    tool: str,
    rules: list[tuple[str, str, str]],
    reflections: list[tuple[str, str]] | None = None,
) -> str:
    """Create a concatenated pointer file for CLAUDE.md / AGENTS.md / GEMINI.md.

    Args:
        tool: Target tool name (claude, codex, gemini)
        rules: List of (filename, name, description) tuples
        reflections: List of (filename, summary) tuples from memory
    """
    # Tools that support @import syntax
    supports_imports = tool in ("claude", "gemini")

    lines = [
        "# Project Instructions",
        "",
        "**This file is auto-generated. Edit sources in `.agent/` instead.**",
        "",
        "Sources:",
        "- Rules: `.agent/rules/`",
        "- Reflections: `.agent/memory/reflections/`",
        "",
    ]

    # Rules section
    lines.append("## Rules")
    lines.append("")

    for filename, name, description in rules:
        if supports_imports:
            # Claude and Gemini support @imports
            lines.append(f"@.agent/rules/{filename}")
        else:
            # Codex: include instruction to read source
            lines.append(f"### {to_title_case(name)}")
            lines.append("")
            lines.append(f"{description}")
            lines.append("")
            lines.append(f"Read full rule: `.agent/rules/{filename}`")
        lines.append("")

    # Reflections section (learnings from completed features)
    if reflections:
        lines.append("## Learnings from Past Work")
        lines.append("")
        lines.append("The following reflections capture learnings from completed features.")
        lines.append("")

        for filename, summary in reflections:
            if supports_imports:
                lines.append(f"@.agent/memory/reflections/{filename}")
            else:
                lines.append(f"### {filename.replace('.md', '').replace('-', ' ').title()}")
                lines.append("")
                lines.append(f"{summary}")
                lines.append("")
                lines.append(f"Read full reflection: `.agent/memory/reflections/{filename}`")
            lines.append("")
    else:
        lines.append("## Learnings")
        lines.append("")
        lines.append("No reflections yet. Use `@reflect` after completing features to capture learnings.")
        lines.append("")

    return "\n".join(lines)


def collect_reflections() -> list[tuple[str, str]]:
    """Collect reflections from memory, returning (filename, summary) tuples."""
    reflections: list[tuple[str, str]] = []

    if not MEMORY_SOURCE.exists():
        return reflections

    for ref_file in sorted(MEMORY_SOURCE.glob("*.md")):
        # Skip index file
        if ref_file.name == "index.md":
            continue

        try:
            content = ref_file.read_text(encoding="utf-8")
            # Extract summary (first paragraph after "## Summary" or first non-header line)
            summary = ""
            in_summary = False
            for line in content.split("\n"):
                if line.startswith("## Summary"):
                    in_summary = True
                    continue
                if in_summary and line.strip() and not line.startswith("#"):
                    summary = line.strip()
                    break
                if line.startswith("##") and in_summary:
                    break

            if not summary:
                # Fallback: use first non-empty, non-header line
                for line in content.split("\n"):
                    if line.strip() and not line.startswith("#") and not line.startswith("---"):
                        summary = line.strip()[:100]
                        break

            reflections.append((ref_file.name, summary))
        except Exception:
            pass

    return reflections


def sync_rules() -> int:
    """Sync all rules from source to target directories."""
    print(f"Syncing rules from {RULES_SOURCE}...")

    if not RULES_SOURCE.exists():
        print(f"  [SKIP] Source directory not found: {RULES_SOURCE}")
        return 0

    # Collect all rules with metadata
    rules_data: list[tuple[str, str, str]] = []  # (filename, name, description)

    for rule_file in sorted(RULES_SOURCE.glob("*.md")):
        content = rule_file.read_text(encoding="utf-8")
        frontmatter = extract_frontmatter(content)

        name = frontmatter.get("name", rule_file.stem)
        description = frontmatter.get("description", "")

        rules_data.append((rule_file.name, name, description))

    if not rules_data:
        print("  [SKIP] No rules found")
        return 0

    # Collect reflections from memory
    reflections_data = collect_reflections()
    if reflections_data:
        print(f"  Found {len(reflections_data)} reflections in memory")

    rule_count = 0

    # Sync to directory-based targets (Claude, Cline, Cursor, Windsurf)
    for tool, target_dir in RULES_TARGETS.items():
        ensure_dir(target_dir)
        clean_dir(target_dir)
        print(f"  -> {target_dir}")

        # Determine depth for relative paths
        depth = len(target_dir.parts)

        for filename, name, description in rules_data:
            stub_content = create_rule_stub(filename, name, description, depth)

            target_path = target_dir / filename
            target_path.write_text(stub_content, encoding="utf-8")

            print(f"  [OK] {filename} -> {target_dir}")
            rule_count += 1

    # Sync to concatenated targets (CLAUDE.md, AGENTS.md, GEMINI.md)
    for tool, target_file in RULES_CONCAT_TARGETS.items():
        stub_content = create_concat_rules_stub(tool, rules_data, reflections_data)
        target_file.write_text(stub_content, encoding="utf-8")
        print(f"  [OK] {len(rules_data)} rules + {len(reflections_data)} reflections -> {target_file}")

    return rule_count


# =============================================================================
# Commands/Workflows Sync
# =============================================================================

def create_gemini_command_stub(cmd_name: str, description: str) -> str:
    """Create a TOML stub for Gemini commands."""
    # Gemini commands use TOML format
    return f'''# Auto-generated pointer to .agent/commands/{cmd_name}.md
# Edit the source file, not this stub.

[command]
name = "{cmd_name}"
description = "{description}"

[command.prompt]
# Read the full command from the source:
# .agent/commands/{cmd_name}.md
text = """
This command is defined in .agent/commands/{cmd_name}.md

Please read and execute the instructions from that file.
"""
'''


def create_workflow_stub(cmd_name: str, description: str, depth: int = 2) -> str:
    """Create a workflow stub for Windsurf and Cline."""
    relative_prefix = "../" * depth
    relative_path = f"{relative_prefix}.agent/commands/{cmd_name}.md"

    return f"""---
name: {cmd_name}
description: {description}
source: .agent/commands/{cmd_name}.md
---

# {to_title_case(cmd_name.replace('.', ' ').replace('-', ' '))}

**This is a pointer file. Read the full workflow from the source.**

Source: [{relative_path}]({relative_path})

## Instructions

When this workflow is invoked, read and follow the complete instructions at:
`.agent/commands/{cmd_name}.md`
"""


def sync_commands() -> int:
    """Sync all commands from source to target directories."""
    print(f"Syncing commands/workflows from {COMMANDS_SOURCE}...")

    if not COMMANDS_SOURCE.exists():
        print(f"  [SKIP] Source directory not found: {COMMANDS_SOURCE}")
        return 0

    # Collect all commands with metadata
    commands_data: list[tuple[str, str, str]] = []  # (stem, filename, description)

    for cmd_file in sorted(COMMANDS_SOURCE.glob("*.md")):
        content = cmd_file.read_text(encoding="utf-8")
        frontmatter = extract_frontmatter(content)

        description = frontmatter.get("description", "")

        commands_data.append((cmd_file.stem, cmd_file.name, description))

    if not commands_data:
        print("  [SKIP] No commands found")
        return 0

    cmd_count = 0

    # Sync to Gemini commands (TOML format)
    gemini_target = COMMANDS_TARGETS["gemini"]
    ensure_dir(gemini_target)
    clean_dir(gemini_target)
    print(f"  -> {gemini_target}")

    for stem, filename, description in commands_data:
        stub_content = create_gemini_command_stub(stem, description)
        target_path = gemini_target / f"{stem}.toml"
        target_path.write_text(stub_content, encoding="utf-8")
        print(f"  [OK] {filename} -> {target_path}")
        cmd_count += 1

    # Sync to Cline workflows (MD format in .clinerules/workflows/)
    cline_target = COMMANDS_TARGETS["cline"]
    ensure_dir(cline_target)
    clean_dir(cline_target)
    print(f"  -> {cline_target}")

    depth = len(cline_target.parts)
    for stem, filename, description in commands_data:
        stub_content = create_workflow_stub(stem, description, depth)
        target_path = cline_target / filename
        target_path.write_text(stub_content, encoding="utf-8")
        print(f"  [OK] {filename} -> {target_path}")
        cmd_count += 1

    # Sync to Cursor commands (MD format in .cursor/commands/)
    cursor_target = COMMANDS_TARGETS["cursor"]
    ensure_dir(cursor_target)
    clean_dir(cursor_target)
    print(f"  -> {cursor_target}")

    depth = len(cursor_target.parts)
    for stem, filename, description in commands_data:
        stub_content = create_workflow_stub(stem, description, depth)
        target_path = cursor_target / filename
        target_path.write_text(stub_content, encoding="utf-8")
        print(f"  [OK] {filename} -> {target_path}")
        cmd_count += 1

    # Sync to Windsurf workflows (MD format)
    windsurf_target = COMMANDS_TARGETS["windsurf"]
    ensure_dir(windsurf_target)
    clean_dir(windsurf_target)
    print(f"  -> {windsurf_target}")

    depth = len(windsurf_target.parts)
    for stem, filename, description in commands_data:
        stub_content = create_workflow_stub(stem, description, depth)
        target_path = windsurf_target / filename
        target_path.write_text(stub_content, encoding="utf-8")
        print(f"  [OK] {filename} -> {target_path}")
        cmd_count += 1

    return cmd_count


# =============================================================================
# Hooks Sync
# =============================================================================

def create_hook_stub(hook_name: str, description: str, depth: int = 2) -> str:
    """Create a hook stub that points to the source."""
    relative_prefix = "../" * depth
    relative_path = f"{relative_prefix}.agent/hooks/{hook_name}"

    return f"""#!/usr/bin/env bash
# Auto-generated pointer to .agent/hooks/{hook_name}
# Edit the source file, not this stub.
#
# Description: {description}
# Source: {relative_path}
#
# This stub executes the source hook script.
# Cline hooks receive JSON via stdin and return JSON to control execution.

exec "$(dirname "$0")/{relative_path}" "$@"
"""


def sync_hooks() -> int:
    """Sync all hooks from source to target directories."""
    print(f"Syncing hooks from {HOOKS_SOURCE}...")

    if not HOOKS_SOURCE.exists():
        print(f"  [SKIP] Source directory not found: {HOOKS_SOURCE}")
        return 0

    # Collect all hooks
    hooks_data: list[tuple[str, str]] = []  # (filename, description)

    for hook_file in sorted(HOOKS_SOURCE.iterdir()):
        if hook_file.is_file():
            # Try to extract description from file if it's a script with comments
            description = ""
            try:
                content = hook_file.read_text(encoding="utf-8")
                # Look for description in comments
                for line in content.split("\n")[:10]:
                    if line.startswith("# Description:"):
                        description = line.replace("# Description:", "").strip()
                        break
            except Exception:
                pass

            hooks_data.append((hook_file.name, description))

    if not hooks_data:
        print("  [SKIP] No hooks found")
        return 0

    hook_count = 0

    # Sync to Cline hooks
    for tool, target_dir in HOOKS_TARGETS.items():
        ensure_dir(target_dir)
        clean_dir(target_dir)
        print(f"  -> {target_dir}")

        depth = len(target_dir.parts)
        for filename, description in hooks_data:
            stub_content = create_hook_stub(filename, description, depth)
            target_path = target_dir / filename
            target_path.write_text(stub_content, encoding="utf-8")
            # Make executable on Unix
            try:
                target_path.chmod(0o755)
            except Exception:
                pass
            print(f"  [OK] {filename} -> {target_dir}")
            hook_count += 1

    return hook_count


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Sync agent configuration to tool-specific directories")
    parser.add_argument("--skills", action="store_true", help="Sync skills only")
    parser.add_argument("--rules", action="store_true", help="Sync rules only")
    parser.add_argument("--commands", action="store_true", help="Sync commands/workflows only")
    parser.add_argument("--hooks", action="store_true", help="Sync hooks only")
    args = parser.parse_args()

    # If no specific flag, sync everything
    sync_all = not (args.skills or args.rules or args.commands or args.hooks)

    total = 0

    if sync_all or args.skills:
        total += sync_skills()

    if sync_all or args.rules:
        total += sync_rules()

    if sync_all or args.commands:
        total += sync_commands()

    if sync_all or args.hooks:
        total += sync_hooks()

    print(f"\nSync complete!")
    print(f"Total items synced: {total}")


if __name__ == "__main__":
    main()
