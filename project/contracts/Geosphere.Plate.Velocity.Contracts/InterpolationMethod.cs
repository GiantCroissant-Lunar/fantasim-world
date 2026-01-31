namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Interpolation method for computing sample positions along a boundary curve (RFC-V2-0048 §3.1).
/// </summary>
public enum InterpolationMethod
{
    /// <summary>Linear interpolation between vertices.</summary>
    Linear,

    /// <summary>Great-circle interpolation (spherical geometry).</summary>
    GreatCircle
}
