using System.Collections.Immutable;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Trajectory of a material point attached to a plate (RFC-V2-0035 §7.2).
/// </summary>
[MessagePackObject]
public readonly record struct MotionPath(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] CanonicalTick StartTick,
    [property: Key(2)] CanonicalTick EndTick,
    [property: Key(3)] IntegrationDirection Direction,
    [property: Key(4)] ImmutableArray<MotionPathSample> Samples
);
