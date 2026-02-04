# Agent Documentation Management Rules

**Location**: This file should be referenced by agents working with documentation.

See the full documentation management rules in:

- `docs/AGENT-DOCUMENTATION-RULES.md` - Complete agent guidelines
- `docs/DOCUMENTATION-SCHEMA.md` - Front-matter schema
- `docs/RFC-NUMBERING-QUICK-REF.md` - RFC numbering quick reference
- `scripts/README.md` - Available automation tools

## Quick Checklist for Agents

### Before Creating Documentation

- [ ] Check `docs/index/registry.json` for existing docs
- [ ] Run `python scripts/validate-docs.py` to check for duplicates
- [ ] Find next available doc_id number
- [ ] Create draft in `docs/_inbox/`

### Required Front-Matter

```yaml
---
doc_id: 'PREFIX-NNNNN' # e.g., RFC-00042
title: 'Your Title'
doc_type: 'rfc' # rfc|adr|prd|guide|plan|spec|reference|finding|glossary
status: 'draft' # draft|active|superseded|archived
canonical: true
created: '2025-11-20'
tags: ['tag1', 'tag2']
summary: 'One sentence description'
---
```

### Before Finalizing

- [ ] Run `python scripts/validate-docs.py`
- [ ] Check quality: `python scripts/generate-quality-report.py`
- [ ] Move from `_inbox/` to final location
- [ ] Regenerate indexes if needed

### Tools Available

- `generate-frontmatter.py` - Auto-generate front-matter
- `validate-docs.py` - Validate documentation
- `generate-quality-report.py` - Quality scoring
- `generate-index.py` - Regenerate INDEX.md
- `generate-dashboard.py` - Regenerate DASHBOARD.md
- `cleanup-inbox.py` - Clean up old drafts

See `docs/AGENT-DOCUMENTATION-RULES.md` for complete guidelines.
