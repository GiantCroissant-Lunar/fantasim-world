#!/usr/bin/env python3
"""
Smart Document Merger

Intelligently merges files with partial duplication by:
1. Detecting duplicate sections
2. Preserving unique content from each file
3. Creating consolidated documents
4. Removing redundant source files
"""

import argparse
import hashlib
import json
import sys
from difflib import SequenceMatcher
from pathlib import Path
from typing import List, Tuple


class DocumentMerger:
    """Smart merger for documents with partial duplication."""

    def __init__(self, inbox_path: Path, analysis_path: Path, dry_run: bool = False):
        self.inbox_path = inbox_path
        self.analysis_path = analysis_path
        self.dry_run = dry_run
        self.analysis_data = None

    def load_analysis(self):
        """Load the duplicate analysis report."""
        if not self.analysis_path.exists():
            print(f"Error: Analysis file not found: {self.analysis_path}")
            print("Run 'task docs:analyze-inbox' first to generate analysis")
            sys.exit(1)

        with open(self.analysis_path, "r", encoding="utf-8") as f:
            self.analysis_data = json.load(f)

        print(f"Loaded analysis: {self.analysis_data['summary']['total_files']} files")

    def read_file(self, filename: str) -> Tuple[str, List[str]]:
        """Read file content and return (content, lines)."""
        file_path = self.inbox_path / filename
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        return content, content.splitlines()

    def get_line_hashes(self, lines: List[str]) -> List[str]:
        """Get hash for each line."""
        return [hashlib.md5(line.strip().encode()).hexdigest() for line in lines if line.strip()]

    def find_unique_sections(
        self, lines1: List[str], lines2: List[str]
    ) -> Tuple[List[str], List[str], List[str]]:
        """
        Find unique and common sections between two files.
        Returns (unique_to_file1, common, unique_to_file2)
        """
        matcher = SequenceMatcher(None, lines1, lines2)

        unique1 = []
        common = []
        unique2 = []

        # Track which lines we've processed
        processed1 = set()
        processed2 = set()

        # Get matching blocks
        for match in matcher.get_matching_blocks():
            if match.size > 0:
                # Add common section
                common.extend(lines1[match.a : match.a + match.size])

                # Mark as processed
                for i in range(match.a, match.a + match.size):
                    processed1.add(i)
                for i in range(match.b, match.b + match.size):
                    processed2.add(i)

        # Collect unique lines from file1
        for i, line in enumerate(lines1):
            if i not in processed1:
                unique1.append(line)

        # Collect unique lines from file2
        for i, line in enumerate(lines2):
            if i not in processed2:
                unique2.append(line)

        return unique1, common, unique2

    def merge_two_files(self, file1: str, file2: str) -> str:
        """
        Intelligently merge two files with partial duplication.
        Returns merged content.
        """
        content1, lines1 = self.read_file(file1)
        content2, lines2 = self.read_file(file2)

        unique1, common, unique2 = self.find_unique_sections(lines1, lines2)

        # Build merged document
        merged = []

        # Header
        merged.append(f"# Merged Document: {file1} + {file2}\n")
        merged.append("*This document combines content from multiple sources*\n\n")
        merged.append("---\n\n")

        # Common content (appears in both)
        if common:
            merged.append("## Common Content\n\n")
            merged.extend(line + "\n" for line in common)
            merged.append("\n---\n\n")

        # Unique content from file1
        if unique1:
            merged.append(f"## Additional Content from {file1}\n\n")
            merged.extend(line + "\n" for line in unique1)
            merged.append("\n---\n\n")

        # Unique content from file2
        if unique2:
            merged.append(f"## Additional Content from {file2}\n\n")
            merged.extend(line + "\n" for line in unique2)
            merged.append("\n")

        return "".join(merged)

    def merge_multiple_files(self, files: List[str]) -> str:
        """
        Merge multiple files by iteratively combining them.
        """
        if len(files) == 0:
            return ""
        if len(files) == 1:
            content, _ = self.read_file(files[0])
            return content

        # Start with first two files
        result = self.merge_two_files(files[0], files[1])

        # Iteratively merge remaining files
        for i in range(2, len(files)):
            # Write temp result
            temp_lines = result.splitlines()

            # Read next file
            content_next, lines_next = self.read_file(files[i])

            # Merge
            unique_result, common, unique_next = self.find_unique_sections(temp_lines, lines_next)

            # Rebuild
            new_result = []
            new_result.append(f"# Merged Document: {', '.join(files[: i + 1])}\n")
            new_result.append(f"*Combined from {i + 1} sources*\n\n")
            new_result.append("---\n\n")

            if common:
                new_result.append("## Common Content\n\n")
                new_result.extend(line + "\n" for line in common)
                new_result.append("\n---\n\n")

            if unique_result:
                new_result.append("## Additional Content from Previous Merge\n\n")
                new_result.extend(line + "\n" for line in unique_result)
                new_result.append("\n---\n\n")

            if unique_next:
                new_result.append(f"## Additional Content from {files[i]}\n\n")
                new_result.extend(line + "\n" for line in unique_next)
                new_result.append("\n")

            result = "".join(new_result)

        return result

    def suggest_merge_name(self, files: List[str]) -> str:
        """Suggest a name for the merged file."""
        # Extract common words from filenames
        basenames = [Path(f).stem for f in files]

        # Simple heuristic: use first file's name with "-merged" suffix
        base = basenames[0]
        return f"{base}-merged.md"

    def process_exact_duplicates(self):
        """Handle exact duplicates - just delete extras."""
        exact_dupes = self.analysis_data.get("exact_duplicates", [])

        if not exact_dupes:
            print("\n✓ No exact duplicates to process")
            return

        print(f"\n{'=' * 60}")
        print(f"Processing {len(exact_dupes)} groups of exact duplicates")
        print(f"{'=' * 60}\n")

        for i, group in enumerate(exact_dupes, 1):
            files = group["files"]
            print(f"\nGroup {i}: {len(files)} identical files")

            # Keep first, delete rest
            keep = files[0]
            delete = files[1:]

            print(f"  Keeping: {keep}")
            print(f"  Deleting: {', '.join(delete)}")

            if not self.dry_run:
                for filename in delete:
                    file_path = self.inbox_path / filename
                    if file_path.exists():
                        file_path.unlink()
                        print(f"    ✓ Deleted: {filename}")

    def process_similar_files(self, min_overlap: float = 0.4):
        """
        Process similar files by merging those with high overlap.
        """
        similar = self.analysis_data.get("similar_pairs", [])

        if not similar:
            print("\n✓ No similar files to process")
            return

        # Filter by minimum overlap
        candidates = [pair for pair in similar if pair.get("line_overlap", 0) >= min_overlap]

        if not candidates:
            print(f"\n✓ No files with ≥{min_overlap:.0%} line overlap")
            return

        print(f"\n{'=' * 60}")
        print(f"Processing {len(candidates)} similar file pairs (≥{min_overlap:.0%} overlap)")
        print(f"{'=' * 60}\n")

        # Track which files have been merged
        merged_files = set()

        for i, pair in enumerate(candidates, 1):
            file1 = pair["file1"]
            file2 = pair["file2"]

            # Skip if already merged
            if file1 in merged_files or file2 in merged_files:
                print(f"\nPair {i}: Skipped (already merged)")
                print(f"  {file1} ↔ {file2}")
                continue

            print(f"\nPair {i}:")
            print(f"  {file1}")
            print(f"  ↔ {file2}")
            print(f"  Line overlap: {pair.get('line_overlap', 0):.1%}")
            print(f"  Content similarity: {pair.get('similarity', 0):.1%}")

            # Generate merged content
            merged_content = self.merge_two_files(file1, file2)
            merged_name = self.suggest_merge_name([file1, file2])
            merged_path = self.inbox_path / merged_name

            print(f"  → Merged file: {merged_name}")

            if not self.dry_run:
                # Write merged file
                with open(merged_path, "w", encoding="utf-8") as f:
                    f.write(merged_content)
                print(f"    ✓ Created: {merged_name}")

                # Delete source files
                for filename in [file1, file2]:
                    file_path = self.inbox_path / filename
                    if file_path.exists():
                        file_path.unlink()
                        print(f"    ✓ Deleted: {filename}")

                # Mark as merged
                merged_files.add(file1)
                merged_files.add(file2)

    def run(self, min_overlap: float = 0.4):
        """Run the merge process."""
        self.load_analysis()

        mode = "DRY RUN" if self.dry_run else "LIVE"
        print(f"\n{'=' * 60}")
        print(f"Smart Document Merger - {mode}")
        print(f"Minimum overlap threshold: {min_overlap:.0%}")
        print(f"{'=' * 60}")

        self.process_exact_duplicates()
        self.process_similar_files(min_overlap)

        print(f"\n{'=' * 60}")
        print("Merge Complete!")
        if self.dry_run:
            print("\nThis was a DRY RUN. No changes were made.")
            print("Run without --dry-run to apply changes.")
        print(f"{'=' * 60}\n")


def main():
    parser = argparse.ArgumentParser(
        description="Intelligently merge files with partial duplication"
    )
    parser.add_argument("--inbox-path", type=Path, default=None, help="Path to _inbox directory")
    parser.add_argument(
        "--dry-run", action="store_true", help="Preview merges without making changes"
    )
    parser.add_argument(
        "--min-overlap",
        type=float,
        default=0.4,
        help="Minimum line overlap to trigger merge (0.0-1.0, default: 0.4)",
    )

    args = parser.parse_args()

    # Auto-detect paths
    if args.inbox_path is None:
        script_dir = Path(__file__).parent
        project_root = script_dir.parent
        inbox_path = project_root / "docs" / "_inbox"
    else:
        inbox_path = args.inbox_path

    analysis_path = inbox_path / "_analysis" / "duplicates.json"

    merger = DocumentMerger(inbox_path, analysis_path, dry_run=args.dry_run)
    merger.run(min_overlap=args.min_overlap)


if __name__ == "__main__":
    main()
