using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Batch output of boundary velocity analysis for all boundaries at a tick (RFC-V2-0034 §10.3).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryVelocityCollection(
    [property: Key(0)] CanonicalTick Tick,
    [property: Key(1)] ImmutableArray<BoundaryVelocityProfile> Profiles,
    [property: Key(2)] string SolverId
);
