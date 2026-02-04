# Setup Nuke - Detailed Procedure

> **Status:** Deprecated – This setup description is legacy. For current builds, use the shared `unify-build` repository instead of configuring Nuke from `lunar-horse-agent-hub`.

## Overview

Setup Nuke build system. pigeon-pea already has Nuke at `./build/nuke/`. This doc explains setup for reference.

## Prerequisites

- **.NET SDK 8.0+** (`dotnet --version`)
- Command line (bash/PowerShell/cmd)
- Git repository

## Current Setup (pigeon-pea)

Location: `build/nuke/`

- `.nuke/parameters.json` - Build parameters
- `build/Build.cs` - Target definitions
- `build/_build.csproj` - Build project
- `build.sh/ps1/cmd` - Bootstrap scripts

## New Project Setup

### 1. Install Nuke

```bash
dotnet tool install Nuke.GlobalTool --global
nuke --version  # Verify
```

### 2. Bootstrap

```bash
nuke :setup  # Creates build/, Build.cs, scripts
```

### 3. Configure Targets

Edit `build/Build.cs`:

```csharp
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    Target Restore => _ => _.Executes(() =>
        DotNetRestore(s => s.SetProjectFile("./solution.sln")));

    Target Compile => _ => _.DependsOn(Restore).Executes(() =>
        DotNetBuild(s => s.SetProjectFile("./solution.sln")));
}
```

### 4. Make Executable

```bash
chmod +x build.sh  # Linux/macOS
```

### 5. Test

```bash
./build.sh
```

## Build Scripts

**build.sh:** Linux/macOS, auto-installs .NET SDK if missing
**build.ps1:** Cross-platform PowerShell, Windows integration
**build.cmd:** Windows cmd.exe, wraps build.ps1

## Configuration

### Parameters

Use `[Parameter]` attribute:

```csharp
[Parameter("Configuration")]
readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
```

Pass from CLI: `./build.sh --configuration Release`

### Custom Parameters

```csharp
[Parameter("Skip tests")]
readonly bool SkipTests = false;
```

Use: `./build.sh --skip-tests`

## Target Dependencies

```csharp
.DependsOn(Restore)  // Runs Restore before this
.Before(Restore)     // Runs before Restore
.After(Compile)      // Runs after Compile
```

## Environment Detection

`IsLocalBuild` - true on dev machine
`IsServerBuild` - true on CI/CD server

```csharp
Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
```

## CI/CD Integration

**GitHub Actions:** `nuke :add-github-workflow` or add step:

```yaml
- run: ./build.sh Compile --configuration Release
```

**Azure Pipelines:**

```yaml
- task: Bash@3
  inputs:
    filePath: 'build.sh'
    arguments: 'Compile --configuration Release'
```

## Troubleshooting

**Nuke.Common not found:** `cd build && dotnet restore _build.csproj`
**Target not found:** `./build.sh --help` (case-sensitive names)
**Permission denied:** `chmod +x build.sh`
**.NET SDK not found:** Install from dotnet.microsoft.com or let script auto-download
**Windows execution policy:** `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

## Best Practices

1. Keep Build.cs clean (extract complex logic)
2. Use parameters (configurable builds)
3. Document targets (XML comments)
4. Version lock Nuke.Common in \_build.csproj
5. Test locally before CI/CD
6. Commit scripts to git

## Advanced Features

**Parallel:** `.SetMaxCpuCount(null)` (use all CPUs)
**Conditional:** `.OnlyWhenStatic(() => IsLocalBuild)`
**External tools:** Use `Nuke.Common.Tools.*` namespaces

## Related

- [Official Nuke Documentation](https://nuke.build/)
- [Nuke GitHub Repository](https://github.com/nuke-build/nuke)
- [`build-targets.md`](./build-targets.md) - Running Nuke targets
- [`../SKILL.md`](../SKILL.md) - Skill entry point
