using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Service.Contracts;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

/// <summary>
/// High-level query service for plate reconstruction operations per RFC-V2-0045 section 3.
/// Provides unified access to reconstruction, plate assignment, and velocity queries
/// with policy-based configuration.
/// </summary>
public interface IPlateReconstructionQueryService
{
    /// <summary>
    /// Reconstructs a feature set at the specified target tick per RFC-V2-0045 section 3.1.
    /// </summary>
    /// <param name="featureSetId">Identifier of the feature set to reconstruct.</param>
    /// <param name="targetTick">The canonical tick to reconstruct features at.</param>
    /// <param name="policy">
    /// Unified reconstruction policy that controls frame selection, kinematics model,
    /// partition tolerance, and provenance strictness.
    /// </param>
    /// <returns>
    /// Result containing reconstructed features, provenance chain, and query metadata.
    /// </returns>
    ReconstructResult Reconstruct(
        FeatureSetId featureSetId,
        CanonicalTick targetTick,
        ReconstructionPolicy policy);

    /// <summary>
    /// Queries the plate assignment for a point at the specified tick per RFC-V2-0045 section 3.2.
    /// </summary>
    /// <param name="point">The 3D point to query plate assignment for.</param>
    /// <param name="tick">The canonical tick at which to evaluate plate assignment.</param>
    /// <param name="policy">
    /// Unified reconstruction policy that controls frame selection, kinematics model,
    /// partition tolerance, and provenance strictness.
    /// </param>
    /// <returns>
    /// Result containing assigned plate ID, confidence level, candidate plates,
    /// distance to boundary, and provenance chain.
    /// </returns>
    PlateAssignmentResult QueryPlateId(
        Point3 point,
        CanonicalTick tick,
        ReconstructionPolicy policy);

    /// <summary>
    /// Queries the velocity at a point at the specified tick per RFC-V2-0045 section 3.3.
    /// </summary>
    /// <param name="point">The 3D point to query velocity for.</param>
    /// <param name="tick">The canonical tick at which to evaluate velocity.</param>
    /// <param name="policy">
    /// Unified reconstruction policy that controls frame selection, kinematics model,
    /// partition tolerance, and provenance strictness.
    /// </param>
    /// <returns>
    /// Result containing total velocity, velocity decomposition, plate ID,
    /// and provenance chain.
    /// </returns>
    VelocityResult QueryVelocity(
        Point3 point,
        CanonicalTick tick,
        ReconstructionPolicy policy);
}
