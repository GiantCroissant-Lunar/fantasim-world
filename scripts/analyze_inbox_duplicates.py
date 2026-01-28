#!/usr/bin/env python3
"""
Inbox Duplicate Detection and Analysis

Analyzes docs/_inbox for content similarity and partial duplicates.
Provides interactive merging and cleanup recommendations.
"""

import os
import re
import hashlib
from pathlib import Path
from typing import Dict, List, Set, Tuple
from dataclasses import dataclass
from difflib import SequenceMatcher
import json
import argparse


@dataclass
class FileInfo:
    """Information about a file in the inbox."""
    path: Path
    size: int
    hash: str  # Hash of entire file content
    content: str
    lines: List[str]
    line_hashes: List[str]  # Hash of each individual line

    @property
    def name(self) -> str:
        return self.path.name


@dataclass
class SimilarityMatch:
    """Represents a similarity match between two files."""
    file1: str
    file2: str
    similarity: float  # Content similarity (0.0-1.0)
    line_overlap: float  # Line-level overlap (0.0-1.0)
    common_lines: int  # Number of matching line blocks
    unique_common_lines: int  # Number of unique shared lines
    total_lines: int
    duplicate_sections: List[Tuple[int, int, str]]  # (start, end, content)


class InboxAnalyzer:
    """Analyzes inbox for duplicates and similarity."""

    def __init__(self, inbox_path: Path, similarity_threshold: float = 0.3):
        self.inbox_path = inbox_path
        self.similarity_threshold = similarity_threshold
        self.files: Dict[str, FileInfo] = {}
        self.exact_duplicates: Dict[str, List[str]] = {}
        self.similar_pairs: List[SimilarityMatch] = []

    def load_files(self):
        """Load all markdown and text files from inbox."""
        print(f"Loading files from {self.inbox_path}...")

        for file_path in self.inbox_path.rglob('*'):
            if not file_path.is_file():
                continue

            # Only process text-based files
            if file_path.suffix.lower() not in ['.md', '.txt']:
                continue

            try:
                with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()

                # Hash entire file
                file_hash = hashlib.md5(content.encode()).hexdigest()
                lines = content.splitlines()

                # Hash each line individually (normalize whitespace)
                line_hashes = [
                    hashlib.md5(line.strip().encode()).hexdigest()
                    for line in lines
                    if line.strip()  # Skip empty lines
                ]

                self.files[str(file_path)] = FileInfo(
                    path=file_path,
                    size=file_path.stat().st_size,
                    hash=file_hash,
                    content=content,
                    lines=lines,
                    line_hashes=line_hashes
                )
            except Exception as e:
                print(f"Warning: Could not read {file_path}: {e}")

        print(f"Loaded {len(self.files)} files")

    def find_exact_duplicates(self):
        """Find files with identical content."""
        print("\nFinding exact duplicates...")

        hash_to_files: Dict[str, List[str]] = {}
        for path, info in self.files.items():
            if info.hash not in hash_to_files:
                hash_to_files[info.hash] = []
            hash_to_files[info.hash].append(path)

        self.exact_duplicates = {
            h: files for h, files in hash_to_files.items()
            if len(files) > 1
        }

        if self.exact_duplicates:
            print(f"Found {len(self.exact_duplicates)} groups of exact duplicates:")
            for hash_val, files in self.exact_duplicates.items():
                print(f"\n  Group (hash: {hash_val[:8]}...):")
                for f in files:
                    print(f"    - {Path(f).name}")
        else:
            print("No exact duplicates found")

    def calculate_similarity(self, content1: str, content2: str) -> float:
        """Calculate similarity ratio between two texts."""
        return SequenceMatcher(None, content1, content2).ratio()

    def calculate_line_overlap(self, line_hashes1: List[str], line_hashes2: List[str]) -> Tuple[float, int]:
        """
        Calculate what percentage of lines are shared between two files.
        Returns (overlap_ratio, common_line_count)
        """
        set1 = set(line_hashes1)
        set2 = set(line_hashes2)

        common = set1 & set2
        total = max(len(set1), len(set2))

        if total == 0:
            return 0.0, 0

        overlap_ratio = len(common) / total
        return overlap_ratio, len(common)

    def find_common_sections(self, lines1: List[str], lines2: List[str],
                            min_section_lines: int = 10) -> List[Tuple[int, int, str]]:
        """Find common sections between two files."""
        common_sections = []
        matcher = SequenceMatcher(None, lines1, lines2)

        for match in matcher.get_matching_blocks():
            if match.size >= min_section_lines:
                section_content = '\n'.join(lines1[match.a:match.a + match.size])
                common_sections.append((match.a, match.a + match.size, section_content))

        return common_sections

    def find_similar_files(self):
        """Find files with similar content (partial duplicates)."""
        print(f"\nFinding similar files (threshold: {self.similarity_threshold})...")

        file_list = list(self.files.items())
        total_comparisons = len(file_list) * (len(file_list) - 1) // 2
        comparison_count = 0

        for i, (path1, info1) in enumerate(file_list):
            for path2, info2 in file_list[i+1:]:
                comparison_count += 1
                if comparison_count % 100 == 0:
                    print(f"  Progress: {comparison_count}/{total_comparisons} comparisons...")

                # Skip if already exact duplicates
                if info1.hash == info2.hash:
                    continue

                # Calculate both content similarity and line overlap
                similarity = self.calculate_similarity(info1.content, info2.content)
                line_overlap, unique_common = self.calculate_line_overlap(
                    info1.line_hashes, info2.line_hashes
                )

                # Consider files similar if EITHER metric exceeds threshold
                if similarity >= self.similarity_threshold or line_overlap >= self.similarity_threshold:
                    # Find common sections
                    common_sections = self.find_common_sections(
                        info1.lines, info2.lines, min_section_lines=10
                    )

                    common_lines = sum(end - start for start, end, _ in common_sections)
                    total_lines = max(len(info1.lines), len(info2.lines))

                    match = SimilarityMatch(
                        file1=info1.name,
                        file2=info2.name,
                        similarity=similarity,
                        line_overlap=line_overlap,
                        common_lines=common_lines,
                        unique_common_lines=unique_common,
                        total_lines=total_lines,
                        duplicate_sections=common_sections
                    )
                    self.similar_pairs.append(match)

        # Sort by line overlap (more actionable than content similarity)
        self.similar_pairs.sort(key=lambda x: x.line_overlap, reverse=True)

        if self.similar_pairs:
            print(f"Found {len(self.similar_pairs)} similar file pairs:")
            for match in self.similar_pairs[:10]:  # Show top 10
                print(f"\n  {match.file1}")
                print(f"  ↔ {match.file2}")
                print(f"  Content similarity: {match.similarity:.1%}")
                print(f"  Line overlap: {match.line_overlap:.1%} ({match.unique_common_lines} unique shared lines)")
        else:
            print("No similar files found")

    def generate_report(self, output_path: Path):
        """Generate a detailed analysis report."""
        report = {
            'summary': {
                'total_files': len(self.files),
                'exact_duplicate_groups': len(self.exact_duplicates),
                'similar_pairs': len(self.similar_pairs),
                'similarity_threshold': self.similarity_threshold
            },
            'exact_duplicates': [
                {
                    'hash': hash_val[:16],
                    'files': [Path(f).name for f in files],
                    'size': self.files[files[0]].size
                }
                for hash_val, files in self.exact_duplicates.items()
            ],
            'similar_pairs': [
                {
                    'file1': match.file1,
                    'file2': match.file2,
                    'similarity': round(match.similarity, 3),
                    'line_overlap': round(match.line_overlap, 3),
                    'common_lines': match.common_lines,
                    'unique_common_lines': match.unique_common_lines,
                    'total_lines': match.total_lines,
                    'sections': len(match.duplicate_sections)
                }
                for match in self.similar_pairs
            ]
        }

        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2)

        print(f"\nReport saved to: {output_path}")

    def generate_markdown_report(self, output_path: Path):
        """Generate a human-readable markdown report."""
        lines = [
            "# Inbox Duplicate Analysis Report\n\n",
            f"**Analysis Date**: {Path.cwd()}\n",
            f"**Total Files**: {len(self.files)}\n",
            f"**Similarity Threshold**: {self.similarity_threshold:.0%}\n\n",
            "---\n\n"
        ]

        # Exact duplicates section
        lines.append("## Exact Duplicates\n\n")
        if self.exact_duplicates:
            for i, (hash_val, files) in enumerate(self.exact_duplicates.items(), 1):
                lines.append(f"### Group {i} (Hash: `{hash_val[:8]}...`)\n\n")
                lines.append(f"**Size**: {self.files[files[0]].size:,} bytes\n\n")
                lines.append("**Files**:\n")
                for f in files:
                    lines.append(f"- `{Path(f).name}`\n")
                lines.append("\n**Recommendation**: Keep one, delete others\n\n")
        else:
            lines.append("*No exact duplicates found*\n\n")

        lines.append("---\n\n")

        # Similar files section
        lines.append("## Similar Files (Partial Duplicates)\n\n")
        lines.append("*Sorted by line overlap (percentage of unique shared lines)*\n\n")
        if self.similar_pairs:
            for i, match in enumerate(self.similar_pairs, 1):
                lines.append(f"### Pair {i}\n\n")
                lines.append(f"**Files**:\n")
                lines.append(f"- `{match.file1}`\n")
                lines.append(f"- `{match.file2}`\n\n")
                lines.append(f"**Metrics**:\n")
                lines.append(f"- Content similarity: {match.similarity:.1%}\n")
                lines.append(f"- Line overlap: {match.line_overlap:.1%} ({match.unique_common_lines} unique shared lines)\n")
                lines.append(f"- Common sections: {len(match.duplicate_sections)} blocks\n")
                lines.append(f"- Total lines: {match.total_lines}\n\n")

                # Recommendation based on line overlap (more actionable)
                if match.line_overlap > 0.7:
                    lines.append("**Recommendation**: High line overlap - strong candidate for merging\n\n")
                elif match.line_overlap > 0.4:
                    lines.append("**Recommendation**: Moderate line overlap - review for consolidation\n\n")
                else:
                    lines.append("**Recommendation**: Low line overlap - may share common topics or boilerplate\n\n")
        else:
            lines.append("*No similar files found*\n\n")

        lines.append("---\n\n")
        lines.append("## Next Steps\n\n")
        lines.append("1. Review exact duplicates and delete redundant copies\n")
        lines.append("2. Examine similar pairs for merge opportunities\n")
        lines.append("3. Use `task docs:merge-duplicates` to interactively merge files\n")

        with open(output_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)

        print(f"Markdown report saved to: {output_path}")

    def run(self):
        """Run the full analysis."""
        self.load_files()
        self.find_exact_duplicates()
        self.find_similar_files()

        # Generate reports
        report_dir = self.inbox_path / '_analysis'
        report_dir.mkdir(exist_ok=True)

        self.generate_report(report_dir / 'duplicates.json')
        self.generate_markdown_report(report_dir / 'duplicates-report.md')

        print("\n" + "="*60)
        print("Analysis Complete!")
        print("="*60)
        print(f"\nSummary:")
        print(f"  Total files: {len(self.files)}")
        print(f"  Exact duplicate groups: {len(self.exact_duplicates)}")
        print(f"  Similar pairs: {len(self.similar_pairs)}")
        print(f"\nReports generated in: {report_dir}")


def main():
    parser = argparse.ArgumentParser(
        description="Analyze docs/_inbox for duplicates and similar content"
    )
    parser.add_argument(
        '--inbox-path',
        type=Path,
        default=None,
        help='Path to _inbox directory (default: auto-detect)'
    )
    parser.add_argument(
        '--threshold',
        type=float,
        default=0.3,
        help='Similarity threshold (0.0-1.0, default: 0.3)'
    )

    args = parser.parse_args()

    # Auto-detect inbox path
    if args.inbox_path is None:
        script_dir = Path(__file__).parent
        project_root = script_dir.parent
        inbox_path = project_root / 'docs' / '_inbox'
    else:
        inbox_path = args.inbox_path

    if not inbox_path.exists():
        print(f"Error: Inbox path does not exist: {inbox_path}")
        return 1

    analyzer = InboxAnalyzer(inbox_path, similarity_threshold=args.threshold)
    analyzer.run()

    return 0


if __name__ == '__main__':
    exit(main())
