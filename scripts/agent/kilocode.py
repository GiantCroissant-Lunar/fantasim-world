#!/usr/bin/env python3
"""
Generate .kilocode from .agent (skills + workflows + rules with adapter overrides).

Usage:
    python scripts/sync_kilocode.py
    task sync-kilocode
"""

import subprocess
import sys
from pathlib import Path

KILOCODE_DIR = Path(".kilocode")


def run_script(path: str) -> None:
    subprocess.run([sys.executable, path], check=True)


def main() -> int:
    KILOCODE_DIR.mkdir(parents=True, exist_ok=True)

    # Sync skills (handled by sync_skills.py which includes .kilocode)
    run_script("scripts/sync_skills.py")

    # Sync workflows (with adapter override support)
    run_script("scripts/sync_workflows.py")

    # Sync rules (with adapter override support)
    run_script("scripts/sync_rules.py")

    print("\nSynced .kilocode (skills + workflows + rules)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
