# Autonomous Development Rules

## Core Principle

**You are autonomous. Build, run, test, and fix code yourself. Do not ask the user to do these tasks.**

## Build and Run Workflow

### ALWAYS Do This

1. **Build the project yourself**

   ```bash
   task game:build-console
   # or build entire solution
   task dotnet:build
   ```

2. **Capture and analyze build output**
   - Read compiler errors and warnings
   - Identify files and line numbers
   - Understand the root cause

3. **Fix issues autonomously**
   - Read the problematic files
   - Make necessary corrections
   - Rebuild to verify fixes

4. **Run the application yourself**

   ```bash
   task game:run-console
   # or build and run in one command
   task game:build-and-run-console
   ```

5. **Check runtime logs**

   ```bash
   # Read latest runtime log
   cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
   ```

   - Capture console output
   - Check logs for errors, warnings, or unexpected behavior
   - Analyze stack traces if exceptions occur

6. **Iterate until success**
   - Fix runtime errors
   - Rebuild and rerun
   - Check logs again
   - Repeat until application runs as expected

### NEVER Do This

- ❌ Ask user to "build and paste the output"
- ❌ Ask user to "run the app and tell me what happens"
- ❌ Request user to "check if it compiles"
- ❌ Say "please run this and let me know if it works"
- ❌ Stop after writing code without verifying it works

## When to Ask the User

Only ask the user for:

- **Design decisions**: "Should we use Strategy A or Strategy B?"
- **Requirements clarification**: "What should happen when X occurs?"
- **Feature preferences**: "Which behavior do you prefer?"
- **Stuck situations**: "I've tried X, Y, Z and still seeing this fundamental issue. Need guidance."

## Common Scenarios

### Scenario: User Reports Bug

❌ **WRONG:**

> "Can you run the app and paste the error message?"

✅ **RIGHT:**

> "Let me build and run the app to see the issue..."
> _[executes build, captures errors, fixes them, reruns]_
> "I found the issue in GameView.cs:42. It was a null reference. Fixed and verified it works now."

### Scenario: After Code Changes

❌ **WRONG:**

> "I've updated the code. Please build and test it."

✅ **RIGHT:**

> _[builds the project]_
> _[runs the application]_
> _[verifies output is correct]_
> "Updated the code and verified it builds and runs successfully. The new feature is working as expected."

### Scenario: Build Errors

❌ **WRONG:**

> "There might be some build errors. Can you check?"

✅ **RIGHT:**

> _[runs build, gets errors]_
> _[reads error messages: "CS0246: The type or namespace name 'Foo' could not be found"]_
> _[opens the file, adds missing using statement]_
> _[rebuilds successfully]_
> "Fixed missing using statement in EntryPoint.cs. Build now succeeds."

## Efficient Error Fixing

### Compiler Errors

1. Parse error format: `File.cs(line,col): error CS####: message`
2. Extract file path and line number
3. Read the specific file
4. Identify and fix the issue
5. Rebuild to verify

### Runtime Errors

1. Capture full stack trace
2. Identify the topmost application code (not framework code)
3. Read the relevant file and surrounding context
4. Understand the root cause
5. Fix and rerun

### Multiple Errors

- Fix related errors together in one pass
- Start with the first error (others may be cascading)
- Rebuild after each logical group of fixes

## Project-Specific Commands

**This project uses [Task](https://taskfile.dev) as the build runner. Always use `task` commands.**

### Console App (Main Game)

```bash
# Build console app (RECOMMENDED)
task game:build-console

# Run console app
task game:run-console

# Build and run (one command)
task game:build-and-run-console
```

### Full .NET Solution

```bash
# Build entire solution
task dotnet:build

# Run tests
task dotnet:test

# Format code
task dotnet:format
```

### Other Useful Tasks

```bash
# List all available tasks
task --list

# Run pre-commit hooks
pre-commit run --all-files

# Build Rust dev-tool-server
task rust:build-and-run
```

### Direct dotnet Commands (Avoid These)

Use `task` commands instead, but if needed:

```bash
# Build solution directly
cd dotnet && dotnet build PigeonPea.sln

# Run console app directly
dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console
```

## Verification Checklist

Before telling the user "done":

- [ ] Code builds without errors
- [ ] Code builds without warnings (or warnings are expected)
- [ ] **Build logs checked** - No hidden errors
- [ ] Application runs without crashes
- [ ] Application produces expected output
- [ ] **Runtime logs checked** - No exceptions or errors
- [ ] Feature works as intended

## Log Locations

**Build logs:** `build/_artifacts/latest/build-logs/*.log`
**Runtime logs:** `build/_artifacts/latest/PigeonPea.Console/logs/*.log`

**Quick commands:**

```bash
# Check latest build log
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Check latest runtime log
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
```

See `.agent/rules/build-artifacts-logs.md` for complete guide.

## Communication Style

Be direct and results-focused:

✅ "Built and tested. Working correctly."
✅ "Fixed 3 compiler errors. App now runs successfully."
✅ "Implemented feature X. Verified it works with test scenario Y."

❌ "I've made the changes. Could you test it?"
❌ "This should work. Let me know if you see any errors."
❌ "Try building it and tell me what happens."

## Autonomy Levels

### High Autonomy (Default for Windsurf)

- Build, run, test, fix autonomously
- Show results and summary to user
- Only ask for design decisions

### Medium Autonomy

- Execute commands but explain what you're doing
- Show intermediate results
- Confirm major changes

### Low Autonomy

- Ask before executing build/run commands
- Explain trade-offs before making changes
- Seek approval for fixes

**For this project, use HIGH AUTONOMY by default.**

## Examples

### Example 1: Fixing a Build Error

```
User: "The GameView class isn't working"

Agent:
[runs: task game:build-console]
[sees error: CS0103: The name 'renderer' does not exist]
[reads GameView.cs, identifies missing field]
[adds private IRenderer _renderer field]
[runs: task game:build-console - success]
[runs: task game:run-console - verifies it works]

Response: "Fixed GameView.cs - was missing the _renderer field declaration. Built and tested successfully."
```

### Example 2: Implementing New Feature

```
User: "Add a pause menu"

Agent:
[reads existing menu code for patterns]
[implements PauseMenu class]
[integrates into GameApplication]
[runs: task game:build-console - gets error about missing reference]
[adds using statement]
[runs: task game:build-console - success]
[runs: task game:run-console, presses pause key, verifies menu appears]
[tests resume functionality]

Response: "Implemented pause menu with Resume/Quit options. Press 'P' to pause. Tested and working."
```

## Remember

**You have all the tools to build, run, and verify code. Use them. Be autonomous.**
