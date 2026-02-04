# Schemas

This directory contains JSON Schema definitions for validating agent and skill manifests.

## Overview

Schemas ensure that agent and skill definitions are correct, complete, and follow the expected structure. This prevents configuration errors and improves discoverability.

## Schema Files

- **skill.schema.json** - Validates skill SKILL.md front-matter (YAML header)
- **subagent.schema.json** - Validates sub-agent YAML manifests
- **orchestrator.schema.json** - Validates orchestrator agent routing rules

## Schema Validation

### What Gets Validated

1. **Required Fields**: Ensures all mandatory fields are present
2. **Data Types**: Validates correct types (string, array, object, etc.)
3. **Format**: Checks patterns (semantic versioning, kebab-case, PascalCase)
4. **Constraints**: Enforces min/max lengths, allowed values (enums)
5. **Relationships**: Verifies references between entities

### Skill Schema (skill.schema.json)

Validates the YAML front-matter in `SKILL.md` files:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Skill Front-Matter",
  "type": "object",
  "required": ["name", "version", "kind", "description", "contracts"],
  "properties": {
    "name": {
      "type": "string",
      "pattern": "^[a-z][a-z0-9-]*$",
      "description": "Skill name (kebab-case)"
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "kind": {
      "enum": ["cli", "mcp", "http", "script", "composite"]
    },
    "description": {
      "type": "string",
      "minLength": 20,
      "maxLength": 300
    },
    "inputs": {
      "type": "object",
      "additionalProperties": true
    },
    "contracts": {
      "type": "object",
      "properties": {
        "success": { "type": "string" },
        "failure": { "type": "string" }
      },
      "required": ["success", "failure"]
    }
  }
}
```

### Sub-Agent Schema (subagent.schema.json)

Validates sub-agent YAML manifests:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Sub-Agent Definition",
  "type": "object",
  "required": ["name", "description", "version", "skills", "goals"],
  "properties": {
    "name": {
      "type": "string",
      "description": "Sub-agent name (PascalCase)"
    },
    "description": {
      "type": "string",
      "minLength": 20
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "skills": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1
    },
    "goals": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1
    },
    "constraints": {
      "type": "array",
      "items": { "type": "string" }
    },
    "success_criteria": {
      "type": "array",
      "items": { "type": "string" }
    }
  }
}
```

## Usage

### Validating Skills

```bash
# Validate a single skill
scripts/validate_skills.py .agent/skills/dotnet-build/SKILL.md

# Validate all skills
scripts/validate_skills.py --all
```

### Validating Agents

```bash
# Validate a single agent
scripts/validate_agents.py .agent/agents/dotnet-build.yaml

# Validate all agents
scripts/validate_agents.py --all
```

### Pre-commit Integration

Validation is integrated into pre-commit hooks to ensure all manifests are valid before commit:

```yaml
# .pre-commit-config.yaml
- repo: local
  hooks:
    - id: validate-agent-manifests
      name: Validate Agent Manifests
      entry: scripts/validate_agents.py --all
      language: python
      pass_filenames: false
```

## Validation Scripts

Example Python validation script:

```python
import yaml
import json
from jsonschema import validate, ValidationError
from pathlib import Path

def validate_skill_frontmatter(skill_path, schema_path):
    """Validate skill YAML front-matter against schema."""
    with open(skill_path) as f:
        content = f.read()

    # Extract YAML front-matter
    if not content.startswith('---'):
        raise ValueError(f"Missing front-matter in {skill_path}")

    parts = content.split('---', 2)
    if len(parts) < 3:
        raise ValueError(f"Invalid front-matter format in {skill_path}")

    front_matter = yaml.safe_load(parts[1])

    # Validate against schema
    with open(schema_path) as f:
        schema = json.load(f)

    try:
        validate(instance=front_matter, schema=schema)
        print(f"✓ {skill_path} validated successfully")
    except ValidationError as e:
        print(f"✗ {skill_path} validation failed:")
        print(f"  {e.message}")
        raise

def validate_agent_manifest(agent_path, schema_path):
    """Validate agent YAML manifest against schema."""
    with open(agent_path) as f:
        manifest = yaml.safe_load(f)

    with open(schema_path) as f:
        schema = json.load(f)

    try:
        validate(instance=manifest, schema=schema)
        print(f"✓ {agent_path} validated successfully")
    except ValidationError as e:
        print(f"✗ {agent_path} validation failed:")
        print(f"  {e.message}")
        raise
```

## Benefits

1. **Early Error Detection**: Catch configuration errors before runtime
2. **Documentation**: Schemas serve as documentation for expected formats
3. **IDE Support**: JSON schemas enable autocomplete and validation in editors
4. **Consistency**: Ensure all manifests follow the same structure
5. **Discoverability**: Make capabilities and constraints explicit

## Schema Evolution

When updating schemas:

1. Use semantic versioning for schema files
2. Maintain backward compatibility when possible
3. Document breaking changes in RFC updates
4. Update validation scripts to handle both old and new versions during transition

## Related

- **Skills**: See `.agent/skills/README.md` for skill format
- **Agents**: See `.agent/agents/README.md` for agent format
- **RFC-004**: Agent Infrastructure Enhancement design document
- **JSON Schema**: <https://json-schema.org/>
