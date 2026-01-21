import shutil
from pathlib import Path

SOURCE_DIR = Path(".agent/commands")
TARGET_DIR = Path(".opencode/commands")


def main() -> int:
    if not SOURCE_DIR.exists():
        print(f"Nothing to sync: {SOURCE_DIR} does not exist")
        return 0

    TARGET_DIR.mkdir(parents=True, exist_ok=True)

    for src in SOURCE_DIR.glob("*.md"):
        dst = TARGET_DIR / src.name
        shutil.copy2(src, dst)

    print(f"Synced commands: {SOURCE_DIR} -> {TARGET_DIR}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
