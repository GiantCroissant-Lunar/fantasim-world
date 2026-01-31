using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Provenance information for a reconstructed point (RFC-V2-0049 §3.4).
/// </summary>
/// <param name="KinematicsSegment">Which kinematics segment was used for this step.</param>
/// <param name="CrossedBoundary">Non-null if a plate boundary was crossed during this step.</param>
/// <param name="InterpolationFactor">Position within the segment [0,1].</param>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ReconstructionProvenance(
    [property: Key(0)] MotionSegmentId KinematicsSegment,
    [property: Key(1)] BoundaryId? CrossedBoundary,
    [property: Key(2)] double InterpolationFactor
);
