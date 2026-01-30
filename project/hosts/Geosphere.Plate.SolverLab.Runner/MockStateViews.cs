using System.Collections.Immutable;
using TimeDete = Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using MessagePack;

namespace FantaSim.Geosphere.Plate.SolverLab.Runner;

using CanonicalTick = TimeDete.CanonicalTick;
using Topology = FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Mock implementation of IPlateTopologyStateView for corpus generation.
/// </summary>
[MessagePackObject]
public class MockTopologyStateView : IPlateTopologyStateView
{
    [Key(0)]
    public TruthStreamIdentity Identity { get; init; } = new("test", "trunk", 1, Domain.Parse("geo.plates.test"), "0");

    [Key(1)]
    public ImmutableDictionary<PlateId, Topology.Entities.Plate> Plates { get; init; }

    [Key(2)]
    public ImmutableDictionary<BoundaryId, Topology.Entities.Boundary> Boundaries { get; init; }

    [Key(3)]
    public ImmutableDictionary<JunctionId, Topology.Entities.Junction> Junctions { get; init; } = ImmutableDictionary<JunctionId, Topology.Entities.Junction>.Empty;

    [Key(4)]
    public long LastEventSequence { get; init; } = 0;

    // Non-serialized interface implementations
    [IgnoreMember]
    IReadOnlyDictionary<PlateId, Topology.Entities.Plate> IPlateTopologyStateView.Plates => Plates;

    [IgnoreMember]
    IReadOnlyDictionary<BoundaryId, Topology.Entities.Boundary> IPlateTopologyStateView.Boundaries => Boundaries;

    [IgnoreMember]
    IReadOnlyDictionary<JunctionId, Topology.Entities.Junction> IPlateTopologyStateView.Junctions => Junctions;

    public MockTopologyStateView()
    {
        Plates = ImmutableDictionary<PlateId, Topology.Entities.Plate>.Empty;
        Boundaries = ImmutableDictionary<BoundaryId, Topology.Entities.Boundary>.Empty;
    }

    public MockTopologyStateView(Dictionary<PlateId, Topology.Entities.Plate> plates, Dictionary<BoundaryId, Topology.Entities.Boundary> boundaries)
    {
        Plates = plates.ToImmutableDictionary();
        Boundaries = boundaries.ToImmutableDictionary();
    }
}

/// <summary>
/// Mock implementation of IPlateKinematicsStateView for corpus generation.
/// Returns constant angular velocities for plates.
/// </summary>
[MessagePackObject]
public class MockKinematicsStateView : IPlateKinematicsStateView
{
    [Key(0)]
    public TruthStreamIdentity Identity { get; init; } = new("test", "trunk", 1, Domain.Parse("geo.plates.kinematics.test"), "0");

    [Key(1)]
    public long LastEventSequence { get; init; } = 0;

    [Key(2)]
    public ImmutableDictionary<PlateId, AngularVelocity3d> AngularVelocities { get; init; }

    public MockKinematicsStateView()
    {
        AngularVelocities = ImmutableDictionary<PlateId, AngularVelocity3d>.Empty;
    }

    public MockKinematicsStateView(Dictionary<PlateId, AngularVelocity3d> angularVelocities)
    {
        AngularVelocities = angularVelocities.ToImmutableDictionary();
    }

    public bool TryGetRotation(PlateId plateId, TimeDete.CanonicalTick tick, out Quaterniond rotation)
    {
        // Compute rotation from angular velocity: θ = ω * t
        // For corpus generation, we use a simple linear rotation model
        if (AngularVelocities.TryGetValue(plateId, out var omega))
        {
            double angle = omega.Rate() * tick.Value;
            if (angle > 0)
            {
                var (axisX, axisY, axisZ) = omega.GetAxis();
                rotation = Quaterniond.FromAxisAngle(new Vector3d(axisX, axisY, axisZ), angle);
                return true;
            }
        }

        rotation = Quaterniond.Identity;
        return false;
    }
}
