using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// A single sample along a motion path (RFC-V2-0049 §3.2).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MotionPathSample(
    [property: Key(0)] CanonicalTick Tick,
    [property: Key(1)] Point3 Position,
    [property: Key(2)] PlateId PlateId,
    [property: Key(3)] Vector3d Velocity,
    [property: Key(4)] ReconstructionProvenance Provenance,
    [property: Key(5)] double AccumulatedError
);
