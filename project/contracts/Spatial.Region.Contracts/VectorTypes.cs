namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// 3D vector representation.
/// Structurally identical to Point3 but represents direction/magnitude, not position.
/// </summary>
public readonly record struct Vec3(double X, double Y, double Z);

/// <summary>
/// Quaternion for rotation representation.
/// </summary>
public readonly record struct Quaternion(double W, double X, double Y, double Z)
{
    /// <summary>
    /// Identity quaternion (no rotation).
    /// </summary>
    public static Quaternion Identity => new(1.0, 0.0, 0.0, 0.0);
}
