using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public interface IPlateReconstructionSolver
{
    IReadOnlyList<ReconstructedBoundary> ReconstructBoundaries(
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null);
}
