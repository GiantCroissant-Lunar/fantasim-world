using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Context;

/// <summary>
/// Provides the current plates truth stream identities (topology + kinematics) for reconstruction queries.
/// </summary>
/// <remarks>
/// RFC-V2-0045 requires derived query identity to be scoped by (VariantId, BranchId, L, Domain, M).
/// The query-service surface should not require each host to pass those axes on every query call.
/// </remarks>
public interface IPlatesTruthStreamSelection
{
    PlatesTruthStreamSelection GetCurrent();
}

public sealed record PlatesTruthStreamSelection(
    TruthStreamIdentity TopologyStream,
    TruthStreamIdentity KinematicsStream);
