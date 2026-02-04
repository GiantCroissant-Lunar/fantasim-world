#!/usr/bin/env python3
"""Validate skill manifests against schema and size limits."""

import json
import sys
from pathlib import Path

import yaml
from jsonschema import ValidationError, validate


def extract_frontmatter(skill_md_path):
    """Extract YAML front-matter from SKILL.md"""
    with open(skill_md_path, encoding="utf-8") as f:
        content = f.read()

    if not content.startswith("---"):
        raise ValueError(f"Missing front-matter in {skill_md_path}")

    parts = content.split("---", 2)
    if len(parts) < 3:
        raise ValueError(f"Invalid front-matter format in {skill_md_path}")

    return yaml.safe_load(parts[1])


def validate_skill(skill_path, schema):
    """Validate a single skill against schema and size limits."""
    skill_md = skill_path / "SKILL.md"

    try:
        # Extract and validate front-matter
        front_matter = extract_frontmatter(skill_md)

        try:
            validate(instance=front_matter, schema=schema)
            print(f"OK {skill_md}: Schema valid")
        except ValidationError as e:
            print(f"ERR {skill_md}: {e.message}")
            return False

        # Check size limits
        with open(skill_md, encoding="utf-8") as f:
            entry_lines = len(f.readlines())

        if entry_lines > 220:
            print(f"ERR {skill_md}: Entry too large ({entry_lines} lines, max 220)")
            return False

        print(f"OK {skill_md}: Size OK ({entry_lines} lines)")

        # Check references (sorted for deterministic order)
        ref_dir = skill_path / "references"
        if ref_dir.exists():
            refs = sorted(ref_dir.glob("*.md"))
            if refs:
                # Check first reference for cold-start budget
                with open(refs[0], encoding="utf-8") as f:
                    ref_lines = len(f.readlines())

                if ref_lines > 320:
                    msg = (
                        f"ERR {refs[0]}: Reference too large "
                        f"({ref_lines} lines, max 320)"
                    )
                    print(msg)
                    return False

                total = entry_lines + ref_lines
                if total > 550:
                    print(f"ERR Cold-start budget exceeded: {total} lines (max 550)")
                    return False

                print(f"OK Cold-start budget OK: {total} lines")

                # Check all other references
                for ref in refs[1:]:
                    with open(ref, encoding="utf-8") as f:
                        ref_lines = len(f.readlines())

                    if ref_lines > 320:
                        msg = (
                            f"ERR {ref}: Reference too large "
                            f"({ref_lines} lines, max 320)"
                        )
                        print(msg)
                        return False

                    print(f"OK {ref}: Size OK ({ref_lines} lines)")

    except OSError as e:
        print(f"ERR {skill_md}: Error reading file: {e}")
        return False
    except Exception as e:
        print(f"ERR {skill_md}: Unexpected error: {e}")
        return False

    return True


def main():
    # Resolve paths relative to repo root to support execution from any CWD
    repo_root = Path(__file__).resolve().parents[2]
    skills_dir = repo_root / ".agent" / "skills"
    schema_path = repo_root / ".agent" / "schemas" / "skill.schema.json"

    if not skills_dir.exists():
        print("Error: .agent/skills directory not found")
        return 1

    if not schema_path.exists():
        print("Error: skill schema not found")
        return 1

    # Pre-load schema
    with open(schema_path, encoding="utf-8") as f:
        schema = json.load(f)

    # Determine which skill directories to validate
    skill_dirs_to_validate = set()

    if len(sys.argv) > 1:
        # Files passed from pre-commit - extract skill directories
        for file_path in sys.argv[1:]:
            path = Path(file_path)
            # Navigate up to find the skill directory
            # Expected path: .agent/skills/{skill-name}/SKILL.md
            # or .agent/skills/{skill-name}/references/*.md
            if ".agent/skills" in str(path):
                parts = path.parts
                try:
                    skills_idx = parts.index(".agent")
                    if len(parts) > skills_idx + 2:
                        skill_name = parts[skills_idx + 2]
                        skill_dir = skills_dir / skill_name
                        if skill_dir.is_dir() and (skill_dir / "SKILL.md").exists():
                            skill_dirs_to_validate.add(skill_dir)
                except (ValueError, IndexError):
                    pass
    else:
        # No files specified, validate all skills
        skill_dirs_to_validate = {
            skill_dir
            for skill_dir in skills_dir.iterdir()
            if skill_dir.is_dir() and (skill_dir / "SKILL.md").exists()
        }

    all_valid = True
    for skill_dir in sorted(skill_dirs_to_validate):
        print(f"\nValidating {skill_dir.name}...")
        if not validate_skill(skill_dir, schema):
            all_valid = False

    return 0 if all_valid else 1


if __name__ == "__main__":
    sys.exit(main())
