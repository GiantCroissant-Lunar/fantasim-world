using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;

public interface IPlateKinematicsStateView
{
    TruthStreamIdentity Identity { get; }

    long LastEventSequence { get; }

    bool TryGetRotation(
        PlateId plateId,
        CanonicalTick tick,
        out Quaterniond rotation);
}
