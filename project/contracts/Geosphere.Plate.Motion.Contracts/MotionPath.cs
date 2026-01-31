using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Trajectory of a material point attached to a plate (RFC-V2-0049 §3.2).
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct MotionPath(
    [property: Key(0)] PlateId AnchorPlate,
    [property: Key(1)] CanonicalTick StartTick,
    [property: Key(2)] CanonicalTick EndTick,
    [property: Key(3)] IntegrationDirection Direction,
    [property: Key(4)] ReferenceFrameId Frame,
    [property: Key(5)] ImmutableArray<MotionPathSample> Samples
);
