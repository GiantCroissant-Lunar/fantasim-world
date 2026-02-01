using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts;

#region Reference Frames

/// <summary>
/// Discriminated union identifying a reference frame for reconstruction.
/// </summary>
[MessagePackObject]
[Union(0, typeof(MantleFrame))]
[Union(1, typeof(PlateAnchor))]
[Union(2, typeof(AbsoluteFrame))]
[Union(3, typeof(CustomFrame))]
public abstract record ReferenceFrameId
{
    /// <summary>
    /// Gets a string representation of the frame identity.
    /// </summary>
    public abstract override string ToString();
}

/// <summary>
/// A no-net-rotation frame relative to the mantle.
/// </summary>
[MessagePackObject]
public sealed record MantleFrame : ReferenceFrameId
{
    /// <summary>
    /// Singleton instance for convenience.
    /// </summary>
    public static readonly MantleFrame Instance = new();

    public override string ToString() => "Mantle";
}

/// <summary>
/// A frame fixed to a specific plate (anchored plate).
/// </summary>
[MessagePackObject]
public sealed record PlateAnchor : ReferenceFrameId
{
    /// <summary>
    /// Gets the anchor plate identifier.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    public override string ToString() => $"Anchor({PlateId.Value})";
}

/// <summary>
/// An absolute reference frame (e.g., relative to spin axis).
/// </summary>
[MessagePackObject]
public sealed record AbsoluteFrame : ReferenceFrameId
{
    /// <summary>
    /// Singleton instance for convenience.
    /// </summary>
    public static readonly AbsoluteFrame Instance = new();

    public override string ToString() => "Absolute";
}

/// <summary>
/// A user-defined custom reference frame defined by a transform chain.
/// </summary>
[MessagePackObject]
public sealed record CustomFrame : ReferenceFrameId
{
    /// <summary>
    /// Gets the frame definition.
    /// </summary>
    [Key(0)]
    public required FrameDefinition Definition { get; init; }

    public override string ToString() => $"Custom({Definition.Name})";
}

#endregion

/// <summary>
/// Thrown when a custom frame definition contains cyclic references.
/// </summary>
public sealed class CyclicFrameReferenceException : InvalidOperationException
{
    public CyclicFrameReferenceException(string message) : base(message)
    {
    }
}

#region Frame Definition

/// <summary>
/// Defines a custom reference frame via a chain of transforms.
/// </summary>
[MessagePackObject]
public sealed record FrameDefinition
{
    /// <summary>
    /// Gets the unique name of this custom frame.
    /// </summary>
    [Key(0)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the chain of transforms defining this frame.
    /// </summary>
    [Key(1)]
    public required IReadOnlyList<FrameChainLink> Chain { get; init; }

    /// <summary>
    /// Gets optional metadata for this frame definition.
    /// </summary>
    [Key(2)]
    public FrameDefinitionMetadata? Metadata { get; init; }
}

/// <summary>
/// A single link in a frame transformation chain.
/// </summary>
[MessagePackObject]
public sealed record FrameChainLink
{
    /// <summary>
    /// Gets the base reference frame this link transforms FROM.
    /// </summary>
    [Key(0)]
    public required ReferenceFrameId BaseFrame { get; init; }

    /// <summary>
    /// Gets the fixed finite rotation transform (base -> this).
    /// </summary>
    [Key(1)]
    public required FiniteRotation Transform { get; init; }

    /// <summary>
    /// Gets the optional validity range for this link.
    /// </summary>
    [Key(2)]
    public CanonicalTickRange? ValidityRange { get; init; }

    /// <summary>
    /// Gets an optional sequence hint for deterministic ordering.
    /// </summary>
    [Key(3)]
    public int? SequenceHint { get; init; }
}

/// <summary>
/// Inclusive validity range over canonical ticks.
/// </summary>
[MessagePackObject]
public readonly record struct CanonicalTickRange
{
    [Key(0)]
    public required CanonicalTick StartTick { get; init; }

    [Key(1)]
    public required CanonicalTick EndTick { get; init; }

    public bool Contains(CanonicalTick tick) => tick >= StartTick && tick <= EndTick;
}

/// <summary>
/// Metadata for a frame definition.
/// </summary>
[MessagePackObject]
public sealed record FrameDefinitionMetadata
{
    [Key(0)]
    public string? Description { get; init; }

    [Key(1)]
    public string? Author { get; init; }
}

#endregion
