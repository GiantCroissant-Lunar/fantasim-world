# MessagePack Custom Formatter Pattern

This document describes the custom MessagePack formatter pattern used in Spatial.Region.Contracts and how to extend it.

## Problem Statement

Types from `UnifyGeometry.Primitives` (Point3, Point2) don't have MessagePack `[MessagePackObject]` attributes. The naive solution would be to duplicate these types with attributes, but this causes:
- Type mismatch errors when RegionSpec needs to interop with other plate contracts
- Code duplication and maintenance burden
- Inconsistent serialization across the codebase

## Solution: Custom Formatters

MessagePack supports custom formatters via `IMessagePackFormatter<T>` interface. This allows serialization of types without modifying their source.

### Pattern Structure

```
Spatial.Region.Contracts/
├── Formatters/
│   ├── Point3Formatter.cs    # For UnifyGeometry.Point3
│   ├── Point2Formatter.cs     # For UnifyGeometry.Point2
│   ├── Vec3Formatter.cs       # For local Vec3
│   └── RegionQuaternionFormatter.cs  # For local Quaternion
├── RegionSerializationOptions.cs  # CompositeResolver configuration
└── VectorTypes.cs            # Local type definitions (Vec3, Quaternion)
```

### 1. Creating a Custom Formatter

```csharp
using MessagePack;
using MessagePack.Formatters;

namespace FantaSim.Spatial.Region.Contracts.Formatters;

/// <summary>
/// MessagePack formatter for MyType.
/// </summary>
public sealed class MyTypeFormatter : IMessagePackFormatter<MyType>
{
    public void Serialize(ref MessagePackWriter writer, MyType value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);  // or other format
        writer.Write(value.Field1);
        writer.Write(value.Field2);
        // ...
    }

    public MyType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != expected)
        {
            throw new MessagePackSerializationException($"Invalid MyType format...");
        }

        var field1 = reader.ReadType();
        var field2 = reader.ReadType();
        // ...

        return new MyType(field1, field2);
    }
}
```

**Key requirements:**
- Implement `IMessagePackFormatter<T>`
- Use `ref MessagePackReader/Writer` for efficient streaming
- Include length validation for security
- Throw `MessagePackSerializationException` on invalid data

### 2. Registering the Formatter

In `RegionSerializationOptions.cs`:

```csharp
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

public static class RegionSerializationOptions
{
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                new Point3Formatter(),
                new Point2Formatter(),
                new Vec3Formatter(),
                new RegionQuaternionFormatter()
            },
            new IFormatterResolver[]
            {
                NativeGuidResolver.Instance,
                BuiltinResolver.Instance,
                StandardResolver.Instance
            }
        ));
}
```

### 3. Using Custom Options in Tests

```csharp
using FantaSim.Spatial.Region.Contracts;
using MessagePack;

var bytes = MessagePackSerializer.Serialize(obj, RegionSerializationOptions.Options);
var restored = MessagePackSerializer.Deserialize<T>(bytes, RegionSerializationOptions.Options);
```

**Never use** `MessagePackSerializer.Serialize(obj)` without options - it will use StandardResolver which doesn't know about custom formatters.

## Adding a New Type

### Step 1: Check if type exists in UnifyGeometry

- `Point3`, `Point2` → Use UnifyGeometry.Primitives, write formatter
- New geometry types → Consider adding to unify-maths first
- Domain-specific types (Vec3, Quaternion) → Define locally, write formatter

### Step 2: Create the formatter

Follow the template in Section 1. Place in `Formatters/` directory.

### Step 3: Register in RegionSerializationOptions

Add to the `IMessagePackFormatter[]` array.

### Step 4: Update tests

Ensure serialization tests use `RegionSerializationOptions.Options`.

## Local Type Definitions

Some types are defined locally because they don't exist in UnifyGeometry:

### Vec3 (VectorTypes.cs)
```csharp
public readonly record struct Vec3(double X, double Y, double Z);
```
- Structurally identical to Point3
- Represents direction/magnitude, not position
- Uses same `[x, y, z]` array format

### Quaternion (VectorTypes.cs)
```csharp
public readonly record struct Quaternion(double W, double X, double Y, double Z)
{
    public static Quaternion Identity => new(1.0, 0.0, 0.0, 0.0);
}
```
- Uses `[w, x, y, z]` array format
- Formatter named `RegionQuaternionFormatter` to avoid conflict with MessagePack built-in

## Canonical Hashing

For canonical SHA-256 hashing (see `RegionCanonicalMessagePackEncoder.cs`):

1. Use `RegionSerializationOptions.Options` for encoding
2. This ensures byte-for-byte identical output across runs
3. Custom formatters produce deterministic serialization

## Reference Implementations

- **Topology.SerializationOptions**: Original pattern for plate topology
- **Spatial.Region.RegionSerializationOptions**: This project's implementation
- **Point2Formatter.cs, Point3Formatter.cs**: Examples for UnifyGeometry types
