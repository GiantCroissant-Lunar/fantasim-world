---
name: file-organizer
version: 0.1.0
kind: composite
description: "Intelligently organizes your files and folders by understanding context, finding duplicates, suggesting better structures, and automating cleanup tasks."
contracts:
  success: "Files organized according to rules with no data loss"
  failure: "Organization failed or files were incorrectly moved"
references:
  - "examples.md"
---

# File Organizer

This skill acts as your personal organization assistant, helping you maintain a clean, logical file structure without the mental overhead of constant manual organization.

## When to Use This Skill

- Your Downloads folder is a chaotic mess
- You can't find files because they're scattered everywhere
- You have duplicate files taking up space
- Your folder structure doesn't make sense anymore
- You want to establish better organization habits

## What This Skill Does

1. **Analyzes Current Structure**: Reviews your folders and files
2. **Finds Duplicates**: Identifies duplicate files across your system
3. **Suggests Organization**: Proposes logical folder structures
4. **Automates Cleanup**: Moves, renames, and organizes files with your approval
5. **Maintains Context**: Makes smart decisions based on file types, dates, and content

## Instructions

When a user requests file organization help:

### 1. Understand the Scope

Ask clarifying questions:
- Which directory needs organization?
- What's the main problem? (Can't find things, duplicates, too messy?)
- Any files or folders to avoid?
- How aggressively to organize? (Conservative vs. comprehensive)

### 2. Analyze Current State

Review the target directory — summarize:
- Total files and folders
- File type breakdown
- Size distribution
- Date ranges
- Obvious organization issues

### 3. Identify Organization Patterns

Group files by **type** (Documents, Images, Videos, Archives, Code), **purpose** (Work vs. Personal, Active vs. Archive), or **date** (Current year, Previous years, Archive candidates).

### 4. Find Duplicates (if requested)

For each set of duplicates:
- Show all file paths, sizes, modification dates
- Recommend which to keep (usually newest or best-named)
- **Always ask for confirmation before deleting**

### 5. Propose Organization Plan

Present a clear plan before making changes:
- Current state summary
- Proposed folder structure
- List of changes (creates, moves, renames, deletes)
- Files needing user decision
- Ask for approval before proceeding

### 6. Execute Organization

After approval, organize systematically:
- Create folder structure first
- Move files with clear logging
- Handle filename conflicts gracefully
- **Always confirm before deleting anything**
- Stop and ask if you encounter unexpected situations

### 7. Provide Summary

After organizing, report what changed and provide maintenance tips.

## Best Practices

### Folder Naming
- Use clear, descriptive names
- Avoid spaces (use hyphens or underscores)
- Use prefixes for ordering: "01-current", "02-archive"

### File Naming
- Include dates: "2024-10-17-meeting-notes.md"
- Be descriptive: "q3-financial-report.xlsx"
- Remove download artifacts: "document-final-v2 (1).pdf" → "document.pdf"

### When to Archive
- Projects not touched in 6+ months
- Completed work that might be referenced later
- Files you're hesitant to delete (archive first)

## Pro Tips

1. **Start Small**: Begin with one messy folder to build trust
2. **Regular Maintenance**: Run weekly cleanup on Downloads
3. **Consistent Naming**: Use "YYYY-MM-DD - Description" format
4. **Archive Aggressively**: Move old projects to Archive instead of deleting

See `references/examples.md` for detailed examples and common prompts.
