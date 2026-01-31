using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Traces the evolution of a spreading center or subduction zone (RFC-V2-0049 §4).
/// </summary>
public record Flowline(
    Point3 SeedPoint,
    BoundaryId SourceBoundary,
    PlateSide Side,
    CanonicalTick StartTick,
    CanonicalTick EndTick,
    SpreadingModel SpreadingModel,
    ImmutableArray<FlowlineSample> Samples
);

/// <summary>
/// A sample point along a flowline (RFC-V2-0049 §4.2).
/// </summary>
public record FlowlineSample(
    CanonicalTick Tick,
    Point3 Position,
    PlateId PlateId,
    Vector3d Velocity,
    ReconstructionProvenance Provenance,
    double AccumulatedError,
    double SpreadingRate,
    double AccumulatedOpening,
    bool IsRidgeSegment,
    double? SubductionAge
);

/// <summary>
/// Defines how spreading rates are determined (RFC-V2-0049 §4.3).
/// </summary>
public record SpreadingModel(SpreadingModelType Type);

/// <summary>
/// Types of spreading models.
/// </summary>
public enum SpreadingModelType
{
    Uniform,
    VelocityBased,
    AgeBased
}

/// <summary>
/// Solver for computing flowlines (RFC-V2-0049 §4).
/// </summary>
public interface IFlowlineSolver
{
    Flowline ComputeFlowline(
        Point3 seedPoint,
        BoundaryId boundaryId,
        PlateSide side,
        SpreadingModel spreadingModel,
        CanonicalTick tickA,
        CanonicalTick tickB,
        StepPolicy stepPolicy,
        FantaSim.Geosphere.Plate.Topology.Contracts.Derived.IPlateTopologyStateView topology,
        FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived.IPlateKinematicsStateView kinematics);

    ImmutableArray<Flowline> ComputeFlowlineBundle(
        BoundaryId boundaryId,
        PlateSide side,
        double sampleSpacing,
        SpreadingModel spreadingModel,
        CanonicalTick tickA,
        CanonicalTick tickB,
        StepPolicy stepPolicy,
        FantaSim.Geosphere.Plate.Topology.Contracts.Derived.IPlateTopologyStateView topology,
        FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived.IPlateKinematicsStateView kinematics);
}
