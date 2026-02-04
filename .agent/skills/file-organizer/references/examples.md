# File Organizer Examples

## Example 1: Organizing Downloads

**User**: "My Downloads folder is a mess with 500+ files. Help me organize it."

**Process**:
1. Analyzes Downloads folder
2. Finds patterns: work docs, personal photos, installers, random PDFs
3. Proposes structure:
   - Downloads/
     - Work/
     - Personal/
     - Installers/ (DMG, PKG files)
     - Archive/
     - ToSort/ (things needing decisions)
4. Asks for confirmation
5. Moves files intelligently based on content and names
6. Results: 500 files → 5 organized folders

## Example 2: Finding and Removing Duplicates

**User**: "Find duplicate files in my Documents and help me decide which to keep."

**Output**:
```markdown
# Found 23 Sets of Duplicates (156 MB total)

## Duplicate Set 1: "proposal.pdf"
- `/Documents/proposal.pdf` (2.3 MB, modified: 2024-03-15)
- `/Documents/old/proposal.pdf` (2.3 MB, modified: 2024-03-15)
- `/Desktop/proposal.pdf` (2.3 MB, modified: 2024-03-10)

**Recommendation**: Keep `/Documents/proposal.pdf` (most recent in correct location)
Delete the other 2 copies?

[Continue for all duplicates...]
```

## Example 3: Restructuring Projects Folder

**User**: "Review my ~/Projects directory and suggest improvements."

**Output**:
```markdown
# Analysis of ~/Projects

## Current Structure Issues
- Mix of active and archived projects (3+ years old)
- No consistent naming convention
- Some projects at root, others in random subfolders
- Duplicate folders (project-name, project-name-old, project-name-v2)

## Proposed Structure
Projects/
├── Active/
│   ├── client-work/
│   ├── side-projects/
│   └── learning/
├── Archive/
│   ├── 2022/
│   ├── 2023/
│   └── 2024/
└── Templates/

## Specific Changes
1. Move 12 projects not touched since 2022 → Archive/
2. Consolidate 4 duplicate project folders
3. Rename projects to consistent format: "client-name-project-name"
4. Create Archive for old work
```

## Example 4: Organizing Photos by Date

**User**: "Organize my photo folders by year and month."

Creates structure like:
```
Photos/
├── 2023/
│   ├── 01-January/
│   ├── 02-February/
│   └── ...
├── 2024/
│   ├── 01-January/
│   └── ...
└── Unsorted/
```

Then moves photos based on EXIF data or file modification dates.

## Common Prompts

- "Organize my Downloads folder"
- "Find duplicate files in my Documents folder"
- "Review my Projects folder structure"
- "My Desktop is covered in files. Help me organize."
- "Organize all photos by date (year/month)"
- "Help me separate work files from personal files"
