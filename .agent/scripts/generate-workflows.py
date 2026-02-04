#!/usr/bin/env python3
"""
Generate platform-specific workflows from canonical YAML definitions.

Usage:
    python scripts/generate-workflows.py                     # Generate all
    python scripts/generate-workflows.py --platform windsurf # Specific platform
    python scripts/generate-workflows.py --workflow build    # Specific workflow
"""

import argparse
import sys
from pathlib import Path
from typing import Dict, Optional

try:
    import yaml
except ImportError:
    print("Error: PyYAML is required. Install with: pip install pyyaml")
    sys.exit(1)


class WorkflowGenerator:
    """Convert canonical YAML workflows to platform-specific formats."""

    def __init__(self, project_root: Path):
        self.project_root = project_root
        self.workflows_dir = project_root / ".agent" / "workflows"
        self.platforms = {
            "windsurf": self._generate_windsurf,
            "claude": self._generate_claude,
            "copilot": self._generate_copilot,
            "cursor": self._generate_cursor,
        }

    def generate_all(
        self, platform: Optional[str] = None, workflow: Optional[str] = None
    ):
        """Generate workflows for all or specific platforms."""
        if not self.workflows_dir.exists():
            print(f"Error: Workflows directory not found: {self.workflows_dir}")
            return

        # Find all YAML workflow files
        yaml_files = list(self.workflows_dir.glob("*.yaml")) + list(
            self.workflows_dir.glob("*.yml")
        )
        yaml_files = [f for f in yaml_files if f.stem != "SCHEMA"]

        if not yaml_files:
            print("No workflow YAML files found")
            return

        # Filter by specific workflow if requested
        if workflow:
            yaml_files = [f for f in yaml_files if f.stem == workflow]
            if not yaml_files:
                print(f"Error: Workflow '{workflow}' not found")
                return

        # Generate for each workflow
        for yaml_file in yaml_files:
            try:
                with open(yaml_file, encoding="utf-8") as f:
                    data = yaml.safe_load(f)

                workflow_name = data.get("name", yaml_file.stem)
                supported_platforms = data.get("platforms", list(self.platforms.keys()))

                print(f"\nGenerating workflow: {workflow_name}")

                # Filter platforms
                target_platforms = [platform] if platform else supported_platforms

                for plat in target_platforms:
                    if plat not in self.platforms:
                        print(f"  [WARN] Unknown platform: {plat}")
                        continue

                    if plat not in supported_platforms:
                        print(f"  [SKIP] Skipping {plat} (not supported by workflow)")
                        continue

                    generator = self.platforms[plat]
                    output = generator(data)

                    # Write output file
                    output_dir = self._get_output_dir(plat)
                    output_dir.mkdir(parents=True, exist_ok=True)

                    output_file = (
                        output_dir / f"{workflow_name}.{self._get_extension(plat)}"
                    )
                    with open(output_file, "w", encoding="utf-8") as f:
                        f.write(output)

                    print(
                        f"  [OK] {plat}: {output_file.relative_to(self.project_root)}"
                    )

            except Exception as e:
                print(f"  [ERROR] Error processing {yaml_file.name}: {e}")

    def _get_output_dir(self, platform: str) -> Path:
        """Get output directory for platform."""
        dirs = {
            "windsurf": self.project_root / ".windsurf" / "workflows",
            "claude": self.project_root / ".claude" / "workflows",
            "copilot": self.project_root / ".github" / "copilot-workflows",
            "cursor": self.project_root / ".cursor" / "workflows",
        }
        return dirs.get(platform, self.project_root / f".{platform}" / "workflows")

    def _get_extension(self, platform: str) -> str:
        """Get file extension for platform."""
        extensions = {
            "windsurf": "md",
            "claude": "md",
            "copilot": "md",
            "cursor": "cursorrules",
        }
        return extensions.get(platform, "md")

    def _generate_windsurf(self, data: Dict) -> str:
        """Generate Windsurf markdown workflow."""
        lines = [
            f"# {data['name'].replace('-', ' ').title()}",
            "",
            data["description"],
            "",
            "## Steps",
            "",
        ]

        step_num = 1
        for stage in data.get("stages", []):
            lines.append(f"### {step_num}. {stage['name']}")
            lines.append("")

            for step in stage.get("steps", []):
                step_type = step.get("type", "manual")
                step_name = step["name"]

                if step_type == "command":
                    lines.append(f"**{step_name}**")
                    lines.append("")
                    if step.get("description"):
                        lines.append(step["description"])
                        lines.append("")
                    lines.append("```bash")
                    lines.append(step["command"])
                    lines.append("```")
                    lines.append("")
                    if step.get("auto_fix"):
                        lines.append(
                            "If errors occur, fix them autonomously and retry."
                        )
                        lines.append("")

                elif step_type == "manual":
                    lines.append(f"**{step_name}**")
                    lines.append("")
                    if step.get("description"):
                        desc = step["description"].strip()
                        lines.append(desc)
                        lines.append("")

                elif step_type == "conditional":
                    lines.append(f"**{step_name} (if {step.get('condition')})**")
                    lines.append("")
                    if step.get("action"):
                        action = step["action"].strip()
                        lines.append(action)
                        lines.append("")

                elif step_type == "output":
                    lines.append(f"**{step_name}**")
                    lines.append("")
                    lines.append("Provide a summary:")
                    lines.append("")
                    lines.append("```")
                    lines.append(step.get("template", "").strip())
                    lines.append("```")
                    lines.append("")

            step_num += 1

        # Add metadata footer
        lines.extend(
            [
                "---",
                "",
                f"*Generated from `.agent/workflows/{data['name']}.yaml`*",
                "",
                "To modify this workflow, edit the canonical YAML file and regenerate.",
            ]
        )

        return "\n".join(lines)

    def _generate_claude(self, data: Dict) -> str:
        """Generate Claude markdown workflow."""
        # Similar to Windsurf but with Claude-specific formatting
        return self._generate_windsurf(data)

    def _generate_copilot(self, data: Dict) -> str:
        """Generate GitHub Copilot workflow."""
        lines = [
            f"# Workflow: {data['name']}",
            "",
            data["description"],
            "",
            "When I say `/{}`".format(data["name"]),
            "",
        ]

        for stage in data.get("stages", []):
            lines.append(f"## {stage['name']}")
            lines.append("")

            for step in stage.get("steps", []):
                step_type = step.get("type", "manual")

                if step_type == "command":
                    lines.append(f"- Run: `{step['command']}`")
                    if step.get("auto_fix"):
                        lines.append("  - If errors, fix autonomously and retry")
                elif step_type == "manual":
                    lines.append(
                        f"- {step['name']}: {step.get('description', '').strip()}"
                    )
                elif step_type == "conditional":
                    condition = step.get("condition")
                    action = step.get("action", "").strip()
                    lines.append(f"- If {condition}: {action}")

            lines.append("")

        return "\n".join(lines)

    def _generate_cursor(self, data: Dict) -> str:
        """Generate Cursor rules format."""
        lines = [
            f"@workflow {data['name']}",
            f"@description {data['description']}",
            "",
        ]

        for stage in data.get("stages", []):
            lines.append(f"@stage {stage['name']}")

            for step in stage.get("steps", []):
                step_type = step.get("type", "manual")

                if step_type == "command":
                    lines.append(f"  @run {step['command']}")
                    if step.get("auto_fix"):
                        lines.append("  @auto-fix true")
                elif step_type == "manual":
                    lines.append(f"  @step {step['name']}")
                    if step.get("description"):
                        lines.append(f"    {step['description'].strip()}")

            lines.append("")

        return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(
        description="Generate platform-specific workflows from canonical YAML"
    )
    parser.add_argument(
        "--platform",
        choices=["windsurf", "claude", "copilot", "cursor"],
        help="Generate for specific platform only",
    )
    parser.add_argument(
        "--workflow",
        help="Generate specific workflow only (e.g., 'build-and-test')",
    )

    args = parser.parse_args()

    # Find project root (directory containing .agent folder)
    current = Path.cwd()
    project_root = None

    for parent in [current] + list(current.parents):
        if (parent / ".agent").is_dir():
            project_root = parent
            break

    if not project_root:
        print("Error: Could not find project root (directory with .agent folder)")
        sys.exit(1)

    print(f"Project root: {project_root}")

    generator = WorkflowGenerator(project_root)
    generator.generate_all(platform=args.platform, workflow=args.workflow)

    print("\n[SUCCESS] Workflow generation complete!")


if __name__ == "__main__":
    main()
