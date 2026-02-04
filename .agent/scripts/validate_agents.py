#!/usr/bin/env python3
"""Validate agent manifests against schemas."""

import json
import sys
from pathlib import Path

import yaml
from jsonschema import ValidationError, validate
from jsonschema.exceptions import SchemaError


def validate_agent(agent_path, schema, agent_type):
    """Validate a single agent YAML against a loaded schema (UTF-8)."""
    try:
        with open(agent_path, encoding="utf-8") as f:
            agent = yaml.safe_load(f)

        try:
            validate(instance=agent, schema=schema)
            print(f"OK {agent_path.name}: Valid {agent_type}")
            return True
        except ValidationError as e:
            print(f"ERR {agent_path.name}: {e.message}")
            return False
    except (OSError, IOError) as e:
        print(f"ERR {agent_path.name}: Error reading file: {e}")
        return False
    except yaml.YAMLError as e:
        print(f"ERR {agent_path.name}: Invalid YAML: {e}")
        return False
    except SchemaError as e:
        print(f"ERR {agent_path.name}: Invalid JSON schema: {e}")
        return False


def main():
    # Resolve paths relative to repo root to support execution from any CWD
    repo_root = Path(__file__).resolve().parents[2]
    agents_dir = repo_root / ".agent" / "agents"
    schemas_dir = repo_root / ".agent" / "schemas"
    skills_dir = repo_root / ".agent" / "skills"

    if not agents_dir.exists():
        print("Error: .agent/agents directory not found")
        return 1

    if not schemas_dir.exists():
        print("Error: .agent/schemas directory not found")
        return 1
    if not skills_dir.exists():
        print("Error: .agent/skills directory not found")
        return 1
    # Get files to validate from arguments, or all files if none provided
    files_to_validate = []
    if len(sys.argv) > 1:
        # Validate only specified files (from pre-commit)
        files_to_validate = [
            Path(arg) if Path(arg).is_absolute() else (repo_root / Path(arg))
            for arg in sys.argv[1:]
        ]
    else:
        # Validate all agent files
        files_to_validate = sorted(agents_dir.glob("*.yaml"))

    all_valid = True

    # Pre-load schemas (UTF-8)
    orchestrator_schema = None
    subagent_schema = None

    orchestrator_schema_path = schemas_dir / "orchestrator.schema.json"
    subagent_schema_path = schemas_dir / "subagent.schema.json"

    if orchestrator_schema_path.exists():
        with open(orchestrator_schema_path, encoding="utf-8") as f:
            orchestrator_schema = json.load(f)
    else:
        print(f"Warning: orchestrator schema not found at {orchestrator_schema_path}")

    if subagent_schema_path.exists():
        with open(subagent_schema_path, encoding="utf-8") as f:
            subagent_schema = json.load(f)
    else:
        print(f"Warning: subagent schema not found at {subagent_schema_path}")

    # Helper: discover available skills (directories that contain SKILL.md)
    available_skills = set()
    if skills_dir.exists():
        for skill_dir in sorted(skills_dir.iterdir()):
            if skill_dir.is_dir() and (skill_dir / "SKILL.md").exists():
                available_skills.add(skill_dir.name)

    # Separate orchestrator and sub-agent files for two-phase validation
    orchestrator_files = [
        Path(f)
        for f in files_to_validate
        if Path(f).name.lower() == "orchestrator.yaml"
    ]
    subagent_files = [
        Path(f)
        for f in files_to_validate
        if Path(f).name.lower() != "orchestrator.yaml"
    ]

    # If we're validating an orchestrator (e.g., pre-commit passes only that file),
    # make sure we also consider all existing sub-agent manifests in the repo so
    # cross-checks don't falsely fail due to an empty valid_subagent_names set.
    if orchestrator_files:
        for agent_manifest in sorted(agents_dir.glob("*.yaml")):
            if agent_manifest.name.lower() == "orchestrator.yaml":
                continue
            if agent_manifest not in subagent_files:
                subagent_files.append(agent_manifest)

    # Phase 1: validate sub-agents and collect valid names
    valid_subagent_names = set()
    for agent_file in subagent_files:
        agent_path = Path(agent_file)
        if not agent_path.exists():
            print(f"ERR {agent_file}: File not found")
            all_valid = False
            continue

        if subagent_schema is None:
            # If schema missing, skip schema validation but cannot ensure
            # correctness
            print(
                (
                    "Warning: subagent schema missing; skipping schema "
                    f"validation for {agent_path.name}"
                )
            )
        else:
            print(f"\nValidating {agent_path.name}...")
            if not validate_agent(agent_path, subagent_schema, "sub-agent"):
                all_valid = False
                continue

        # If schema validation passed (or skipped), perform cross-checks
        try:
            with open(agent_path, encoding="utf-8") as f:
                agent_data = yaml.safe_load(f) or {}
        except (OSError, IOError, yaml.YAMLError) as e:
            print(f"ERR {agent_path.name}: Unable to parse YAML for cross-checks: {e}")
            all_valid = False
            continue

        name = agent_data.get("name")
        if isinstance(name, str) and name:
            valid_subagent_names.add(name)

        # Verify each referenced skill exists as a directory with SKILL.md
        skills = agent_data.get("skills", [])
        missing_skills = [s for s in skills if s not in available_skills]
        if missing_skills:
            for s in missing_skills:
                print(
                    (
                        f"ERR {agent_path.name}: references missing skill '{s}' "
                        f"(expected .agent/skills/{s}/SKILL.md)"
                    )
                )
            all_valid = False

    # Phase 2: validate orchestrator(s) and cross-reference sub-agents
    for agent_file in orchestrator_files:
        agent_path = Path(agent_file)
        if not agent_path.exists():
            print(f"ERR {agent_file}: File not found")
            all_valid = False
            continue

        if orchestrator_schema is None:
            print(
                (
                    "Warning: orchestrator schema missing; skipping schema "
                    f"validation for {agent_path.name}"
                )
            )
        else:
            print("\nValidating orchestrator...")
            if not validate_agent(agent_path, orchestrator_schema, "orchestrator"):
                all_valid = False
                # Even if schema fails, still attempt cross-checks
                # for helpful output

        try:
            with open(agent_path, encoding="utf-8") as f:
                orch = yaml.safe_load(f) or {}
        except (OSError, IOError, yaml.YAMLError) as e:
            print(f"ERR {agent_path.name}: Unable to parse YAML for cross-checks: {e}")
            all_valid = False
            continue

        subagents_list = orch.get("subagents", []) or []
        missing_agents = [a for a in subagents_list if a not in valid_subagent_names]
        if missing_agents:
            for a in missing_agents:
                print(
                    (
                        f"ERR {agent_path.name}: references missing sub-agent "
                        f"'{a}' (no valid manifest found)"
                    )
                )
            all_valid = False

        # Validate routing.to values are in subagents list
        routing = orch.get("routing", {}) or {}
        for rule in routing.get("rules", []) or []:
            to_agent = rule.get("to")
            if to_agent and to_agent not in subagents_list:
                print(
                    (
                        f"ERR {agent_path.name}: routing rule targets "
                        f"'{to_agent}' which is not listed in 'subagents'"
                    )
                )
                all_valid = False

        # If a fallback is provided (either top-level or in routing),
        # ensure it's in subagents
        fallback = orch.get("fallback") or routing.get("fallback")
        if fallback and fallback not in subagents_list:
            print(
                (
                    f"ERR {agent_path.name}: fallback '{fallback}' is not listed "
                    "in 'subagents'"
                )
            )
            all_valid = False

    if all_valid:
        print("\nAll agent manifests are valid")
    else:
        print("\nSome agent manifests have validation errors")
    return 0 if all_valid else 1


if __name__ == "__main__":
    sys.exit(main())
