using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;

/// <summary>
/// RFC-V2-0046 Section 5.1: Frame-aware kinematics view.
/// Provides plate rotations expressed in any reference frame.
/// </summary>
public interface IFrameAwareKinematicsView
{
    /// <summary>
    /// Gets the rotation of a specific plate in the specified reference frame.
    /// </summary>
    /// <param name="plateId">The plate to get the rotation for.</param>
    /// <param name="tick">The time at which to evaluate the rotation.</param>
    /// <param name="frame">The reference frame to express the rotation in.</param>
    /// <returns>
    /// The finite rotation of the plate in the specified frame,
    /// or null if the plate's rotation is not available.
    /// </returns>
    FiniteRotation? GetRotationInFrame(PlateId plateId, CanonicalTick tick, ReferenceFrameId frame);

    /// <summary>
    /// Gets the rotations of all plates in the specified reference frame.
    /// </summary>
    /// <param name="tick">The time at which to evaluate the rotations.</param>
    /// <param name="frame">The reference frame to express the rotations in.</param>
    /// <returns>
    /// A dictionary mapping plate IDs to their rotations in the specified frame.
    /// Only plates with valid rotations are included.
    /// </returns>
    IReadOnlyDictionary<PlateId, FiniteRotation> GetAllRotationsInFrame(CanonicalTick tick, ReferenceFrameId frame);
}
