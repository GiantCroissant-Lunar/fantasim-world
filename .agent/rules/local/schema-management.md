# RFC-021 Schema Management Guide

This document provides practical guidance for managing JSON Schema files and generating DTOs using the RFC-021 schema-driven architecture.

---

## Quick Reference

### Generate DTOs from Schemas

```bash
# Generate DTOs for all schemas
task generate-dtos

# Preview generation without writing files
task generate-dtos:dryrun
```

### Add a New Schema-Driven Type

1. **Create JSON Schema** in `project/schemas/{domain}/{type}.schema.json`
2. **Run generation**: `task generate-dtos`
3. **Create Domain Model** in `contracts/{Project}/Models/{Type}.cs`
4. **Create Mapper** in `contracts/{Project}/Mapping/{Type}Mapper.cs`
5. **Build**: `dotnet build`

---

## Architecture Overview

### 4-Layer Stack

```
Layer 1: JSON Schema (Source of Truth)
    project/schemas/plates/dataset-manifest.schema.json

Layer 2: QuickType-Generated DTOs
    contracts/Geosphere.Plate.Datasets.Contracts/Generated/
    - Mutable, System.Text.Json attributes
    - DO NOT EDIT - Regenerated automatically

Layer 3: Domain Models
    contracts/Geosphere.Plate.Datasets.Contracts/Models/
    - Immutable records with [UnifyModel]
    - Rich behavior, validation
    - Hand-written

Layer 4: Mapperly Mappers
    contracts/Geosphere.Plate.Datasets.Contracts/Mapping/
    - Compile-time generated mapping code
    - Custom mapping methods for special cases
```

---

## File Organization

### Schema Files

```
project/schemas/
├── plates/
│   ├── dataset-manifest.schema.json
│   ├── dataset-ingest-audit.schema.json
│   ├── body-frame.schema.json
│   ├── time-mapping.schema.json
│   └── asset-types.schema.json
└── [future domains]/
```

### Naming Conventions

| Layer | File Pattern | Example |
|-------|--------------|---------|
| Schema | `{kebab-case}.schema.json` | `dataset-manifest.schema.json` |
| DTO | `{PascalCase}Dto.cs` | `PlatesDatasetManifestDto.cs` |
| Domain Model | `{PascalCase}.cs` | `PlatesDatasetManifest.cs` |
| Mapper | `{PascalCase}Mapper.cs` | `PlatesDatasetManifestMapper.cs` |

---

## JSON Schema Best Practices

### Schema Structure

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://fantasim.world/schemas/{domain}/{type}/v1.schema.json",
  "title": "PascalCaseTypeName",
  "version": "1.0.0",
  "type": "object",
  "required": ["requiredField1", "requiredField2"],
  "properties": {
    "propertyName": {
      "type": "string",
      "description": "Human-readable description"
    }
  },
  "definitions": {
    "NestedType": {
      "type": "object",
      "properties": {}
    }
  }
}
```

### Property Naming

- Use `camelCase` for property names in JSON Schema
- QuickType will convert to `PascalCase` for C# DTOs
- Be consistent with existing schemas

### Type Mappings

| JSON Schema | C# DTO | Notes |
|-------------|--------|-------|
| `"type": "string"` | `string` | |
| `"type": "integer"` | `int` or `long` | Use minimum/maximum hints |
| `"type": "number"` | `double` | |
| `"type": "boolean"` | `bool` | |
| `"type": "array"` | `T[]` or `List<T>` | |
| `"type": "object"` | Class | Use `$ref` for reuse |
| `"enum": [...]` | Enum | Generates C# enum |

---

## Domain Model Guidelines

### Basic Structure

```csharp
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Models;

[UnifyModel]
public sealed record PlatesDatasetManifest
{
    [UnifyProperty(0)]
    public required string DatasetId { get; init; }

    [UnifyProperty(1)]
    public required string BodyId { get; init; }

    // ... additional properties

    // Rich domain behavior
    public bool IsValidForBody(string bodyId) => BodyId == bodyId;
}
```

### Key Principles

1. **Immutability**: Use `init` properties, `record` types
2. **Required Members**: Use `required` keyword for mandatory fields
3. **UnifySerialization**: Add `[UnifyModel]` and `[UnifyProperty(n)]` for event serialization
4. **Validation**: Add computed properties for validation logic
5. **Null Safety**: Enable nullable reference types

---

## Mapper Guidelines

### Basic Mapper

```csharp
using Riok.Mapperly.Abstractions;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Generated;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Mapping;

[Mapper]
public partial class PlatesDatasetManifestMapper
{
    public partial PlatesDatasetManifest ToDomain(PlatesDatasetManifestDto dto);
    public partial PlatesDatasetManifestDto ToDto(PlatesDatasetManifest domain);
}
```

### Custom Mappings

```csharp
[Mapper]
public partial class ExampleMapper
{
    public partial DomainType ToDomain(DtoType dto);

    // Custom mapping for value objects
    private CustomType MapCustom(string value) => CustomType.Parse(value);
    private string MapCustom(CustomType value) => value.ToString();

    // Custom mapping for enums (DTO string → Domain enum)
    [MapEnum(EnumMappingStrategy.ByName)]
    private MyEnum MapEnum(string value) => Enum.Parse<MyEnum>(value);

    // Ignore computed properties
    [MapperIgnoreTarget(nameof(DomainType.ComputedProperty))]
    public partial DomainType ToDomain(DtoType dto);
}
```

### Global Configuration (Optional)

Create `MapperDefaults.cs` in the Mapping folder:

```csharp
using Riok.Mapperly.Abstractions;

[assembly: MapperDefaults(
    EnumMappingStrategy = EnumMappingStrategy.ByName,
    EnumMappingIgnoreCase = true,
    ThrowOnMappingNullMismatch = true
)]
```

---

## Workflow Examples

### Example 1: Adding a New Property

1. **Update Schema**:
   ```json
   {
     "properties": {
       "newProperty": { "type": "string" }
     }
   }
   ```

2. **Regenerate DTO**:
   ```bash
   task generate-dtos
   ```

3. **Update Domain Model**:
   ```csharp
   [UnifyProperty(n)]
   public string? NewProperty { get; init; }
   ```

4. **Build**:
   ```bash
   dotnet build
   ```
   - Mapperly will auto-generate mapping for the new property
   - If you forget to add to domain model, you'll get a compiler error

### Example 2: Adding a Nested Type

1. **Add to Schema**:
   ```json
   {
     "definitions": {
       "NewNestedType": {
         "type": "object",
         "properties": { ... }
       }
     },
     "properties": {
       "nested": { "$ref": "#/definitions/NewNestedType" }
     }
   }
   ```

2. **Regenerate DTOs** (includes new nested type)

3. **Create Nested Domain Model** (if needed separately)

4. **Update Mapper** with custom mapping if needed

---

## Troubleshooting

### QuickType Not Found

```bash
npm install -g quicktype
# or
npx quicktype --version
```

### Mapperly Not Generating Code

1. Verify `[Mapper]` attribute is present
2. Verify methods are declared `partial`
3. Check build output in `obj/Debug/net*/generated/Riok.Mapperly/`
4. Ensure `Riok.Mapperly` package is referenced

### Generated DTO Has Wrong Namespace

Edit `scripts/generate-dtos.ps1` and adjust the `--namespace` parameter for your domain.

### Schema Changes Not Reflected

1. Ensure you saved the `.schema.json` file
2. Re-run `task generate-dtos`
3. Check that output file was updated (timestamp)
4. Rebuild project to pick up changes

---

## Migration from Manual Mapping

When converting existing manual mapping code:

1. **Identify** the DTO and Domain Model types
2. **Create Schema** from DTO structure
3. **Generate** new DTO (should match existing)
4. **Create Mapper** with `[Mapper]` attribute
5. **Delete** old manual mapping code
6. **Update** call sites to use new mapper
7. **Verify** with tests

---

## References

- [RFC-021: Schema-Driven Domain Model Architecture](https://github.com/GiantCroissant-Lunar/fantasim-hub/blob/main/docs/rfcs/v1/architecture/021-schema-driven-domain-models.md)
- [QuickType Documentation](https://github.com/quicktype/quicktype)
- [Mapperly Documentation](https://mapperly.riok.app/)
- [JSON Schema](https://json-schema.org/)
