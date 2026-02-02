#!/usr/bin/env python3
"""
Inbox Documentation Organizer

Organizes files in docs/_inbox into categorized subfolders.
Should be run AFTER merge_duplicates.py
"""

import argparse
import shutil
import sys
from pathlib import Path
from typing import Callable, Dict


class InboxOrganizer:
    """Handles reorganization of docs/_inbox directory into categorized subfolders."""

    def __init__(self, inbox_path: Path, dry_run: bool = False):
        self.inbox_path = inbox_path
        self.dry_run = dry_run

        # Define folder structure
        self.subfolders = {
            "conversation-exports": "Raw conversation exports and dated files",
            "discussions": "Discussion threads and architectural conversations",
            "implementations": "Implementation-specific documentation and merged specs",
            "refactoring": "Refactoring plans and architecture changes",
            "integrations": "Tool and system integration documentation",
            "planning": "Planning documents and research findings",
            "maintenance": "Maintenance tasks and bug fixes",
        }

        # Define categorization rules (patterns and explicit mappings)
        self.categorization_rules: Dict[str, Callable[[str], bool]] = {
            "conversation-exports": lambda f: (f.startswith("2026-") and f.endswith(".txt"))
            or ("caveat" in f.lower()),
            "discussions": lambda f: "discussion" in f.lower(),
            "implementations": lambda f: any(
                x in f.lower() for x in ["implement", "rfc-v2", "contracts", "dataset"]
            )
            or ("merged" in f.lower() and ("implement" in f.lower() or "rfc" in f.lower())),
            "refactoring": lambda f: "refactor" in f.lower(),
            "integrations": lambda f: any(
                x in f.lower() for x in ["integrate", "opencode", "ui_ux"]
            ),
            "planning": lambda f: any(
                x in f.lower() for x in ["research", "plan", "alignment", "gplates"]
            ),
            "maintenance": lambda f: any(
                x in f.lower() for x in ["merge", "fixing", "clean", "unload", "sync", "sync-"]
            ),
        }

    def log(self, message: str):
        """Log a message."""
        prefix = "[DRY RUN] " if self.dry_run else ""
        print(f"{prefix}{message}")

    def create_subfolders(self):
        """Create the subfolder structure."""
        self.log("\n=== Creating Subfolder Structure ===")
        for folder, description in self.subfolders.items():
            folder_path = self.inbox_path / folder
            if not folder_path.exists():
                if not self.dry_run:
                    folder_path.mkdir(parents=True, exist_ok=True)
                self.log(f"Created folder: {folder}/")

    def organize_files(self):
        """Move files into appropriate subfolders based on rules."""
        self.log("\n=== Organizing Files ===")

        # Only process files in the root of the inbox
        files = [
            f
            for f in self.inbox_path.iterdir()
            if f.is_file() and f.name != "README.md" and f.name != ".gitignore"
        ]

        moved_count = 0
        for file_path in files:
            filename = file_path.name
            target_folder = None

            # Match against rules
            for folder, rule in self.categorization_rules.items():
                if rule(filename):
                    target_folder = folder
                    break

            if target_folder:
                dest_dir = self.inbox_path / target_folder
                dest_path = dest_dir / filename

                self.log(f"Moving: {filename} -> {target_folder}/")
                if not self.dry_run:
                    # Handle name collisions if any
                    if dest_path.exists():
                        base = file_path.stem
                        ext = file_path.suffix
                        counter = 1
                        while (dest_dir / f"{base}_{counter}{ext}").exists():
                            counter += 1
                        dest_path = dest_dir / f"{base}_{counter}{ext}"

                    shutil.move(str(file_path), str(dest_path))
                moved_count += 1
            else:
                self.log(f"No category found for: {filename}")

        self.log(f"\nTotal files organized: {moved_count}")

    def generate_readme(self):
        """Generate/Update README.md in the inbox directory."""
        readme_path = self.inbox_path / "README.md"

        lines = [
            "# Documentation Inbox\n\n",
            "This folder acts as a staging area for new documentation, chat exports, and technical discussions.\n",
            "Files are organized into categories periodically using automated scripts.\n\n",
            "## Folder Structure\n\n",
        ]

        for folder, desc in self.subfolders.items():
            lines.append(f"- **{folder}/**: {desc}\n")

        lines.append("\n## Maintenance Tools\n\n")
        lines.append("- `task docs:analyze-inbox`: Analyzes files for duplicates and similarity.\n")
        lines.append(
            "- `task docs:merge-duplicates`: Intelligently merges overlapping content and removes redundancy.\n"
        )
        lines.append(
            "- `task docs:organize-inbox`: Organizes files into the folders defined above.\n"
        )

        if not self.dry_run:
            with open(readme_path, "w", encoding="utf-8") as f:
                f.writelines(lines)
            self.log(f"Generated: {readme_path.name}")

    def run(self):
        """Run the organizer."""
        if not self.inbox_path.exists():
            print(f"Error: Path does not exist: {self.inbox_path}")
            sys.exit(1)

        self.create_subfolders()
        self.organize_files()
        self.generate_readme()
        self.log("\nOrganization Complete.")


def main():
    parser = argparse.ArgumentParser(description="Organize docs/_inbox files into subfolders")
    parser.add_argument("--inbox-path", type=Path, help="Path to inbox")
    parser.add_argument("--dry-run", action="store_true", help="Preview moves")

    args = parser.parse_args()

    # Auto-detect
    inbox_path = args.inbox_path
    if not inbox_path:
        # Check current project structure
        project_root = Path(__file__).parent.parent
        inbox_path = project_root / "docs" / "_inbox"

    organizer = InboxOrganizer(inbox_path, dry_run=args.dry_run)
    organizer.run()


if __name__ == "__main__":
    main()
