import shutil
import subprocess
import sys
from pathlib import Path

AGENT_OPENCODE_DIR = Path(".agent/adapters/opencode")
AGENT_OPENCODE_AGENTS_DIR = AGENT_OPENCODE_DIR / "agents"

OPENCODE_DIR = Path(".opencode")
OPENCODE_AGENTS_DIR = OPENCODE_DIR / "agents"


def run_script(path: str) -> None:
    subprocess.run([sys.executable, path], check=True)


def ensure_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        path.write_text(content, encoding="utf-8")


def copy_tree_overwrite(src: Path, dst: Path) -> None:
    if not src.exists():
        return

    if dst.exists():
        shutil.rmtree(dst)

    shutil.copytree(src, dst)


def main() -> int:
    OPENCODE_DIR.mkdir(parents=True, exist_ok=True)

    # Seed minimal OpenCode runtime files
    if AGENT_OPENCODE_DIR.exists():
        package_json = AGENT_OPENCODE_DIR / "package.json"
        if package_json.exists():
            shutil.copy2(package_json, OPENCODE_DIR / "package.json")

        gitignore = AGENT_OPENCODE_DIR / ".gitignore"
        if gitignore.exists():
            shutil.copy2(gitignore, OPENCODE_DIR / ".gitignore")

    # Always ensure node_modules is ignored at minimum
    ensure_file(OPENCODE_DIR / ".gitignore", "node_modules/\n")

    # Seed OpenCode agent markdowns (optional, but used by current audits)
    copy_tree_overwrite(AGENT_OPENCODE_AGENTS_DIR, OPENCODE_AGENTS_DIR)

    # Generate commands + skills into .opencode
    run_script("scripts/sync_commands.py")
    run_script("scripts/sync_skills.py")

    print("Synced .opencode (seed + commands + skills)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
