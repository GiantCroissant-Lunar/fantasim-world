# Agent Rules

This directory contains rule definitions for agent behavior and constraints.

## Purpose

Rules define how agents should behave, what constraints they must follow, and what policies govern their actions.

## Available Rules

### Core Rules

- **`autonomous-development.md`** - Autonomous build, test, and fix workflow
- **`build-artifacts-logs.md`** - Build artifacts and log locations
- **`code-quality.md`** - Code quality standards
- **`git-commit-rules.md`** - Git and commit requirements (CRITICAL)
- **`documentation-management.md`** - Documentation workflow (RFC-012)
- **`dotnet-architecture.md`** - .NET tiered architecture and layer rules (CRITICAL)

## File Format

Rules can be defined in various formats:

- `.md` - Markdown documentation with embedded rules
- `.yaml` - Structured rule definitions
- `.json` - JSON-formatted rule configurations

## Key Rules Summary

### Autonomous Development

**ALWAYS** build, run, test, and fix code yourself. Never ask user to do these tasks.

### Build and Logs

**ALWAYS** check logs after build/run. Logs are in `build/_artifacts/latest/`.

### Git Commits

**ALL COMMITS MUST PASS PRE-COMMIT HOOKS.** Run `pre-commit run --all-files` before committing.

### Code Quality

- .NET: Use `dotnet format`
- Python: Use `black` and `isort`
- JavaScript/TypeScript: Use `prettier`

### Documentation

Follow RFC-012 documentation management system. Check `docs/index/registry.json` before creating new docs.

### .NET Architecture (CRITICAL)

**ALL .NET code MUST follow four-tier architecture.** See `dotnet-architecture.md` for complete rules.

Key requirements:

- **NO wrapper projects** - Use external libraries directly in plugins
- **Four tiers**: Contracts (1) → Proxies (2) → Implementations (3) → Providers (4)
- **Double-plugin for content domains**: Domain plugins (WHAT) + Platform plugins (HOW)
- **Plugins are isolated** - Never depend on other plugins
- **Contracts are stable** - Minimal dependencies, framework types only

**Before writing .NET code**: Read [.NET Architecture Rules](./dotnet-architecture.md)
