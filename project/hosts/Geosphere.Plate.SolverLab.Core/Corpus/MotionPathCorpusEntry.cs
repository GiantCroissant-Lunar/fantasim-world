using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;

/// <summary>
/// Strongly-typed corpus entry for motion path test cases (RFC-V2-0035 §12).
/// </summary>
[MessagePackObject]
public sealed class MotionPathCorpusEntry
{
    [Key(0)]
    public required string CaseId { get; init; }

    [Key(1)]
    public required string Description { get; init; }

    [Key(2)]
    public required MotionPathInput Input { get; init; }

    [Key(3)]
    public required MotionPath ExpectedOutput { get; init; }

    [Key(4)]
    public required CaseDifficulty Difficulty { get; init; }

    [Key(5)]
    public required string[] Tags { get; init; }
}

/// <summary>
/// Input parameters for motion path corpus cases.
/// </summary>
/// <remarks>
/// <para>
/// <b>Type safety:</b> StartPoint is a <see cref="Point3"/> (position on sphere),
/// while RotationAxis is a <see cref="UnitVector3d"/> (direction/axis of rotation).
/// This distinction prevents "point vs vector" confusion bugs.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct MotionPathInput(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] Point3 StartPoint,
    [property: Key(2)] UnitVector3d RotationAxis,  // Direction vector (unit length)
    [property: Key(3)] double AngularRate,
    [property: Key(4)] int StepCount,
    [property: Key(5)] int StepTicks,
    [property: Key(6)] CanonicalTick StartTick,
    [property: Key(7)] IntegrationDirection Direction
);
