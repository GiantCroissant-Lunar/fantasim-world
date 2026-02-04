# Build Targets - Detailed Procedure

> **Status:** Deprecated – This target description is legacy. Build orchestration is now owned by the `unify-build` repository.

## Overview

Execute Nuke build targets in pigeon-pea: Clean, Restore, Compile. Located at `build/nuke/build/Build.cs`.

## Prerequisites

- **.NET SDK 8.0+** (`dotnet --version`)
- Nuke setup at `./build/nuke/`
- Command line (bash/PowerShell/cmd)

## Available Targets

### Clean

Removes build artifacts, prepares for fresh build.

```bash
cd ./build/nuke
./build.sh Clean
```

### Restore

Restores NuGet packages and dependencies. Runs after Clean (if specified).

```bash
cd ./build/nuke
./build.sh Restore
```

### Compile (Default)

Compiles solution, requires Restore first.

```bash
cd ./build/nuke
./build.sh           # Default
./build.sh Compile   # Explicit
```

## Execution Patterns

### Single Target

```bash
cd ./build/nuke
./build.sh Clean
```

### Multiple Targets

```bash
cd ./build/nuke
./build.sh Clean Restore Compile
```

**Dependency resolution:** `Compile` runs `Restore` automatically. `Clean Compile` runs: Clean → Restore → Compile.

### Default Target

```bash
./build.sh    # Runs Compile (+ Restore dependency)
```

## Platform-Specific Execution

**Linux/macOS:** `./build.sh Compile --configuration Release`
**PowerShell:** `./build.ps1 Compile --configuration Release`
**Command Prompt:** `build.cmd Compile --configuration Release`

## Build Parameters

### Configuration

```bash
./build.sh Compile                        # Debug (local) or Release (CI/CD)
./build.sh Compile --configuration Release
./build.sh Compile --configuration Debug
```

### Custom Parameters

Syntax: `--parameter-name value`

Boolean: `--skip-tests` (true) or `--skip-tests false`

## Build Output

**Success:** Targets show ✓, "Build succeeded" message, exit code 0
**Failure:** Targets show ✗, error details, non-zero exit code

## Common Scenarios

**Fresh Build:** `./build.sh Clean Compile`
**Quick Rebuild:** `./build.sh Compile` (uses cached packages)
**Release Build:** `./build.sh Clean Compile --configuration Release`
**CI/CD:** `./build.sh Clean Restore Compile --configuration Release`

## Target Dependencies

**Order:** Clean → Restore → Compile

**Examples:**

- `Compile` → Restore → Compile
- `Clean Compile` → Clean → Restore → Compile
- `Restore` → Restore only

## Verbosity Control

Levels: `quiet`, `minimal` (default), `normal`, `detailed`, `diagnostic`

```bash
./build.sh Compile --verbosity detailed
```

## Getting Help

**List targets:** `./build.sh --help`
**Target help:** `./build.sh Compile --help`

## Performance Optimization

**Parallel builds:** Default (all cores) or `--max-cpu-count 4`
**Skip restore:** `--no-restore` (only if deps unchanged)
**Incremental:** Automatic (recompiles only changed files)

## Troubleshooting

### Target not found

**Fix:** `./build.sh --help` to list targets. Names are case-sensitive.

### Build failed (exit code 1)

**Fix:** Check error output, run `--verbosity detailed`

### Cannot execute binary file

**Fix:** `chmod +x build.sh` or `dos2unix build.sh` (line endings)

### Wrong script on Windows

**Fix:** Use `./build.ps1` (PowerShell) or `build.cmd` (cmd), not `.sh`

### .NET SDK not found

**Fix:** Install .NET SDK 8.0+ or let script auto-download

### NuGet restore failed (NU1301)

**Fix:** Check network, verify sources: `dotnet nuget list source`

### Compilation failed (CS####)

**Fix:** Review error messages, fix syntax errors, ensure deps restored

## Integration with Other Skills

**After build:** `dotnet test` (testing)
**Before build:** `dotnet format` (code formatting)
**With analysis:** `dotnet build /p:RunAnalyzers=true`

## Advanced Usage

**Custom targets:** `./build.sh CustomTarget`
**Environment vars:** `export Configuration=Release`
**Dry run:** `./build.sh Compile --plan` (shows execution order)

## Best Practices

1. Start from `build/nuke` directory
2. Use `Clean` for fresh builds
3. Specify configuration explicitly in CI/CD
4. Check exit codes: `echo $?` or `$LASTEXITCODE`
5. Use verbosity for debugging
6. Don't commit build artifacts

## Related

- [`setup-nuke.md`](./setup-nuke.md) - Nuke setup and configuration
- [`../SKILL.md`](../SKILL.md) - Skill entry point
- [`../../../build/nuke/build/Build.cs`](../../../build/nuke/build/Build.cs) - Target definitions
- [Nuke Documentation](https://nuke.build/) - Official docs
