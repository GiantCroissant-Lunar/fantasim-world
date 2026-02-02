import argparse
import datetime
import os
import re
import sys

# Configuration
DOCS_DIR = "docs"
TODAY = datetime.date.today().isoformat()


def get_file_stats(filepath):
    """Returns basic file stats for logging."""
    return f"{filepath}"


def to_kebab_case(string):
    """Converts a string to kebab-case."""
    string = string.lower()
    string = re.sub(r"[^a-z0-9\s-]", "", string)
    string = re.sub(r"\s+", "-", string)
    return string


def extract_title_from_content(content):
    """Extracts title from the first H1 header in markdown content."""
    match = re.search(r"^#\s+(.+)$", content, re.MULTILINE)
    if match:
        return match.group(1).strip()
    return None


def parse_pseudo_frontmatter(content):
    """
    Looks for lines like '**Status**: Draft' or 'Date: 2026-...' at the start of the file.
    Returns a dictionary of found metadata and the remaining content.
    """
    metadata = {}
    lines = content.splitlines()
    new_lines = []

    # We'll scan the first 20 lines for metadata.
    # If we hit a header or empty lines for too long, we stop.
    scanning = True
    idx = 0
    while idx < len(lines) and idx < 20 and scanning:
        line = lines[idx]

        # Check for "**Key**: Value" or "Key: Value"
        # Regex to match: optional bold chars, Key, optional bold chars, colon, whitespace, Value
        match = re.match(r"^(\*\*)?([a-zA-Z0-9\s]+)(\*\*)?:\s*(.+)$", line)
        if match:
            key = match.group(2).strip().lower()
            value = match.group(4).strip()

            # Map common keys
            if key in ["status", "state"]:
                metadata["status"] = value
            elif key in ["date", "created"]:
                metadata["date"] = value
            elif key in ["tags", "keywords"]:
                metadata["tags"] = [t.strip() for t in value.split(",")]
            elif key in ["reference", "refs"]:
                metadata["reference"] = value
            # We don't preserve everything, essentially we consume lines that look like metadata
        elif line.strip() == "":
            pass  # Skip empty lines while scanning
        elif line.startswith("#"):
            scanning = False  # Header stops scanning
            new_lines.append(line)
        else:
            scanning = False  # Regular text stops scanning
            new_lines.append(line)
        idx += 1

    # Append the rest of the file
    new_lines.extend(lines[idx:])
    return metadata, "\n".join(new_lines)


def generate_frontmatter_yaml(meta):
    """Generates the YAML frontmatter string."""
    yaml = "---\n"
    yaml += f'title: "{meta.get("title", "Untitled")}"\n'
    yaml += f'id: "{meta.get("id", "unknown")}"\n'
    if meta.get("description"):
        yaml += f'description: "{meta.get("description")}"\n'
    else:
        yaml += 'description: ""\n'

    date_val = meta.get("date", TODAY)
    # Ensure date is a string
    yaml += f'date: "{date_val}"\n'

    tags = meta.get("tags", [])
    if tags:
        yaml += "tags:\n"
        for tag in tags:
            yaml += f"  - {tag}\n"
    else:
        yaml += "tags: []\n"

    # Add any extra fields we decided to keep
    for k, v in meta.items():
        if k not in ["title", "id", "description", "date", "tags"]:
            yaml += f'{k}: "{v}"\n'

    yaml += "---\n\n"
    return yaml


def process_markdown_file(filepath, dry_run=False):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    # Check if already has frontmatter
    has_frontmatter = content.startswith("---\n")

    if has_frontmatter:
        # If it has frontmatter, check for duplicate H1
        fm_end = content.find("\n---\n", 4)
        if fm_end != -1:
            fm_end_idx = fm_end + 5  # include \n---\n
            frontmatter = content[:fm_end_idx]
            body = content[fm_end_idx:]

            # Look for H1 at start of body
            match = re.search(r"^\s*#\s+(.+)$", body, re.MULTILINE)
            if match and match.start() < 50:  # Should be near top
                if dry_run:
                    print(f"[DRY RUN] Would remove duplicate H1 in {filepath}: '{match.group(1)}'")
                else:
                    # Remove H1
                    new_body = body[: match.start()] + body[match.end() :].lstrip()
                    with open(filepath, "w", encoding="utf-8") as f:
                        f.write(frontmatter + new_body)
                    print(f"[FIX] Removed duplicate H1 from {filepath}")
        return

    filename = os.path.basename(filepath)
    name_no_ext = os.path.splitext(filename)[0]

    # Defaults
    meta = {
        "id": to_kebab_case(name_no_ext),
        "title": name_no_ext.replace("-", " ").replace("_", " ").title(),
        "date": TODAY,
        "tags": [],
    }

    # Extract pseudo-frontmatter
    pseudo_meta, remaining_content = parse_pseudo_frontmatter(content)
    meta.update(pseudo_meta)

    # Try to find a better title from the content if not explicitly set in pseudo
    content_title = extract_title_from_content(remaining_content)
    if content_title:
        if "title" not in pseudo_meta:
            meta["title"] = content_title

        # Remove the H1 from remaining_content since we moved it to frontmatter
        remaining_content = re.sub(
            r"^\s*#\s+.+\n?", "", remaining_content, count=1, flags=re.MULTILINE
        ).lstrip()

    # Construct new content
    frontmatter = generate_frontmatter_yaml(meta)
    new_content = frontmatter + remaining_content

    if dry_run:
        print(f"[DRY RUN] Would update {filepath}")
        print(f"  > Generated Title: {meta['title']}")
        print(f"  > Generated ID:    {meta['id']}")
    else:
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(new_content)
        print(f"[UPDATE] Updated {filepath}")


def process_text_file(filepath, dry_run=False):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    filename = os.path.basename(filepath)
    name_no_ext = os.path.splitext(filename)[0]

    meta = {
        "id": to_kebab_case(name_no_ext),
        "title": name_no_ext.replace("-", " ").replace("_", " ").title(),
        "date": TODAY,
        "tags": ["conversation-export"],
    }

    # Try to extract date from filename (e.g. 2026-01-26-...)
    date_match = re.match(r"(\d{4}-\d{2}-\d{2})", filename)
    if date_match:
        meta["date"] = date_match.group(1)

    frontmatter = generate_frontmatter_yaml(meta)

    # Wrap content in text block
    new_content = frontmatter + "```text\n" + content + "\n```"

    new_filepath = os.path.splitext(filepath)[0] + ".md"

    if dry_run:
        print(f"[DRY RUN] Would convert {filepath} -> {new_filepath}")
    else:
        with open(new_filepath, "w", encoding="utf-8") as f:
            f.write(new_content)
        os.remove(filepath)
        print(f"[CONVERT] Converted {filepath} -> {new_filepath}")


def main():
    parser = argparse.ArgumentParser(description="Organize docs and add frontmatter.")
    parser.add_argument(
        "--dry-run", action="store_true", help="Print actions without modifying files."
    )
    args = parser.parse_args()

    root_dir = os.path.abspath(DOCS_DIR)

    if not os.path.exists(root_dir):
        print(f"Error: {root_dir} does not exist.")
        sys.exit(1)

    print(f"Scanning {root_dir}...")

    for root, dirs, files in os.walk(root_dir):
        for file in files:
            filepath = os.path.join(root, file)

            if file.lower().endswith(".md"):
                process_markdown_file(filepath, dry_run=args.dry_run)
            elif file.lower().endswith(".txt"):
                process_text_file(filepath, dry_run=args.dry_run)
            else:
                # print(f"[IGNORE] Skipping {file}")
                pass


if __name__ == "__main__":
    main()
