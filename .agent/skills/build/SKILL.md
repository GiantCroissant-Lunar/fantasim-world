---
name: build
version: 0.1.0
kind: cli
description: "Build, test, or compile the solution using Nuke. Use when asked to build, compile, run tests, clean, restore, or perform any .NET build operation."
contracts:
  success: "Build completes successfully with zero errors"
  failure: "Build fails with compilation errors or missing dependencies"
---

# Build Skill

This project uses **Nuke Build** for all build operations. Never use raw `dotnet` commands.

## Command Reference

| Operation | Nuke Command | Do NOT Use |
|-----------|--------------|------------|
| Build | `nuke compile` | `dotnet build` |
| Test | `nuke test` | `dotnet test` |
| Clean | `nuke clean` | `dotnet clean` |
| Restore | `nuke restore` | `dotnet restore` |
| Pack | `nuke pack` | `dotnet pack` |

## Common Workflows

### Build the solution

```bash
nuke compile
```

### Run all tests

```bash
nuke test
```

### Clean and rebuild

```bash
nuke clean compile
```

### List available targets

```bash
nuke --help
```

## Why Nuke (Not dotnet)

- Ensures consistent build behavior across local and CI environments
- Handles project dependencies and build ordering correctly
- Runs code generators and pre/post-build tasks
- Provides reproducible builds with proper configuration

## Instructions

When the user asks to build, test, compile, or perform any .NET build operation:

1. Use the appropriate `nuke` command from the table above
2. Report build errors clearly from the output
3. **Never** suggest `dotnet build`, `dotnet test`, or other raw dotnet commands as alternatives
4. If a specific project needs building, check if there's a Nuke target for it first
