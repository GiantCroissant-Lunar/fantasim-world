# Documentation Rules for AI Agents

**Rule ID Prefix:** `R-DOC`

---

## Core Principles

**R-DOC-001: Inbox-First Writing**
All AI agents MUST write new documentation ONLY to `docs/_inbox/`. Never write directly to canonical locations (`docs/specs/`, `docs/rfcs/`, etc.).

**R-DOC-002: Mandatory Front-Matter**
Every markdown file in `docs/` MUST include YAML front-matter with ALL required fields. See `docs/DOCUMENTATION-SCHEMA.md`.

**R-DOC-003: Registry-First Search**
Before creating new documentation, ALWAYS check `docs/index/registry.json` to find existing canonical documents on the same topic.

**R-DOC-004: Update Over Create**
ALWAYS prefer updating an existing canonical document over creating a new one. Only create new docs when the topic is genuinely new.

---

## Required Workflow

### 1. Before Creating Documentation

Check if topic already exists:

1. Review `docs/index/registry.json`
2. Search for related docs in `docs/`

If a canonical document exists → Update it instead of creating new.

### 2. Creating New Documentation

If you must create a new document:

**Step 2.1: Write to inbox**

```yaml
# Save to: docs/_inbox/YYYY-MM-DD-your-title--DOC-YYYY-NNNNN.md
---
doc_id: DOC-YYYY-NNNNN           # Replace with next ID from registry
title: Your Descriptive Title
doc_type: spec|rfc|adr|plan|finding|guide|glossary|reference
status: draft
canonical: false                 # Always false in inbox
created: 2025-10-21
tags: [relevant, tags, here]
summary: >
  One-line description of what this document covers.
source:
  author: agent
  agent: claude|copilot|windsurf|gemini
  model: sonnet-4.5|gpt-4|etc
  session: session-id-if-available
---

# Your Title

Content here...
```

**Step 2.2: Include metadata**

- Always set `source.author: agent`
- Always set `source.agent` to your agent name
- Include `source.model` if known
- Include `source.session` if available

**Step 2.3: Get next doc_id**

Find the highest existing doc_id in registry and increment by 1.

### 3. Updating Existing Documentation

When updating a canonical document:

**Step 3.1: Verify it's canonical**

Check the document's front-matter to ensure `canonical: true`.

**Step 3.2: Preserve front-matter**

- Do NOT change `doc_id`, `created`, `canonical`
- Update `updated` field to current date
- Add your session to `source` if updating significantly

**Step 3.3: Add update note**

```markdown
## Changelog

### 2025-10-21 (Claude Sonnet 4.5)
- Updated section X with new approach Y
- Added examples for Z
```

---

## Document Types and Locations

| Type       | When to Use                                   | Save To         |
|------------|-----------------------------------------------|-----------------|
| `spec`     | Product/feature specifications                | `_inbox/` first |
| `rfc`      | Proposals needing discussion/decision         | `_inbox/` first |
| `adr`      | Architectural decisions (accepted/rejected)   | `_inbox/` first |
| `plan`     | Implementation plans, milestones, phases      | `_inbox/` first |
| `finding`  | Research results, benchmarks, comparisons     | `_inbox/` first |
| `guide`    | How-tos, tutorials, playbooks, runbooks       | `_inbox/` first |
| `glossary` | Term definitions                              | Update existing |
| `reference`| API docs, technical reference                 | `_inbox/` first |

---

## Anti-Patterns (DO NOT DO THIS)

❌ **Creating versioned duplicates**

```
DESIGN.md
DESIGN-V2.md
DESIGN-V3.md
```

✅ **Instead**: Update `DESIGN.md` and use `supersedes` field.

❌ **Creating phase/iteration docs**

```
PHASE1-COMPLETE.md
PHASE1-REVISED-COMPLETE.md
PHASE1-FINAL.md
```

✅ **Instead**: Single canonical `docs/plans/project-phases.md` with status updates.

❌ **Scattered findings**

```
/root/ANALYSIS.md
/packages/foo/RESEARCH.md
```

✅ **Instead**: All findings in `docs/_inbox/` → promoted to `docs/findings/`.

❌ **Creating docs without checking registry**

✅ **Instead**: Check registry first, update existing doc.

---

## Validation and CI

All documentation is validated:

- ✅ Required front-matter present
- ✅ Valid `doc_type` and `status` values
- ✅ Only one `canonical: true` per concept
- ✅ No near-duplicates in `_inbox/` vs corpus

If validation fails:

1. Read the error message carefully
2. Fix front-matter in affected files
3. See front-matter requirements above for schema
4. For duplicates, merge content into canonical doc

---

## Summary Checklist

Before saving documentation, verify:

- [ ] Checked `docs/index/registry.json` for existing docs
- [ ] Saving to `docs/_inbox/` (not canonical location)
- [ ] All required front-matter fields present
- [ ] `doc_id` follows `PREFIX-YYYY-NNNNN` format
- [ ] `doc_type` is valid value
- [ ] `status` is valid value
- [ ] `canonical: false` (always in inbox)
- [ ] `source` metadata includes agent info
- [ ] `tags` are lowercase and relevant
- [ ] `summary` is clear and concise
- [ ] Content is well-structured with headers
- [ ] Links use relative paths

---

## References

- **Schema Definition**: See front-matter requirements in this document
- **Registry**: `docs/index/registry.json` (auto-generated, consumer-side)

---

**Last Updated**: 2025-10-21
**Rule Version**: 1.0
**Applies To**: All AI agents (Claude, Copilot, Windsurf, Gemini, etc.)
