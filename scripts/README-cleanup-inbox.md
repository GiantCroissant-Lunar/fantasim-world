# Intelligent Inbox Duplicate Detection

## Overview

Smart content-aware duplicate detection system that analyzes actual file content to find exact and partial duplicates.

## Quick Start

### 1. Analyze for Duplicates

```bash
task docs:analyze-inbox
```

This will:
- Scan all `.md` and `.txt` files in `docs/_inbox`
- Find exact duplicates (identical content)
- Find similar files (partial duplicates with shared lines)
- Generate reports in `docs/_inbox/_analysis/`

### 2. Review the Report

```bash
# View the markdown report
cat docs/_inbox/_analysis/duplicates-report.md
```

### 3. Merge Files with Partial Duplication

```bash
# Preview first (recommended)
task docs:merge-duplicates:dry-run

# Apply merges
task docs:merge-duplicates
```

**What it does**:
- Combines files with ≥40% line overlap
- Creates merged documents with sections:
  - Common content (appears in both)
  - Unique content from each source
- Deletes redundant source files
- Names merged files: `original-name-merged.md`

## How It Works

### Dual-Metric Detection

The analyzer uses **two complementary metrics**:

1. **Content Similarity** (SequenceMatcher)
   - Compares entire file content as sequences
   - Good for finding files with similar structure
   - Sensitive to line order

2. **Line Overlap** (Set-based comparison)
   - Hashes each line individually (MD5)
   - Compares sets of line hashes
   - Finds files sharing many identical lines
   - **Insensitive to line order** - perfect for reorganized content!

**Files are flagged as similar if EITHER metric exceeds the threshold.**

### Line-Level Hashing

```python
# For each line in the file:
# 1. Strip whitespace
# 2. Skip empty lines
# 3. Calculate MD5 hash
line_hash = hashlib.md5(line.strip().encode()).hexdigest()

# Compare sets of hashes
common_lines = set(file1_hashes) & set(file2_hashes)
overlap_ratio = len(common_lines) / max(len(file1_hashes), len(file2_hashes))
```

**Why this works better**:
- Detects files with same content in different order
- Finds files where sections were copy-pasted
- Ignores whitespace differences
- More actionable for cleanup (you can see exactly which lines are duplicated)

### Similarity Threshold

Adjust sensitivity with `--threshold`:

```bash
# More sensitive (finds more matches)
task docs:analyze-inbox:threshold THRESHOLD=0.2

# Less sensitive (only high similarity)
task docs:analyze-inbox:threshold THRESHOLD=0.5
```

**Recommended thresholds**:
- `0.2-0.3`: Find loosely related documents
- `0.4-0.6`: Find moderately similar documents
- `0.7-0.9`: Find nearly identical documents
- `1.0`: Exact matches only (same as hash comparison)

## Reports Generated

### 1. `duplicates-report.md` (Human-Readable)

Markdown report with:
- Summary statistics
- Exact duplicate groups
- Similar file pairs with recommendations
- Next steps

### 2. `duplicates.json` (Machine-Readable)

JSON data for programmatic processing:
```json
{
  "summary": {
    "total_files": 42,
    "exact_duplicate_groups": 2,
    "similar_pairs": 15,
    "similarity_threshold": 0.3
  },
  "exact_duplicates": [...],
  "similar_pairs": [...]
}
```

## Interactive Merger

The `merge_duplicates.py` tool provides:

### Main Menu

1. **Handle exact duplicates**: Choose which copy to keep
2. **Handle similar files**: Merge or delete options
3. **Quit**: Exit

### Options for Similar Files

- **Keep file 1, delete file 2**
- **Keep file 2, delete file 1**
- **Create merged file**: Manual merge guidance
- **Skip**: Leave for later
- **Quit**: Exit

## Example Workflow

```bash
# Step 1: Analyze
task docs:analyze-inbox

# Output:
# Loading files from docs/_inbox...
# Loaded 42 files
# Found 2 groups of exact duplicates
# Found 15 similar file pairs
# Reports generated in: docs/_inbox/_analysis

# Step 2: Review report
cat docs/_inbox/_analysis/duplicates-report.md

# Step 3: Merge interactively
task docs:merge-duplicates

# Interactive prompts guide you through each duplicate
```

## Advantages Over Hardcoded Approach

| Hardcoded Script | Smart Detection |
|-----------------|-----------------|
| Manual rule definition | Automatic content analysis |
| Fixed file list | Adapts to any files |
| No similarity detection | **Dual-metric: content + line overlap** |
| Batch operations | Interactive decisions |
| One-size-fits-all | Configurable threshold |
| Order-sensitive | **Line overlap ignores order** |
| File-level only | **Line-level granularity** |

## Customization

### Modify Similarity Algorithm

Edit `analyze_inbox_duplicates.py`:

```python
def calculate_similarity(self, content1: str, content2: str) -> float:
    # Current: SequenceMatcher
    return SequenceMatcher(None, content1, content2).ratio()

    # Alternative: Could use other algorithms
    # - Levenshtein distance
    # - Jaccard similarity
    # - TF-IDF cosine similarity
```

### Change Minimum Section Size

```python
def find_common_sections(self, lines1, lines2, min_section_lines=10):
    # Change min_section_lines to adjust sensitivity
```

### Filter File Types

```python
if file_path.suffix.lower() not in ['.md', '.txt']:
    # Add more extensions: '.rst', '.adoc', etc.
```

## Troubleshooting

### Issue: No duplicates found

- Lower the threshold: `task docs:analyze-inbox:threshold THRESHOLD=0.2`
- Check file types are `.md` or `.txt`

### Issue: Too many false positives

- Raise the threshold: `task docs:analyze-inbox:threshold THRESHOLD=0.5`
- Review common sections in the report

### Issue: Analysis file not found

- Run `task docs:analyze-inbox` first before `task docs:merge-duplicates`

## Future Enhancements

Potential improvements:
- Semantic similarity using embeddings
- Automatic merge strategies
- Diff visualization
- Undo/rollback capability
- Batch operations mode
- Integration with git history
