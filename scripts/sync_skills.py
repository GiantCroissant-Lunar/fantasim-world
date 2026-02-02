#!/usr/bin/env python3
"""
Sync agent configuration from .agent/ to tool-specific directories.

Reads adapter configs from .agent/adapters/<tool>/config.yaml to determine
how to sync content for each tool. Falls back to default behavior if no
adapter config exists.

Syncs:
- Skills: .agent/skills/ → .claude/skills/, .cline/skills/, .codex/skills/, .cursor/skills/, etc.
- Rules: .agent/rules/ → .claude/rules/, .clinerules/, .cursor/rules/, .windsurf/rules/, AGENTS.md, GEMINI.md
- Commands/Workflows: .agent/commands/ → .cursor/commands/, .gemini/commands/, .windsurf/workflows/, .clinerules/workflows/
- Hooks: .agent/hooks/ → .clinerules/hooks/ (Cline hooks)

Adapter configs can override default behavior:
- rules.strategy: "import" (use @imports), "copy" (full content), "directory" (stub files)
- skills.strategy: "copy_full" (full content), "stub" (pointer files)
- cleanup: list of paths to remove (legacy/deprecated)

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
import shutil
from pathlib import Path
from typing import Any, TypeVar

try:
    import yaml
    HAS_YAML = True
except ImportError:
    HAS_YAML = False

# =============================================================================
# Configuration
# =============================================================================

AGENT_DIR = Path(".agent")
ADAPTERS_DIR = AGENT_DIR / "adapters"

# Cache for loaded adapter configs
_adapter_configs: dict[str, dict[str, Any]] = {}

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
    Path(".roo/skills"),
    Path(".windsurf/skills"),
]

# Rules targets (Cline uses .clinerules/ root, others use subdirectories)
RULES_SOURCE = AGENT_DIR / "rules"
RULES_TARGETS = {
    "claude": Path(".claude/rules"),
    "cline": Path(".clinerules"),
    "cursor": Path(".cursor/rules"),
    "kilocode": Path(".kilocode/rules"),
    "roo": Path(".roo/rules"),
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
    "roo": Path(".roo/commands"),
    "windsurf": Path(".windsurf/workflows"),
}

# Hooks targets
HOOKS_SOURCE = AGENT_DIR / "hooks"
HOOKS_TARGETS = {
    "cline": Path(".clinerules/hooks"),
}


# =============================================================================
# Adapter Config Loading
# =============================================================================

def load_adapter_config(tool: str) -> dict[str, Any]:
    """Load adapter config for a tool from .agent/adapters/<tool>/config.yaml."""
    if tool in _adapter_configs:
        return _adapter_configs[tool]

    config_path = ADAPTERS_DIR / tool / "config.yaml"
    config: dict[str, Any] = {}

    if config_path.exists() and HAS_YAML:
        try:
            with open(config_path, encoding="utf-8") as f:
                config = yaml.safe_load(f) or {}
            print(f"  [CONFIG] Loaded adapter config for {tool}")
        except Exception as e:
            print(f"  [WARN] Failed to load {config_path}: {e}")

    _adapter_configs[tool] = config
    return config


def get_skills_strategy(tool: str) -> str:
    """Get skills sync strategy for a tool. Default is 'stub'."""
    config = load_adapter_config(tool)
    return config.get("skills", {}).get("strategy", "stub")


def get_rules_strategy(tool: str) -> str:
    """Get rules sync strategy for a tool. Default is 'directory'."""
    config = load_adapter_config(tool)
    return config.get("rules", {}).get("strategy", "directory")


def get_rules_extension(tool: str) -> str:
    """Get rule file extension for a tool. Default is '.md'."""
    config = load_adapter_config(tool)
    return config.get("rules", {}).get("extension", ".md")


def get_cleanup_paths(tool: str) -> list[str]:
    """Get list of paths to clean up for a tool."""
    config = load_adapter_config(tool)
    return config.get("cleanup", [])


def run_cleanup(tool: str) -> None:
    """Run cleanup for deprecated/legacy paths specified in adapter config."""
    cleanup_paths = get_cleanup_paths(tool)
    for path_str in cleanup_paths:
        path = Path(path_str)
        if path.exists():
            print(f"  [CLEANUP] Removing {path}")
            if path.is_dir():
                shutil.rmtree(path)
            else:
                path.unlink()


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


def clean_files_in_dir(path: Path, pattern: str) -> None:
    if not path.exists():
        return
    for item in path.glob(pattern):
        if item.is_file():
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

    # Determine strategy for each tool based on target directory name
    tool_strategies: dict[str, str] = {}
    for target_dir in SKILLS_TARGETS:
        # Extract tool name from path (e.g., ".claude/skills" -> "claude")
        tool = target_dir.parts[0].lstrip(".")
        tool_strategies[str(target_dir)] = get_skills_strategy(tool)

    # Ensure target directories exist and clean them
    for target_dir in SKILLS_TARGETS:
        ensure_dir(target_dir)
        clean_dir(target_dir)
        strategy = tool_strategies.get(str(target_dir), "stub")
        print(f"  -> {target_dir} (strategy: {strategy})")

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

        # Write to each target directory based on strategy
        for target_dir in SKILLS_TARGETS:
            strategy = tool_strategies.get(str(target_dir), "stub")
            target_skill_dir = target_dir / skill_name
            ensure_dir(target_skill_dir)

            target_path = target_skill_dir / "SKILL.md"

            if strategy == "copy_full":
                # Copy full content from source
                target_path.write_text(content, encoding="utf-8")
                # Also copy any additional files in the skill directory
                for extra_file in skill_dir.iterdir():
                    if extra_file.is_file() and extra_file.name != "SKILL.md":
                        target_extra = target_skill_dir / extra_file.name
                        target_extra.write_text(
                            extra_file.read_text(encoding="utf-8"),
                            encoding="utf-8"
                        )
            else:
                # Create stub content (default)
                stub_content = create_skill_stub(skill_name, name, description)
                target_path.write_text(stub_content, encoding="utf-8")

            print(f"  [OK] {skill_name} -> {target_dir}")

        skill_count += 1

    return skill_count


def _filter_skills_targets(tools: set[str] | None) -> list[Path]:
    if not tools:
        return SKILLS_TARGETS
    return [
        target_dir
        for target_dir in SKILLS_TARGETS
        if target_dir.parts and target_dir.parts[0].lstrip(".") in tools
    ]


def sync_skills_for_tools(tools: set[str] | None) -> int:
    global SKILLS_TARGETS
    original_targets = SKILLS_TARGETS
    try:
        SKILLS_TARGETS = _filter_skills_targets(tools)
        return sync_skills()
    finally:
        SKILLS_TARGETS = original_targets


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
        strategy = get_rules_strategy(tool)

        # Skip directory-based rules if strategy is "import" (uses @imports in main file)
        if strategy == "import":
            print(f"  -> {target_dir} [SKIP - using import strategy]")
            # Run cleanup for this tool (removes legacy directory if exists)
            run_cleanup(tool)
            continue

        ensure_dir(target_dir)
        ext = get_rules_extension(tool)
        # Important: only remove rule files, not subdirectories (e.g., .clinerules/workflows)
        clean_files_in_dir(target_dir, f"*{ext}")
        print(f"  -> {target_dir} (strategy: {strategy})")

        # Determine depth for relative paths
        depth = len(target_dir.parts)

        for filename, name, description in rules_data:
            stub_content = create_rule_stub(filename, name, description, depth)

            target_name = Path(filename).stem + ext
            target_path = target_dir / target_name
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
    """Create an MD stub for Gemini commands."""
    return f"""---
name: {cmd_name}
description: {description}
source: .agent/commands/{cmd_name}.md
---

# {cmd_name}

**This is a pointer file. Read the full command from the source.**

Source: [.agent/commands/{cmd_name}.md](../../.agent/commands/{cmd_name}.md)

When this command is invoked, read and follow the complete instructions at:
`.agent/commands/{cmd_name}.md`
"""


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
    if "gemini" in COMMANDS_TARGETS:
        gemini_target = COMMANDS_TARGETS["gemini"]
        ensure_dir(gemini_target)
        clean_dir(gemini_target)
        print(f"  -> {gemini_target}")

        for stem, filename, description in commands_data:
            stub_content = create_gemini_command_stub(stem, description)
            target_path = gemini_target / f"{stem}.md"
            target_path.write_text(stub_content, encoding="utf-8")
            print(f"  [OK] {filename} -> {target_path}")
            cmd_count += 1

    # Sync to Cline workflows (MD format in .clinerules/workflows/)
    if "cline" in COMMANDS_TARGETS:
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
    if "cursor" in COMMANDS_TARGETS:
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
    if "windsurf" in COMMANDS_TARGETS:
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

    # Sync to Roo commands (MD format in .roo/commands/)
    if "roo" in COMMANDS_TARGETS:
        roo_target = COMMANDS_TARGETS["roo"]
        ensure_dir(roo_target)
        clean_dir(roo_target)
        print(f"  -> {roo_target}")

        depth = len(roo_target.parts)
        for stem, filename, description in commands_data:
            stub_content = create_workflow_stub(stem, description, depth)
            target_path = roo_target / filename
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


T = TypeVar("T")


def _filter_map_targets(targets: dict[str, T], tools: set[str] | None) -> dict[str, T]:
    if not tools:
        return targets
    return {k: v for k, v in targets.items() if k in tools}


def sync_rules_for_tools(tools: set[str] | None) -> int:
    global RULES_TARGETS, RULES_CONCAT_TARGETS
    original_targets = RULES_TARGETS
    original_concat = RULES_CONCAT_TARGETS
    try:
        RULES_TARGETS = _filter_map_targets(RULES_TARGETS, tools)
        RULES_CONCAT_TARGETS = _filter_map_targets(RULES_CONCAT_TARGETS, tools)
        return sync_rules()
    finally:
        RULES_TARGETS = original_targets
        RULES_CONCAT_TARGETS = original_concat


def sync_commands_for_tools(tools: set[str] | None) -> int:
    global COMMANDS_TARGETS
    original_targets = COMMANDS_TARGETS
    try:
        COMMANDS_TARGETS = _filter_map_targets(COMMANDS_TARGETS, tools)
        return sync_commands()
    finally:
        COMMANDS_TARGETS = original_targets


def sync_hooks_for_tools(tools: set[str] | None) -> int:
    global HOOKS_TARGETS
    original_targets = HOOKS_TARGETS
    try:
        HOOKS_TARGETS = _filter_map_targets(HOOKS_TARGETS, tools)
        return sync_hooks()
    finally:
        HOOKS_TARGETS = original_targets


# =============================================================================
# Main
# =============================================================================

def run_all_cleanups() -> None:
    """Run cleanup for all tools with adapter configs."""
    if not ADAPTERS_DIR.exists():
        return

    print("\nRunning cleanups from adapter configs...")
    for adapter_dir in ADAPTERS_DIR.iterdir():
        if adapter_dir.is_dir():
            tool = adapter_dir.name
            cleanup_paths = get_cleanup_paths(tool)
            if cleanup_paths:
                run_cleanup(tool)


def main():
    parser = argparse.ArgumentParser(description="Sync agent configuration to tool-specific directories")
    parser.add_argument("--skills", action="store_true", help="Sync skills only")
    parser.add_argument("--rules", action="store_true", help="Sync rules only")
    parser.add_argument("--commands", action="store_true", help="Sync commands/workflows only")
    parser.add_argument("--hooks", action="store_true", help="Sync hooks only")
    parser.add_argument("--cleanup", action="store_true", help="Run cleanup only")
    parser.add_argument(
        "--tools",
        type=str,
        default="",
        help="Comma-separated list of tools to target (currently used for --skills). Example: opencode,windsurf",
    )
    args = parser.parse_args()

    # If no specific flag, sync everything
    sync_all = not (args.skills or args.rules or args.commands or args.hooks or args.cleanup)

    total = 0

    if args.cleanup:
        run_all_cleanups()
        return

    tools = {t.strip() for t in args.tools.split(",") if t.strip()} or None

    if sync_all or args.skills:
        total += sync_skills_for_tools(tools)

    if sync_all or args.rules:
        total += sync_rules_for_tools(tools)

    if sync_all or args.commands:
        total += sync_commands_for_tools(tools)

    if sync_all or args.hooks:
        total += sync_hooks_for_tools(tools)

    # Run cleanups at the end
    if sync_all:
        run_all_cleanups()

    print(f"\nSync complete!")
    print(f"Total items synced: {total}")


if __name__ == "__main__":
    main()
