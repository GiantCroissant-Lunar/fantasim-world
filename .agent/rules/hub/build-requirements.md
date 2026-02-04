# Build Requirements

**Status:** Deprecated – Build orchestration is now owned by the `unify-build` repository.
**Applies to:** All .NET development
**Last updated:** 2025-11-25

## Critical Rule: Always Use NUKE Build System

**DO NOT use `dotnet build` or `dotnet publish` directly.** Always use the NUKE build system via the `build` command or build scripts.

## Why This Matters

The project uses a plugin architecture where incorrect builds can cause:

1. **Namespace conflicts** - Old plugin DLLs with outdated namespaces remain in bin directories
2. **Type identity issues** - Plugin loading fails with "Could not load type" errors
3. **Non-reproducible builds** - Without GitVersion, artifacts lack proper versioning
4. **Plugin discovery failures** - Plugins must be copied to the correct location

## Correct Build Process

### Using the Build Command

The preferred method is using the `build` command defined in `.agent/commands/build.yaml`:

```bash
# This is handled automatically when you use the 'build' command
# The command will:
# 1. Clean old plugin binaries
# 2. Run NUKE build system with PublishPlayers target
# 3. Output versioned artifacts to build/_artifacts/{version}
# 4. Copy plugins to build/_artifacts/{version}/plugins
# 5. Update build/_artifacts/latest symlink
```

### Using NUKE Directly

If you need to use NUKE directly (not through the command):

```powershell
# Publish all configured players
./build/nuke/build.ps1 PublishPlayers

# Build and run console app in one step
./build/nuke/build.ps1 Task

# Build and run Windows app
./build/nuke/build.ps1 Task --player windows

# Build and run with arguments
./build/nuke/build.ps1 Task --player-args "--debug --level 5"
```

## Artifact Structure

After a successful build, artifacts are organized as:

```
build/
└── _artifacts/
    ├── {version}/                          # Versioned artifacts (e.g., 0.1.0-alpha.42)
    │   ├── PigeonPea.Console/             # Console app
    │   │   ├── PigeonPea.Console.exe
    │   │   └── ...dependencies...
    │   ├── PigeonPea.Windows/             # Windows app
    │   │   ├── PigeonPea.Windows.exe
    │   │   └── ...dependencies...
    │   ├── plugins/                        # Shared plugins directory
    │   │   ├── config-service/
    │   │   ├── input-basic/
    │   │   └── ...other plugins...
    │   └── build-logs/                     # Build metadata and logs
    │       └── publish-players-*.log
    └── latest/                             # Symlink/copy to current version
        ├── PigeonPea.Console/
        ├── PigeonPea.Windows/
        ├── plugins/
        └── build-logs/
```

## Running Built Executables

### From Latest Build

```powershell
# Console app
./build/_artifacts/latest/PigeonPea.Console/PigeonPea.Console.exe

# Windows app
./build/_artifacts/latest/PigeonPea.Windows/PigeonPea.Windows.exe
```

### From Specific Version

```powershell
# Console app
./build/_artifacts/0.1.0-alpha.42/PigeonPea.Console/PigeonPea.Console.exe

# Windows app
./build/_artifacts/0.1.0-alpha.42/PigeonPea.Windows/PigeonPea.Windows.exe
```

## Versioning

The build system uses **GitVersion** for automatic semantic versioning:

- Reads from git history and branch names
- Generates versions like `0.1.0-alpha.42`, `1.0.0`, etc.
- Ensures reproducible builds with version tracking
- Updates `latest` symlink to point to current build

## Plugin Discovery

The published executables discover plugins relative to their location:

1. **Player executables** are in `build/_artifacts/{version}/PigeonPea.Console/` or `.../PigeonPea.Windows/`
2. **Plugin directory** is at `build/_artifacts/{version}/plugins/`
3. Players discover plugins via `../plugins` relative path

This structure ensures:

- Clean separation of concerns
- Multiple versions can coexist
- Plugins are shared between console and Windows apps
- No bin directory pollution

## What Gets Published

The build system publishes projects configured in `build/nuke/build.config.json`:

```json
{
  "publishProjectPaths": [
    "projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj",
    "projects/dungeon/dotnet/windows-app/core/src/PigeonPea.Windows/PigeonPea.Windows.csproj",
    ...
  ]
}
```

## Troubleshooting

### "Could not load type" Errors

**Cause:** Old plugin DLLs with outdated namespaces in bin directories

**Solution:**

1. Use the `build` command - it automatically cleans old plugins
2. Or manually clean: `Remove-Item -Recurse -Force projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/bin/Debug/net9.0/plugins`
3. Rebuild using NUKE

### Plugin Not Found at Runtime

**Cause:** Plugin not copied to artifacts directory

**Solution:**

1. Ensure plugin is built successfully
2. Check it's being discovered during PublishPlayers (see build logs)
3. Verify plugins are in `build/_artifacts/{version}/plugins/`

### Version Not Found

**Cause:** GitVersion not available or git history issues

**Solution:**

1. Ensure you're in a git repository with commits
2. Check GitVersion is installed: `dotnet tool restore`
3. Run `dotnet gitversion` to verify

## Developer Workflow

### Standard Development Cycle

```powershell
# 1. Make code changes
# 2. Build and test
./build/nuke/build.ps1 Task

# 3. If successful, artifacts are in build/_artifacts/{version}
# 4. Run from versioned location or 'latest' symlink
```

### Quick Iteration

For rapid iteration during development, you can still use `dotnet run`:

```powershell
# Run directly from source (faster for iteration)
dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console

# But for final testing, ALWAYS use the build system
./build/nuke/build.ps1 Task
```

⚠️ **Warning:** Running with `dotnet run` uses plugins from the project's bin directory, which may have stale DLLs. For reliable testing, always use the NUKE build system.

## Summary

✅ **DO:**

- Use the `build` command for building
- Use NUKE build system (`./build/nuke/build.ps1`)
- Run executables from `build/_artifacts/latest/` or versioned directories
- Check build logs at `build/_artifacts/{version}/build-logs/` if issues occur

❌ **DON'T:**

- Use `dotnet build` for full builds
- Use `dotnet publish` directly
- Run executables from project bin directories
- Commit the `build/_artifacts/` directory (it's in .gitignore)

## Related Documentation

- [`.agent/commands/build.yaml`](../commands/build.yaml) - Build command definition
- [`build/nuke/build/Build.cs`](../../build/nuke/build/Build.cs) - NUKE build script
- [`build/nuke/build/Components/IPublish.cs`](../../build/nuke/build/Components/IPublish.cs) - Publish component
- [`.agent/rules/dotnet-architecture.md`](dotnet-architecture.md) - Architecture rules
- [`docs/guides/dotnet-tiered-architecture-guide.md`](../../docs/guides/dotnet-tiered-architecture-guide.md) - Complete architecture guide
