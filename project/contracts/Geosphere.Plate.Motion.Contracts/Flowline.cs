using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Material path seeded from a boundary sample (RFC-V2-0035 §7.3).
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct Flowline(
    [property: Key(0)] BoundaryId BoundaryId,
    [property: Key(1)] int SeedIndex,
    [property: Key(2)] PlateSide Side,
    [property: Key(3)] CanonicalTick StartTick,
    [property: Key(4)] CanonicalTick EndTick,
    [property: Key(5)] IntegrationDirection Direction,
    [property: Key(6)] ImmutableArray<MotionPathSample> Samples
);
