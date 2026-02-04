# Build Artifacts and Logs

> **Status:** Deprecated – artifact and log locations are now defined in the `unify-build` repository's build documentation.

Rules for locating and analyzing build artifacts and logs in the PigeonPea project.

## Artifact Structure

The Nuke build system generates versioned artifacts in `build/_artifacts/`.

### Directory Layout

```
build/_artifacts/
├── {version}/                           # Versioned build (e.g., 0.0.1-claude-repo-organization-20251114.1)
│   ├── build-logs/                      # Build-time logs
│   │   └── publish-players-*.log       # Timestamped build logs
│   ├── PigeonPea.Console/              # Console app artifacts
│   │   ├── logs/                        # Runtime logs
│   │   ├── PigeonPea.Console.exe       # Executable
│   │   ├── *.dll                        # Dependencies
│   │   └── appsettings.json            # Configuration
│   ├── PigeonPea.Windows/              # Windows app artifacts
│   │   └── logs/                        # Runtime logs
│   └── plugins/                         # Plugin artifacts
└── latest/                              # Symlink/copy of latest build
    ├── build-logs/                      # Latest build logs
    ├── PigeonPea.Console/
    │   └── logs/                        # Latest runtime logs
    ├── PigeonPea.Windows/
    │   └── logs/
    └── plugins/
```

## Log Locations

### Build-Time Logs

**Location:** `build/_artifacts/{version}/build-logs/`

**Latest:** `build/_artifacts/latest/build-logs/` (synced after each build)

**Naming:** `publish-players-{timestamp}.log` (e.g., `publish-players-2025-11-18T09-18-53Z.log`)

**Purpose:**

- Nuke build output
- Compilation errors and warnings
- Publishing process logs
- Build timing and performance

**When to check:**

- Build failures
- Compilation errors
- Publishing issues
- Performance analysis

**How to access:**

```bash
# Latest build log
ls -lt build/_artifacts/latest/build-logs/ | head -2

# Read latest log
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Specific version
cat build/_artifacts/0.0.1-*/build-logs/publish-players-*.log
```

### Runtime Logs

**Location:** `build/_artifacts/{version}/PigeonPea.*/logs/`

**Latest:** `build/_artifacts/latest/PigeonPea.Console/logs/`

**Purpose:**

- Application execution logs
- Serilog output (Console + File sinks)
- Error messages and exceptions
- Gameplay events and debugging

**When to check:**

- Runtime crashes
- Application errors
- Unexpected behavior
- Debugging gameplay issues

**How to access:**

```bash
# Console app runtime logs
ls -la build/_artifacts/latest/PigeonPea.Console/logs/

# Read latest log
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"

# Windows app runtime logs
ls -la build/_artifacts/latest/PigeonPea.Windows/logs/
```

## Agent Workflow for Debugging

### 1. Build Errors

When build fails:

```bash
# Step 1: Check latest build log
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Step 2: Identify errors
grep -i "error\|failed" build/_artifacts/latest/build-logs/*.log

# Step 3: Extract file paths and line numbers
grep -E "\.cs\([0-9]+,[0-9]+\)" build/_artifacts/latest/build-logs/*.log

# Step 4: Read problematic files and fix
```

### 2. Runtime Errors

When application crashes or behaves unexpectedly:

```bash
# Step 1: Check runtime logs
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"

# Step 2: Look for exceptions
grep -i "exception\|error" build/_artifacts/latest/PigeonPea.Console/logs/*.log

# Step 3: Find stack traces
grep -A 20 "Exception" build/_artifacts/latest/PigeonPea.Console/logs/*.log

# Step 4: Identify failing component and fix
```

### 3. After Running Application

Always check logs after running:

```bash
# Build and run
task game:build-and-run-console

# Wait for app to close, then check logs
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
```

## Autonomous Debugging Rules

### Rule 1: Always Check Logs

After any build or run command, **always check the logs** for errors, even if the command appeared to succeed.

```bash
# After build
task game:build-console
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)" | grep -i "error\|warning"

# After run
task game:run-console
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
```

### Rule 2: Build Logs for Compilation Issues

Build-time logs contain:

- C# compiler errors (CS####)
- NuGet restore issues
- Publishing errors
- File path references

**Always read build logs when:**

- Build fails
- Warnings appear
- Publishing fails
- Dependencies are missing

### Rule 3: Runtime Logs for Execution Issues

Runtime logs contain:

- Application exceptions
- Stack traces
- Plugin loading errors
- Gameplay errors

**Always read runtime logs when:**

- App crashes
- Unexpected behavior occurs
- Features don't work
- Debugging gameplay

### Rule 4: Latest vs Versioned

**Use `latest/` for debugging:**

```bash
build/_artifacts/latest/build-logs/         # Current build logs
build/_artifacts/latest/PigeonPea.Console/logs/  # Current runtime logs
```

**Use versioned for history:**

```bash
build/_artifacts/0.0.1-*/build-logs/        # Historical build logs
build/_artifacts/0.0.1-*/PigeonPea.Console/logs/  # Historical runtime logs
```

## Common Log Patterns

### Build Log Errors

```
error CS0246: The type or namespace name 'Foo' could not be found
error CS0103: The name 'bar' does not exist in the current context
error MSB3073: The command "..." exited with code 1
```

**Action:** Read compiler error, identify file/line, fix code, rebuild.

### Runtime Log Errors

```
[ERR] System.NullReferenceException: Object reference not set to an instance of an object.
   at PigeonPea.Console.GameView.Render() in GameView.cs:line 67
   at PigeonPea.Console.GameApplication.Run() in GameApplication.cs:line 45
```

**Action:** Read stack trace, identify failing method, fix null reference, rebuild and rerun.

### Plugin Loading Errors

```
[ERR] Failed to load plugin: PigeonPea.Plugins.Hud.TerminalGui
[ERR] Could not load file or assembly 'Terminal.Gui, Version=...'
```

**Action:** Check plugin dependencies, ensure DLLs are present, rebuild.

## Integration with Workflows

Workflows should incorporate log checking:

### Build-and-Test Workflow

```yaml
- name: Build
  type: command
  command: task game:build-console
  post_action: |
    # Check build logs for errors
    cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"
    grep -i "error" build/_artifacts/latest/build-logs/*.log
```

### Run Workflow

```yaml
- name: Run application
  type: command
  command: task game:run-console
  post_action: |
    # Check runtime logs
    cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
```

### Fix-Bug Workflow

```yaml
- name: Reproduce bug
  type: command
  command: task game:build-and-run-console
  post_action: |
    # Capture runtime logs
    cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)" > /tmp/bug-repro.log

    # Identify error
    grep -i "exception\|error" /tmp/bug-repro.log
```

## Quick Reference Commands

```bash
# Latest build log
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Latest runtime log (Console)
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"

# Latest runtime log (Windows)
cat "$(ls -t build/_artifacts/latest/PigeonPea.Windows/logs/*.log 2>/dev/null | head -1)"

# Search for errors in build logs
grep -i "error" build/_artifacts/latest/build-logs/*.log

# Search for exceptions in runtime logs
grep -i "exception" build/_artifacts/latest/PigeonPea.Console/logs/*.log

# Count errors in latest build
grep -c "error" "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Show last 50 lines of latest runtime log
tail -50 "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
```

## Best Practices

### For Agents

1. **Always check logs** after build/run, even if command succeeds
2. **Read full logs** before reporting "success" - hidden errors may exist
3. **Extract error context** - include file paths, line numbers, stack traces
4. **Use latest/** for current work, **versioned/** for comparisons
5. **Report findings** - summarize errors found in logs

### For Workflows

1. **Include log checks** in all build/run steps
2. **Capture error output** for analysis
3. **Fail fast** if critical errors found in logs
4. **Save diagnostic info** from logs when debugging

### For Debugging

1. **Build logs first** - check compilation before runtime
2. **Runtime logs second** - check execution after build
3. **Stack traces** - work from innermost exception outward
4. **File paths** - use log references to locate problem code

## Related

- **Autonomous Development**: `.agent/rules/autonomous-development.md`
- **Workflows**: `.agent/workflows/SCHEMA.md`
- **Build System**: `Taskfile.yml`
- **Nuke Build**: `build/nuke/build.ps1`
