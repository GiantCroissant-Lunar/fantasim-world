# Skills

This directory contains atomic capability definitions that agents can invoke to perform specific tasks.

## Overview

Skills are discrete, reusable capabilities that follow a progressive disclosure pattern:

- **Entry file (SKILL.md)**: ~200 lines - acts as a router/map
- **Reference files**: 200-300 lines each - detailed procedures
- **Scripts**: Helper scripts for automation

This pattern keeps initial context small (~500 lines total for entry + 1 reference) while providing depth when needed.

## Skills Structure

Each skill is contained in its own subdirectory:

```
skills/
  dotnet-build/
    SKILL.md              # Entry point (~200 lines)
    references/           # Detailed procedures
      build-solution.md   # 200-300 lines
      restore-deps.md
      run-benchmarks.md
    scripts/              # Optional automation
      build-all.sh

  dotnet-test/
    SKILL.md
    references/
      run-unit-tests.md
      generate-coverage.md
      run-benchmarks.md
    scripts/
      test-with-coverage.sh

  code-format/
    SKILL.md
    references/
      dotnet-format.md
      prettier-format.md
      fix-all.md
    scripts/
      format-all.sh

  code-analyze/
    SKILL.md
    references/
      static-analysis.md
      security-scan.md
      dependency-check.md
    scripts/
      analyze.sh
```

## Skill Entry Format

Each `SKILL.md` must include YAML front-matter:

````markdown
---
name: dotnet-build
version: 0.2.0
kind: cli
description: Build .NET solution/projects using dotnet CLI. Use when task involves compiling, restoring dependencies, or building artifacts.
inputs:
  target: [solution, project, all]
  configuration: [Debug, Release]
  project_path: string
contracts:
  success: 'Build completes with zero errors; artifacts in bin/'
  failure: 'Non-zero exit code or compilation errors'
---

# .NET Build Skill (Entry Map)

> **Goal:** Guide agent to the exact build procedure needed.

## Quick Start (Pick One)

- **Build entire solution** → `references/build-solution.md`
- **Restore dependencies only** → `references/restore-deps.md`
- **Run benchmarks after build** → `references/run-benchmarks.md`

## When to Use

- Compiling .NET code (.csproj, .sln)
- Restoring NuGet packages
- Building specific configurations (Debug/Release)
- Preparing for testing or packaging

## Navigation

1. **Build entire solution** → `references/build-solution.md`
2. **Restore dependencies** → `references/restore-deps.md`
3. **Build + run benchmarks** → `references/run-benchmarks.md`

## Common Patterns

```bash

# Build solution (Debug)

dotnet build ./dotnet/PigeonPea.sln

# Build solution (Release)

dotnet build ./dotnet/PigeonPea.sln -c Release
```
````

## Progressive Disclosure Pattern

The Reddit refactor pattern emphasizes:

1. **Entry file (~200 lines)**: Quick orientation and routing
2. **Reference files (200-300 lines)**: Detailed step-by-step procedures
3. **Cold-start budget**: Entry + 1 reference ≤ ~500 lines total

This allows agents to:

- Start quickly with minimal context
- Dive deep only when needed
- Stay within reasonable token limits

## Skill Kinds

- `cli`: Command-line tool invocation
- `mcp`: Model Context Protocol server
- `http`: HTTP API calls
- `script`: Shell/PowerShell scripts
- `composite`: Combines multiple skills

## Validation

Skills are validated against `.agent/schemas/skill.schema.json` to ensure:

- YAML front-matter is valid
- Required fields present (name, version, kind, description, contracts)
- Version follows semantic versioning
- Description includes WHEN to use the skill

## Size Guidelines

| File Type         | Max Lines | Purpose                  |
| ----------------- | --------- | ------------------------ |
| SKILL.md entry    | ~200      | Router/map to references |
| Reference file    | 200-300   | Detailed procedure       |
| Cold-start budget | ~500      | Entry + 1 reference      |

## Adding a New Skill

1. Create subdirectory: `.agent/skills/your-skill-name/`
2. Create `SKILL.md` with YAML front-matter
3. Create `references/` subdirectory
4. Add detailed procedures in reference files
5. (Optional) Add `scripts/` for automation
6. Validate using: `scripts/validate_skills.py` (when available)

## Example Reference File

````markdown
# Build .NET Solution - Detailed Procedure

## Overview

Builds the entire PigeonPea.sln solution, including all projects and test projects.

## Prerequisites

- .NET SDK installed (check: `dotnet --version`)
- Solution file: `./dotnet/PigeonPea.sln`
- All project files (.csproj) present

## Standard Build Flow

### 1. Restore Dependencies

```bash
cd ./dotnet
dotnet restore PigeonPea.sln
```
````

### 2. Build Solution (Debug)

```bash
dotnet build PigeonPea.sln --configuration Debug
```

### 3. Build Solution (Release)

```bash
dotnet build PigeonPea.sln --configuration Release --no-restore
```

## Common Errors

### Error: NU1301 (Unable to load service index)

**Cause:** NuGet package source unreachable.

**Fix:**

```bash
dotnet nuget list source
dotnet restore --source https://api.nuget.org/v3/index.json
```

## Best Practices

1. **Keep entries focused**: Use entry file as a router, not a manual
2. **Progressive depth**: Start simple, provide detail in references
3. **Clear contracts**: Define success and failure criteria explicitly
4. **Actionable examples**: Include copy-paste-ready commands
5. **Error handling**: Document common failures and fixes
6. **Related skills**: Link to complementary skills

## Related

- **Agents**: See `.agent/agents/README.md` for agent definitions
- **Schemas**: See `.agent/schemas/README.md` for validation schemas
- **RFC-004**: Agent Infrastructure Enhancement design document
