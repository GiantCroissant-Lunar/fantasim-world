"""Shared utilities for scripts.

Common functions used across multiple script modules for file operations,
text processing, and frontmatter parsing.
"""

import re
import shutil
from pathlib import Path
from typing import Any


def extract_frontmatter(content: str) -> dict[str, str]:
    """Extract YAML frontmatter from markdown content.

    Args:
        content: Markdown content potentially containing frontmatter.

    Returns:
        Dictionary of frontmatter key-value pairs.
    """
    match = re.match(r"^---\r?\n(.+?)\r?\n---", content, re.DOTALL)
    if not match:
        return {}

    frontmatter: dict[str, str] = {}
    for line in match.group(1).split("\n"):
        if ":" in line:
            key, value = line.split(":", 1)
            frontmatter[key.strip()] = value.strip()
    return frontmatter


def to_title_case(name: str) -> str:
    """Convert kebab-case to Title Case.

    Args:
        name: String in kebab-case (e.g., "my-script-name").

    Returns:
        Title case string (e.g., "My Script Name").
    """
    return " ".join(word.capitalize() for word in name.split("-"))


def ensure_dir(path: Path) -> None:
    """Ensure directory exists, creating it if necessary.

    Args:
        path: Path to directory.
    """
    path.mkdir(parents=True, exist_ok=True)


def clean_dir(path: Path) -> None:
    """Remove all contents from directory.

    Args:
        path: Path to directory to clean.
    """
    if not path.exists():
        return

    for item in path.iterdir():
        if item.is_dir():
            shutil.rmtree(item)
        else:
            item.unlink()


def clean_files_in_dir(path: Path, pattern: str) -> None:
    """Remove files matching pattern from directory.

    Args:
        path: Path to directory.
        pattern: Glob pattern to match files (e.g., "*.md").
    """
    if not path.exists():
        return
    for item in path.glob(pattern):
        if item.is_file():
            item.unlink()
