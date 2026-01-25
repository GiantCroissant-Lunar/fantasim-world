"""Sync OpenCode configuration from .agent/adapters/opencode/ sources.

Generates:
- opencode.json (from permissions.yaml + agents/*.md frontmatter)
- .opencode/ directory (agents, commands, skills)
"""

import json
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

AGENT_OPENCODE_DIR = Path(".agent/adapters/opencode")
AGENT_OPENCODE_AGENTS_DIR = AGENT_OPENCODE_DIR / "agents"
PERMISSIONS_FILE = AGENT_OPENCODE_DIR / "permissions.yaml"
OH_MY_OPENCODE_JSONC = AGENT_OPENCODE_DIR / "oh-my-opencode.jsonc"
OH_MY_OPENCODE_JSON = AGENT_OPENCODE_DIR / "oh-my-opencode.json"

OPENCODE_DIR = Path(".opencode")
OPENCODE_AGENTS_DIR = OPENCODE_DIR / "agents"
OPENCODE_OH_MY_OPENCODE_JSONC = OPENCODE_DIR / "oh-my-opencode.jsonc"
OPENCODE_OH_MY_OPENCODE_JSON = OPENCODE_DIR / "oh-my-opencode.json"
OPENCODE_JSON = Path("opencode.json")


def run_script(path: str) -> None:
    subprocess.run([sys.executable, path], check=True)


def ensure_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        path.write_text(content, encoding="utf-8")


def copy_tree_overwrite(src: Path, dst: Path) -> None:
    if not src.exists():
        return

    if dst.exists():
        shutil.rmtree(dst)

    shutil.copytree(src, dst)


def parse_yaml_value(value: str) -> Any:
    """Parse a simple YAML value (string, bool, number, or dict shorthand)."""
    value = value.strip()
    if value.lower() == "true":
        return True
    if value.lower() == "false":
        return False
    if value.isdigit():
        return int(value)
    try:
        return float(value)
    except ValueError:
        pass
    # Remove quotes if present
    if (value.startswith('"') and value.endswith('"')) or (
        value.startswith("'") and value.endswith("'")
    ):
        return value[1:-1]
    return value


def parse_yaml_frontmatter(content: str) -> tuple[dict[str, Any], str]:
    """Parse YAML frontmatter from markdown content.

    Returns (frontmatter_dict, body_content).
    """
    if not content.startswith("---"):
        return {}, content

    lines = content.split("\n")
    end_idx = -1
    for i, line in enumerate(lines[1:], start=1):
        if line.strip() == "---":
            end_idx = i
            break

    if end_idx == -1:
        return {}, content

    yaml_lines = lines[1:end_idx]
    body = "\n".join(lines[end_idx + 1 :]).strip()

    return parse_simple_yaml(yaml_lines), body


def parse_simple_yaml(lines: list[str]) -> dict[str, Any]:
    """Parse simple YAML (supports nested dicts, no arrays)."""
    result: dict[str, Any] = {}
    stack: list[tuple[int, dict[str, Any]]] = [(-1, result)]

    for line in lines:
        if not line.strip() or line.strip().startswith("#"):
            continue

        # Calculate indentation
        stripped = line.lstrip()
        indent = len(line) - len(stripped)

        # Pop stack to find correct parent
        while stack and stack[-1][0] >= indent:
            stack.pop()

        current_dict = stack[-1][1] if stack else result

        # Parse key: value
        if ":" in stripped:
            key, _, value = stripped.partition(":")
            key = key.strip()
            value = value.strip()

            # Strip quotes from key if present
            if (key.startswith('"') and key.endswith('"')) or (
                key.startswith("'") and key.endswith("'")
            ):
                key = key[1:-1]

            if value:
                # Simple key: value
                current_dict[key] = parse_yaml_value(value)
            else:
                # Nested dict
                current_dict[key] = {}
                stack.append((indent, current_dict[key]))

    return result


def parse_permissions_yaml(path: Path) -> dict[str, Any]:
    """Parse the global permissions.yaml file."""
    if not path.exists():
        return {}

    content = path.read_text(encoding="utf-8")
    lines = content.split("\n")
    return parse_simple_yaml(lines)


def build_agent_config(frontmatter: dict[str, Any]) -> dict[str, Any]:
    """Build agent config from frontmatter, extracting only relevant fields."""
    config: dict[str, Any] = {}

    # Copy non-permission fields that OpenCode supports
    for key in ["description", "mode", "model", "reasoningEffort", "textVerbosity", "maxSteps"]:
        if key in frontmatter:
            config[key] = frontmatter[key]

    # Copy permission if present
    if "permission" in frontmatter:
        config["permission"] = frontmatter["permission"]

    return config


def generate_opencode_json() -> dict[str, Any]:
    """Generate opencode.json from source files."""
    config: dict[str, Any] = {}

    # Load global permissions
    global_perms = parse_permissions_yaml(PERMISSIONS_FILE)

    # Add schema
    if "schema" in global_perms:
        config["$schema"] = global_perms["schema"]
    else:
        config["$schema"] = "https://opencode.ai/config.json"

    # Add global permission block
    if "permission" in global_perms:
        config["permission"] = global_perms["permission"]

    # Load agent configs from markdown files
    agents: dict[str, Any] = {}
    if AGENT_OPENCODE_AGENTS_DIR.exists():
        for agent_file in sorted(AGENT_OPENCODE_AGENTS_DIR.glob("*.md")):
            agent_name = agent_file.stem
            content = agent_file.read_text(encoding="utf-8")
            frontmatter, _ = parse_yaml_frontmatter(content)

            if frontmatter:
                agent_config = build_agent_config(frontmatter)
                if agent_config:
                    agents[agent_name] = agent_config

    if agents:
        config["agent"] = agents

    return config


def main() -> int:
    OPENCODE_DIR.mkdir(parents=True, exist_ok=True)

    # Generate opencode.json from sources
    config = generate_opencode_json()
    OPENCODE_JSON.write_text(
        json.dumps(config, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Generated {OPENCODE_JSON}")

    # Seed minimal OpenCode runtime files
    if AGENT_OPENCODE_DIR.exists():
        package_json = AGENT_OPENCODE_DIR / "package.json"
        if package_json.exists():
            shutil.copy2(package_json, OPENCODE_DIR / "package.json")

        gitignore = AGENT_OPENCODE_DIR / ".gitignore"
        if gitignore.exists():
            shutil.copy2(gitignore, OPENCODE_DIR / ".gitignore")

        if OH_MY_OPENCODE_JSONC.exists():
            shutil.copy2(OH_MY_OPENCODE_JSONC, OPENCODE_OH_MY_OPENCODE_JSONC)
        elif OH_MY_OPENCODE_JSON.exists():
            shutil.copy2(OH_MY_OPENCODE_JSON, OPENCODE_OH_MY_OPENCODE_JSON)

    # Always ensure node_modules is ignored at minimum
    ensure_file(OPENCODE_DIR / ".gitignore", "node_modules/\n")

    # Seed OpenCode agent markdowns (optional, but used by current audits)
    copy_tree_overwrite(AGENT_OPENCODE_AGENTS_DIR, OPENCODE_AGENTS_DIR)

    # Generate commands + skills into .opencode
    run_script("scripts/sync_commands.py")
    subprocess.run(
        [
            sys.executable,
            "scripts/sync_skills.py",
            "--skills",
            "--tools=opencode",
        ],
        check=True,
    )

    print("Synced .opencode (seed + commands + skills)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
