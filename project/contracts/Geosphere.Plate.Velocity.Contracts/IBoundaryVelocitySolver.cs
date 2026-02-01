using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Solves boundary velocity analysis for plate boundaries.
/// </summary>
/// <remarks>
/// <para>
/// <b>RFC-V2-0034:</b> Computes relative velocities and rates along plate boundaries,
/// producing per-sample data and per-boundary aggregate statistics.
/// </para>
/// <para>
/// <b>Normal orientation:</b> The normal vector is computed as n₀ = normalize(cross(p, t))
/// where p is the position and t is the tangent. For boundaries where left→right indicates
/// convergence (per boundary metadata), the normal is flipped to ensure positive values
/// indicate convergence and negative values indicate divergence.
/// </para>
/// <para>
/// <b>Determinism:</b> Same inputs MUST produce identical outputs. The AnalyzeAllBoundaries
/// method sorts profiles by BoundaryId.Value for deterministic ordering.
/// </para>
/// <para>
/// <b>Fallback:</b> Returns zero rates when kinematics data is missing for either plate.
/// This matches the fallback policy from RFC-V2-0033.
/// </para>
/// </remarks>
public interface IBoundaryVelocitySolver
{
    /// <summary>
    /// Analyzes a single boundary, producing per-sample velocities and per-boundary aggregates.
    /// </summary>
    /// <param name="boundary">The boundary to analyze.</param>
    /// <param name="sampling">Sampling specification.</param>
    /// <param name="tick">The target simulation time.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <returns>Velocity profile with samples and aggregates for the boundary.</returns>
    BoundaryVelocityProfile AnalyzeBoundary(
        Boundary boundary,
        BoundarySampleSpec sampling,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics);

    /// <summary>
    /// Analyzes all boundaries at a tick, producing deterministic ordering by BoundaryId.
    /// </summary>
    /// <param name="boundaries">The boundaries to analyze.</param>
    /// <param name="sampling">Sampling specification.</param>
    /// <param name="tick">The target simulation time.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <returns>Collection of velocity profiles sorted by BoundaryId.</returns>
    BoundaryVelocityCollection AnalyzeAllBoundaries(
        IEnumerable<Boundary> boundaries,
        BoundarySampleSpec sampling,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics);
}
