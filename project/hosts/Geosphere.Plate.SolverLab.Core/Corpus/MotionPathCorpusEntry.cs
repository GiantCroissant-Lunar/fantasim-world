using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
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
/// NOTE: RotationAxis uses Point3 as a stand-in for Vector3d until UnifyGeometry adds Vector3d type.
/// The Point3 here represents a direction vector (normalized axis of rotation).
/// </remarks>
[MessagePackObject]
public readonly record struct MotionPathInput(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] Point3 StartPoint,
    [property: Key(2)] Point3 RotationAxis,  // TODO: Replace with Vector3d when available
    [property: Key(3)] double AngularRate,
    [property: Key(4)] int StepCount,
    [property: Key(5)] int StepTicks,
    [property: Key(6)] CanonicalTick StartTick,
    [property: Key(7)] IntegrationDirection Direction
);
