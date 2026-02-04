#!/usr/bin/env python3
import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional

import yaml

from .llm import call_llm


def run(cmd: List[str], cwd: Optional[Path] = None) -> None:
    print(f"$ {' '.join(cmd)}")
    subprocess.run(cmd, cwd=cwd, check=True)


def load_tasks(spec_dir: Path) -> List[Dict]:
    y = spec_dir / "tasks.yaml"
    if not y.exists():
        raise SystemExit(f"tasks.yaml not found at {y}")
    try:
        data = yaml.safe_load(y.read_text(encoding="utf-8")) or []
        if not isinstance(data, list):
            raise ValueError("tasks.yaml must be a list of task objects")
        return data
    except Exception as e:
        raise SystemExit(f"Failed to parse {y}: {e}")


def select_tasks(tasks: List[Dict], repo: str, task_id: Optional[str]) -> List[Dict]:
    filtered = [
        t
        for t in tasks
        if isinstance(t, dict) and (not t.get("repo") or t.get("repo") == repo)
    ]
    if task_id:
        filtered = [t for t in filtered if str(t.get("id")) == task_id]

    # Sort by numeric id tail if present (e.g., T-12)
    def sort_key(t: Dict):
        tid = str(t.get("id", "0"))
        m = re.search(r"(\d+)$", tid)
        return int(m.group(1)) if m else 0

    return sorted(filtered, key=sort_key)


def llm_propose_patch(repo: str, task: Dict, worktree: Path) -> str:
    title = task.get("title", "")
    detail = task.get("detail", "")
    tid = task.get("id", "T-?")
    instructions = (
        "You will propose a unified diff patch that applies to the repository root.\n"
        "Respond ONLY with patch content between markers.\n"
        "Use standard 'diff --git a/.. b/..' hunks and file headers.\n"
        "Avoid commentary.\n"
        "Markers:\n---PATCH START---\n<patch>\n---PATCH END---\n"
    )
    messages = [
        {
            "role": "system",
            "content": "You are a careful code assistant that outputs correct unified diffs.",
        },
        {
            "role": "user",
            "content": (
                f"Repository: {repo}\n"
                f"Worktree: {worktree}\n\n"
                f"Task {tid}: {title}\n\n{detail}\n\n"
                f"{instructions}"
            ),
        },
    ]
    resp = call_llm(messages)
    content = (resp or {}).get("content") or ""
    m = re.search(r"---PATCH START---\s*(.*?)\s*---PATCH END---", content, re.DOTALL)
    if not m:
        # fall back: try to use full content if it looks like a diff
        if content.strip().startswith("diff --git "):
            return content
        raise SystemExit("LLM response did not include a patch between markers")
    return m.group(1).strip()


def main() -> int:
    p = argparse.ArgumentParser(description="Apply a Spec task via LLM-generated patch")
    p.add_argument("--spec", required=True)
    p.add_argument("--slug", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument(
        "--branch",
        required=True,
        help="Existing branch to work on (e.g., spec/003-tiered-...)",
    )
    p.add_argument(
        "--task-id", default=None, help="Optional single task id (e.g., T-3)"
    )
    p.add_argument("--base", default="main")
    args = p.parse_args()

    root = Path(__file__).resolve().parents[2]
    spec_dir = root / "specs" / f"{args.spec}-{args.slug}"
    tasks = load_tasks(spec_dir)
    tasks_sel = select_tasks(tasks, args.repo, args.task_id)
    if not tasks_sel:
        print("No tasks selected for this repo.")
        return 0

    # Target repo should be checked out at ./target by the workflow
    target = root / "target"
    if not target.exists():
        raise SystemExit(f"Missing target checkout at {target}")

    # Ensure we are on the requested branch
    run(["git", "fetch", "origin", args.branch], cwd=target)
    run(["git", "checkout", args.branch], cwd=target)
    run(["git", "pull", "--ff-only", "origin", args.branch], cwd=target)

    for task in tasks_sel:
        tid = task.get("id", "T-?")
        print(f"\n=== Applying {tid}: {task.get('title', '')} ===")
        patch_text = llm_propose_patch(args.repo, task, target)
        (target / "_proposed.patch").write_text(patch_text, encoding="utf-8")

        try:
            run(
                ["git", "apply", "--whitespace=fix", "--index", "_proposed.patch"],
                cwd=target,
            )
        except subprocess.CalledProcessError:
            print("git apply failed; aborting this task.")
            return 1

        # Build and test for .NET repos (best-effort; skip if no solution found)
        try:
            run(["dotnet", "restore"], cwd=target)
            run(
                ["dotnet", "build", "--configuration", "Release", "--no-restore"],
                cwd=target,
            )
            run(
                [
                    "dotnet",
                    "test",
                    "--configuration",
                    "Release",
                    "--no-build",
                    "--verbosity",
                    "minimal",
                ],
                cwd=target,
            )
        except subprocess.CalledProcessError:
            print("Build/test failed; reverting staged changes for this task.")
            run(["git", "reset", "--hard"], cwd=target)
            return 1

        # Commit and push
        msg = f"spec({args.spec}): {tid} apply task via agent"
        run(["git", "add", "."], cwd=target)
        run(
            [
                "git",
                "-c",
                "user.name=automation-bot",
                "-c",
                "user.email=automation-bot@example.com",
                "commit",
                "-m",
                msg,
            ],
            cwd=target,
        )
        run(["git", "push", "origin", args.branch], cwd=target)

    print("\nAll selected tasks applied and pushed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
