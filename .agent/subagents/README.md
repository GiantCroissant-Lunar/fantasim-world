# Subagents

Subagent definitions for delegated, specialized tasks. Unlike `agents/` (which define orchestration roles), subagents are invoked by other agents to perform focused work.

## Available Subagents

| Subagent | Purpose |
|----------|---------|
| `doc-writer` | Technical documentation creation and updates |
| `project-builder` | Build automation and Nuke workflows |

## Usage

Subagents are invoked when the primary agent needs specialized expertise. Each subagent has:
- `name`: Identifier
- `description`: When to use this subagent
- `tools`: Available tool subset
- `model`: Preferred LLM model

## Schema

See [subagent.schema.json](../schemas/subagent.schema.json) for validation.
