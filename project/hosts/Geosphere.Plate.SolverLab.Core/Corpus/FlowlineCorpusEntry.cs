using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;

/// <summary>
/// Strongly-typed corpus entry for flowline test cases (RFC-V2-0035 §12).
/// </summary>
[MessagePackObject]
public sealed class FlowlineCorpusEntry
{
    [Key(0)]
    public required string CaseId { get; init; }

    [Key(1)]
    public required string Description { get; init; }

    [Key(2)]
    public required FlowlineInput Input { get; init; }

    [Key(3)]
    public required Flowline ExpectedOutput { get; init; }

    [Key(4)]
    public required CaseDifficulty Difficulty { get; init; }

    [Key(5)]
    public required string[] Tags { get; init; }
}

/// <summary>
/// Input parameters for flowline corpus cases.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct FlowlineInput(
    [property: Key(0)] BoundaryId BoundaryId,
    [property: Key(1)] BoundaryVelocitySample SeedSample,
    [property: Key(2)] PlateId LeftPlateId,
    [property: Key(3)] PlateId RightPlateId,
    [property: Key(4)] PlateSide Side,
    [property: Key(5)] int StepCount,
    [property: Key(6)] int StepTicks,
    [property: Key(7)] CanonicalTick StartTick,
    [property: Key(8)] IntegrationDirection Direction
);
