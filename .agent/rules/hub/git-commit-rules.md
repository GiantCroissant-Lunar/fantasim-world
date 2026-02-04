# Git and Commit Rules

## Pre-commit Hook Requirement

**CRITICAL: All commits MUST pass pre-commit hooks before being committed.**

### Why This Matters

Pre-commit hooks ensure:

- Code formatting standards are met
- Security checks pass (no secrets committed)
- Syntax errors are caught early
- Consistent code quality across the project

### Enforcement

1. **Before Every Commit:**

   ```bash
   pre-commit run --all-files
   ```

2. **Automatic Checks:** Pre-commit hooks run automatically on `git commit`

3. **Never Skip Hooks:** Do NOT use `--no-verify` flag unless absolutely necessary and approved

### What Gets Checked

The pre-commit hooks (configured in [`.pre-commit-config.yaml`](../../.pre-commit-config.yaml)) check:

- Code formatting (dotnet format, black, isort, prettier)
- Security (gitleaks, detect-secrets)
- Syntax validation (YAML, JSON)
- Code quality (flake8, trailing whitespace, etc.)

## Commit Message Standards

- Write clear, descriptive commit messages
- Use conventional commit format when possible:
  - `feat:` for new features
  - `fix:` for bug fixes
  - `docs:` for documentation changes
  - `refactor:` for code refactoring
  - `test:` for test changes
  - `chore:` for maintenance tasks

## Branch Naming

- Feature branches: `claude/<description>-<session-id>`
- Bug fix branches: `fix/<description>`
- Documentation branches: `docs/<description>`

## Commit Workflow

1. Make code changes
2. Run pre-commit hooks: `pre-commit run --all-files`
3. Fix any issues identified
4. Stage changes: `git add <files>`
5. Commit: `git commit -m "message"`
6. If commit fails, fix issues and try again
7. Push to remote: `git push -u origin <branch-name>`

## Handling Pre-commit Failures

When pre-commit hooks fail:

1. **Read the error message carefully** - it tells you what failed
2. **Fix the issues** - most formatting issues can be auto-fixed by re-running hooks
3. **Re-stage files** - if hooks modified files: `git add <files>`
4. **Try again** - commit after all checks pass

### Common Failures

- **Formatting issues:** Auto-fixed by formatters, just re-stage and commit
- **Security alerts:** Review carefully, add to ignore files only if false positive
- **Syntax errors:** Fix the code syntax errors
- **Large files:** Remove or add to `.gitignore`

## Security Requirements

- **Never commit secrets, API keys, or credentials**
- All commits are scanned by gitleaks and detect-secrets
- False positives can be added to:
  - [`.gitleaksignore`](../../.gitleaksignore) for gitleaks
  - [`.secrets.baseline`](../../.secrets.baseline) for detect-secrets

## Exception Policy

Skipping pre-commit hooks is **strongly discouraged**. Only skip when:

1. Emergency hotfix required
2. Pre-commit infrastructure is broken
3. Explicitly approved by team lead

Even in these cases, follow up immediately to ensure code meets standards.
