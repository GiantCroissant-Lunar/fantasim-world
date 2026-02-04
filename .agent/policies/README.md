# Policies

This directory contains policy definitions that enforce guardrails, standards, and safety constraints for all agents.

## Overview

Policies define the boundaries within which agents operate. They ensure:

- **Safety**: Prevent destructive or harmful actions
- **Quality**: Enforce coding standards and best practices
- **Security**: Prevent credential leaks and security vulnerabilities
- **Consistency**: Maintain uniform behavior across agents

## Policy Files

- **defaults.yaml** - General guardrails for all agents (rate limits, safety, repository boundaries)
- **coding-standards.yaml** - Coding-specific policies for .NET development

## Policy Structure

Each policy file is a YAML document with the following structure:

```yaml
name: PolicyName
description: What this policy enforces
version: 0.1.0

# Policy-specific sections
rate_limits:
  max_tool_calls_per_session: 1000

safety:
  never_commit:
    - 'bin/'
    - 'obj/'

repository_boundaries:
  allowed_directories:
    - './dotnet'
```

## Default Policies (defaults.yaml)

General guardrails applied to all agents:

### Rate Limits

Prevent runaway behavior:

```yaml
rate_limits:
  max_tool_calls_per_session: 1000
  max_file_edits_per_session: 100
  max_file_reads_per_session: 500
```

### Safety Constraints

Prevent accidental damage:

```yaml
safety:
  never_commit:
    - 'bin/'
    - 'obj/'
    - '*.exe'
    - '*.dll'
    - 'node_modules/'
    - '.vs/'

  never_delete:
    - '.git/'
    - '.agent/'
    - '*.sln'
    - '*.csproj'
    - 'README.md'
    - 'LICENSE'

  never_expose:
    - secrets
    - credentials
    - API keys
    - connection strings
    - authentication tokens
```

### Repository Boundaries

Define allowed and forbidden directories:

```yaml
repository_boundaries:
  allowed_directories:
    - './dotnet'
    - './tests'
    - './docs'
    - './.agent'
    - './.github'

  forbidden_directories:
    - './bin'
    - './obj'
    - './packages'
    - './.vs'
    - './node_modules'
```

## Coding Standards (coding-standards.yaml)

Development-specific policies for maintaining code quality:

### .NET Style

```yaml
dotnet:
  style:
    - 'Follow .editorconfig rules strictly'
    - 'Use dotnet-format before every commit'
    - 'PascalCase for public members'
    - 'camelCase for private fields (_camelCase for backing fields)'
    - 'Use explicit access modifiers'

  testing:
    - 'All public APIs must have unit tests'
    - 'Test projects follow naming: {ProjectName}.Tests'
    - 'Use xUnit/NUnit patterns consistently'
    - 'Aim for >70% code coverage on new code'
```

### Formatting

```yaml
formatting:
  tools:
    - 'dotnet-format (C#)'
    - 'prettier (JSON, YAML, Markdown)'

  enforcement:
    - 'Run pre-commit hooks before every commit'
    - 'CI must validate formatting on every PR'
    - 'Zero tolerance for formatting violations in main'
```

### Code Quality

```yaml
code_quality:
  metrics:
    cyclomatic_complexity:
      max: 15
      description: 'Maximum cyclomatic complexity per method'
    method_length:
      max: 50
      preferred: 20
      description: 'Maximum lines per method (prefer shorter)'
    class_length:
      max: 300
      preferred: 200
      description: 'Maximum lines per class (prefer shorter)'

  analysis:
    - 'Enable all Roslyn analyzers'
    - 'Treat warnings as errors in Release builds'
    - 'Fix all critical/high severity issues before merge'
```

## Policy Enforcement

### Agent-Level Enforcement

Agents check policies before executing actions:

```yaml
# .agent/agents/dotnet-build.yaml
name: DotNetBuildAgent
policies:
  - enforce: .agent/policies/defaults.yaml
  - enforce: .agent/policies/coding-standards.yaml
```

### Pre-commit Enforcement

Pre-commit hooks validate policies:

```bash
# Check that no forbidden files are staged
pre-commit run check-forbidden-files

# Validate code style
pre-commit run dotnet-format
pre-commit run prettier
```

### CI/CD Enforcement

CI pipelines enforce policies on every PR:

```yaml
# .github/workflows/quality.yml
- name: Check code standards
  run: |
    scripts/check_policies.py --all
```

## Adding a New Policy

1. Create or update policy file in `.agent/policies/`
2. Use YAML format with clear sections
3. Document each constraint with rationale
4. Reference policy in relevant agent manifests
5. Update pre-commit hooks if enforcement needed
6. Update CI/CD pipelines for continuous validation

## Policy Rationale

### Why Rate Limits?

Prevent infinite loops and runaway automation that could consume resources or make excessive changes.

### Why Safety Constraints?

Protect the repository from accidental damage. Build artifacts, dependencies, and configuration files should not be committed or deleted without careful consideration.

### Why Repository Boundaries?

Focus agent work on relevant directories. Prevent agents from modifying infrastructure or system files unintentionally.

### Why Coding Standards?

Maintain consistent, high-quality code that is easy to read, maintain, and extend.

## Policy Violations

When a policy is violated:

1. **Block the action**: Prevent the violating operation
2. **Log the violation**: Record what was attempted
3. **Provide feedback**: Explain why it was blocked and suggest alternatives
4. **Allow override**: In rare cases, allow manual override with justification

## Best Practices

1. **Be specific**: Clear, actionable constraints
2. **Explain why**: Document rationale for each policy
3. **Keep updated**: Review and update policies as project evolves
4. **Test policies**: Validate that enforcement works as expected
5. **Balance safety with productivity**: Don't over-constrain

## Related

- **Agents**: See `.agent/agents/README.md` for how agents reference policies
- **RFC-004**: Agent Infrastructure Enhancement design document
- **Pre-commit**: See `.pre-commit-config.yaml` for enforcement hooks
