# Agent Definitions

This directory contains sub-agent definitions that specialize in specific development tasks.

## Overview

Sub-agents are specialized actors that handle specific domains of work. The orchestrator agent routes user requests to the appropriate sub-agent based on task intent.

## Sub-Agents

- **orchestrator.yaml** - Top-level router that delegates to specialized sub-agents
- **dotnet-build.yaml** - Handles .NET build and compilation tasks
- **code-review.yaml** - Handles code formatting and quality analysis
- **testing.yaml** - Handles test execution and coverage generation

## Format

Each agent is defined in YAML with:

- `name`: Agent name (PascalCase)
- `description`: Clear description of responsibilities
- `version`: Semantic version
- `skills`: List of skills this agent can invoke
- `goals`: What this agent aims to achieve
- `constraints`: Limitations and boundaries
- `success_criteria`: How to measure success

## Example Structure

```yaml
name: DotNetBuildAgent
description: Handles .NET build, restore, compile, and packaging tasks
version: 0.1.0

skills:
  - dotnet-build

goals:
  - Produce deterministic, reproducible builds
  - Ensure all dependencies restored correctly
  - Generate build artifacts (binaries, packages)
  - Report build warnings and errors clearly

constraints:
  - Work only within ./dotnet directory
  - Never commit build artifacts (bin/, obj/)
  - Report build warnings and errors with context
  - Ensure builds are idempotent

success_criteria:
  - 'Build succeeds with zero errors'
  - 'Artifacts generated in expected output path'
  - 'Build logs attached with full context'
  - 'Dependencies restored successfully'
```

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                   Orchestrator Agent                 │
│          Routes user requests to sub-agents          │
└─────────────────────┬────────────────────────────────┘
                      │
                      │ Delegation based on intent
                      │
        ┌─────────────┼─────────────┐
        │             │             │
┌───────▼──────┐ ┌────▼─────┐ ┌────▼────────┐
│ Build Agent  │ │Code Agent│ │ Test Agent  │
│              │ │          │ │             │
│ - dotnet     │ │- format  │ │- dotnet test│
│   build      │ │- analyze │ │- coverage   │
│ - nuke       │ │- review  │ │- benchmarks │
│   (future)   │ │          │ │             │
└──────┬───────┘ └────┬─────┘ └─────┬───────┘
       │              │              │
       └──────────────┴──────────────┘
                      │
              Invokes Skills Layer
```

## Validation

Sub-agent manifests are validated against `.agent/schemas/subagent.schema.json` to ensure:

- Required fields are present
- Skills reference existing skill definitions
- Version follows semantic versioning
- Constraints and goals are clearly defined

## Adding a New Sub-Agent

1. Create a new YAML file: `.agent/agents/your-agent-name.yaml`
2. Follow the format shown in the example above
3. Reference skills from `.agent/skills/`
4. Validate using: `scripts/validate_agents.py` (when available)
5. Update the orchestrator's routing rules to delegate to your new sub-agent

## Related

- **Skills**: See `.agent/skills/README.md` for skill definitions
- **Orchestrator**: Top-level routing logic
- **RFC-004**: Agent Infrastructure Enhancement design document
