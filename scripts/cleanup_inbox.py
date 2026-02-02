#!/usr/bin/env python3
"""
Inbox Documentation Cleanup Script

Organizes and deduplicates files in docs/_inbox according to predefined rules.
Can be run manually or via task command: task docs:cleanup-inbox
"""

import argparse
import shutil
import sys
from pathlib import Path
from typing import Dict, List, Set


class InboxCleaner:
    """Handles cleanup and reorganization of docs/_inbox directory."""

    def __init__(self, inbox_path: Path, dry_run: bool = False):
        self.inbox_path = inbox_path
        self.dry_run = dry_run
        self.actions_taken: List[str] = []

        # Define folder structure
        self.subfolders = {
            "conversation-exports": "Raw conversation exports and dated files",
            "discussions": "Discussion threads and architectural conversations",
            "implementations": "Implementation-specific documentation",
            "refactoring": "Refactoring plans and architecture changes",
            "integrations": "Tool and system integration documentation",
            "planning": "Planning documents and research findings",
            "maintenance": "Maintenance tasks and bug fixes",
        }

        # Define cleanup rules
        self.files_to_delete: Set[str] = {
            "Implement Spherical Geometry.md",  # Superseded by "Implementing..."
            "Refactor Fantasim-World Architecture 001.md",  # Superseded by 002
            "Fantasim-world Refactoring.md",  # Superseded
            "discussion-001.md",  # Minimal content
            "discussion-003.md",  # Minimal content
        }

        self.files_to_rename: Dict[str, str] = {
            "discussion.md": "topology-first-architecture-discussion.md",
            "discussion-002.md": "spec-kit-workflow-discussion.md",
            "discussion-004.md": "geometry-libraries-comparison.md",
        }

        self.files_to_merge: Dict[str, List[str]] = {
            "RFC-Alignment-and-GPlates-Integration.md": [
                "Fantasim-World RFC Alignment.md",
                "Aligning FantaSim-World with GPlates.md",
            ]
        }

        # Define categorization rules (patterns and explicit mappings)
        self.categorization_rules = {
            "conversation-exports": lambda f: f.startswith("2026-") and f.endswith(".txt"),
            "discussions": lambda f: "discussion" in f.lower() and f.endswith(".md"),
            "implementations": lambda f: any(x in f for x in ["Implement", "Implementing"])
            and f.endswith(".md"),
            "refactoring": lambda f: "Refactor" in f and f.endswith(".md"),
            "integrations": lambda f: any(x in f for x in ["Integrate", "OpenCode", "UI_UX"])
            and f.endswith(".md"),
            "planning": lambda f: any(
                x in f.lower() for x in ["research", "plan", "rfc-alignment", "gplates-integration"]
            ),
            "maintenance": lambda f: any(x in f for x in ["Merge", "Fixing", "Clean"])
            and f.endswith(".md"),
        }

    def log(self, message: str, action: bool = True):
        """Log a message and optionally record it as an action."""
        prefix = "[DRY RUN] " if self.dry_run else ""
        print(f"{prefix}{message}")
        if action:
            self.actions_taken.append(message)

    def create_subfolders(self):
        """Create the subfolder structure."""
        self.log("\n=== Creating Subfolder Structure ===")
        for folder, description in self.subfolders.items():
            folder_path = self.inbox_path / folder
            if not folder_path.exists():
                if not self.dry_run:
                    folder_path.mkdir(parents=True, exist_ok=True)
                self.log(f"Created: {folder}/ - {description}")
            else:
                self.log(f"Exists: {folder}/", action=False)

    def delete_duplicates(self):
        """Delete duplicate and superseded files."""
        self.log("\n=== Deleting Duplicates/Superseded Files ===")
        for filename in self.files_to_delete:
            file_path = self.inbox_path / filename
            if file_path.exists():
                if not self.dry_run:
                    file_path.unlink()
                self.log(f"Deleted: {filename}")
            else:
                self.log(f"Not found (skipping): {filename}", action=False)

    def merge_files(self):
        """Merge related files into consolidated documents."""
        self.log("\n=== Merging Related Files ===")
        for target_name, source_files in self.files_to_merge.items():
            target_path = self.inbox_path / target_name

            if target_path.exists():
                self.log(f"Target already exists (skipping merge): {target_name}", action=False)
                continue

            # Collect content from source files
            merged_content = [f"# {target_name.replace('.md', '').replace('-', ' ')}\n\n"]
            merged_content.append("*This document consolidates multiple related discussions.*\n\n")
            merged_content.append("---\n\n")

            for source_name in source_files:
                source_path = self.inbox_path / source_name
                if source_path.exists():
                    merged_content.append(f"## Source: {source_name}\n\n")
                    with open(source_path, "r", encoding="utf-8") as f:
                        merged_content.append(f.read())
                    merged_content.append("\n\n---\n\n")

            # Write merged file
            if not self.dry_run:
                with open(target_path, "w", encoding="utf-8") as f:
                    f.writelines(merged_content)

            self.log(f"Merged {len(source_files)} files into: {target_name}")

            # Delete source files after merge
            for source_name in source_files:
                source_path = self.inbox_path / source_name
                if source_path.exists():
                    if not self.dry_run:
                        source_path.unlink()
                    self.log(f"  Deleted source: {source_name}")

    def rename_files(self):
        """Rename files for clarity."""
        self.log("\n=== Renaming Files for Clarity ===")
        for old_name, new_name in self.files_to_rename.items():
            old_path = self.inbox_path / old_name
            new_path = self.inbox_path / new_name

            if old_path.exists():
                if new_path.exists():
                    self.log(f"Target exists (skipping): {old_name} -> {new_name}", action=False)
                else:
                    if not self.dry_run:
                        old_path.rename(new_path)
                    self.log(f"Renamed: {old_name} -> {new_name}")
            else:
                self.log(f"Not found (skipping): {old_name}", action=False)

    def categorize_file(self, filename: str) -> str:
        """Determine which subfolder a file belongs to."""
        for category, rule in self.categorization_rules.items():
            if rule(filename):
                return category
        return None  # Stay in root _inbox

    def organize_files(self):
        """Move files into appropriate subfolders."""
        self.log("\n=== Organizing Files into Subfolders ===")

        # Get all files currently in inbox root
        files = [f for f in self.inbox_path.iterdir() if f.is_file()]

        moved_count = 0
        for file_path in files:
            filename = file_path.name
            category = self.categorize_file(filename)

            if category:
                target_dir = self.inbox_path / category
                target_path = target_dir / filename

                if not target_path.exists():
                    if not self.dry_run:
                        shutil.move(str(file_path), str(target_path))
                    self.log(f"Moved: {filename} -> {category}/")
                    moved_count += 1
                else:
                    self.log(f"Target exists (skipping): {filename} -> {category}/", action=False)

        self.log(f"\nMoved {moved_count} files into subfolders")

    def generate_readme(self):
        """Generate README.md for the _inbox directory."""
        readme_path = self.inbox_path / "README.md"

        content = [
            "# Documentation Inbox\n\n",
            "This directory contains work-in-progress documentation, conversation exports, ",
            "and planning materials that haven't been formalized into the main documentation.\n\n",
            "## Folder Structure\n\n",
        ]

        for folder, description in self.subfolders.items():
            folder_path = self.inbox_path / folder
            file_count = len(list(folder_path.glob("*"))) if folder_path.exists() else 0
            content.append(f"- **{folder}/** ({file_count} files): {description}\n")

        content.append("\n## Maintenance\n\n")
        content.append("To reorganize and cleanup this directory, run:\n\n")
        content.append("```bash\ntask docs:cleanup-inbox\n```\n\n")
        content.append("Or manually:\n\n")
        content.append("```bash\npython scripts/cleanup_inbox.py\n```\n")

        if not self.dry_run:
            with open(readme_path, "w", encoding="utf-8") as f:
                f.writelines(content)

        self.log("\nGenerated: README.md")

    def run(self):
        """Execute the full cleanup process."""
        if not self.inbox_path.exists():
            print(f"Error: Inbox path does not exist: {self.inbox_path}")
            sys.exit(1)

        print(f"{'=' * 60}")
        print("Inbox Cleanup Script")
        print(f"Path: {self.inbox_path}")
        print(f"Mode: {'DRY RUN' if self.dry_run else 'LIVE'}")
        print(f"{'=' * 60}")

        self.create_subfolders()
        self.delete_duplicates()
        self.merge_files()
        self.rename_files()
        self.organize_files()
        self.generate_readme()

        print(f"\n{'=' * 60}")
        print("Cleanup Complete!")
        print(f"Total actions: {len(self.actions_taken)}")
        if self.dry_run:
            print("\nThis was a DRY RUN. No changes were made.")
            print("Run without --dry-run to apply changes.")
        print(f"{'=' * 60}\n")


def main():
    parser = argparse.ArgumentParser(description="Cleanup and organize docs/_inbox directory")
    parser.add_argument(
        "--dry-run", action="store_true", help="Show what would be done without making changes"
    )
    parser.add_argument(
        "--inbox-path",
        type=Path,
        default=None,
        help="Path to _inbox directory (default: auto-detect from script location)",
    )

    args = parser.parse_args()

    # Auto-detect inbox path if not provided
    if args.inbox_path is None:
        script_dir = Path(__file__).parent
        project_root = script_dir.parent
        inbox_path = project_root / "docs" / "_inbox"
    else:
        inbox_path = args.inbox_path

    cleaner = InboxCleaner(inbox_path, dry_run=args.dry_run)
    cleaner.run()


if __name__ == "__main__":
    main()
