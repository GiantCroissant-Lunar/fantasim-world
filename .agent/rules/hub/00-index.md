# Agent Instruction Base

Version: 1.0.0
Source of Truth for all automated assistant behavior.

## Project Context

- **Type**: Configurable per consumer project
- **Phase**: Active development
- **Tech Stack**: .NET, TypeScript (configurable)

## Composition

- 10-principles.md: Core philosophy for development
- 20-rules.md: Normative, enforceable rules (ID-based)
- 30-glossary.md: Domain terms
- 40-documentation.md: Documentation standards and schema

Adapters (in ../adapters) must reference **rule IDs** instead of copying rule text.

## Adapter Sync & Versioning

- Adapters MUST declare `Base-Version-Expected:`. If it doesn't match this file's `Version`, adapters should **fail closed** (ask for upgrade).
- Pointer files (e.g., CLAUDE.md) should redirect agents to this canon and the agent-specific adapter.

All adapters must enforce documentation conventions.

## Naming Conventions

### Documentation

- **Markdown Files**: Use YAML front-matter with required fields (see DOCUMENTATION-SCHEMA.md)
- **File Naming**: `YYYY-MM-DD-kebab-case-title--DOC-YYYY-NNNNN.md`
- **Categories**: specs/, rfcs/, adrs/, plans/, findings/, guides/, glossary/
- **Inbox First**: All new docs go to `docs/_inbox/` initially

### Code

- **C# Classes**: PascalCase (e.g., `GameStateManager`)
- **C# Methods**: PascalCase (e.g., `ProcessCommand`)
- **C# Variables**: camelCase (e.g., `currentState`)
- **TypeScript**: Follow standard conventions (camelCase for variables/functions, PascalCase for classes)

## Change Policy

- **Add rule**: append with a new unique ID; never repurpose IDs.
- **Deprecate rule**: mark "DEPRECATED" but keep the ID (do not delete).
- **Major version bump** if any backward-incompatible change (removal or semantics shift). Minor bump for additive rules or clarifications.

## Documentation Integration

This rule system complements the documentation schema in `docs/DOCUMENTATION-SCHEMA.md`. The schema defines the structure, while agent rules define how agents interact with and maintain documentation.

When conflicts arise:

1. Documentation schema takes precedence for structure
2. Agent rules take precedence for automation behavior
3. Escalate to human if genuine conflict exists
