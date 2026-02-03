using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.TruePolarWander;

/// <summary>
/// Represents a True Polar Wander (TPW) model that provides rotations
/// accounting for the motion of Earth's spin axis relative to the mantle.
/// </summary>
/// <remarks>
/// Per RFC-V2-0046 Section 3.3:
/// - AbsoluteFrame represents motion relative to Earth's rotational axis (ITRF-like)
/// - If TPW data is available, it MUST be applied consistently
/// - Without TPW data, AbsoluteFrame MUST behave as identity transform
/// </remarks>
public interface ITruePolarWanderModel
{
    /// <summary>
    /// Gets the TPW rotation at the specified tick.
    /// </summary>
    /// <param name="tick">The canonical tick at which to evaluate the TPW rotation.</param>
    /// <returns>
    /// The finite rotation representing True Polar Wander at the given time.
    /// Returns <see cref="FiniteRotation.Identity"/> if no TPW data is available for the tick.
    /// </returns>
    FiniteRotation GetRotationAt(CanonicalTick tick);
}
