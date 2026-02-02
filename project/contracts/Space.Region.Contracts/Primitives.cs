using MessagePack;

namespace FantaSim.Space.Region.Contracts;

/// <summary>
/// 3D coordinate point.
/// </summary>
[MessagePackObject]
public readonly record struct Point3(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z
);

/// <summary>
/// 3D vector.
/// </summary>
[MessagePackObject]
public readonly record struct Vec3(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z
);

/// <summary>
/// Quaternion for rotation representation.
/// </summary>
[MessagePackObject]
public readonly record struct Quaternion(
    [property: Key(0)] double W,
    [property: Key(1)] double X,
    [property: Key(2)] double Y,
    [property: Key(3)] double Z
)
{
    /// <summary>
    /// Identity quaternion (no rotation).
    /// </summary>
    public static Quaternion Identity => new(1.0, 0.0, 0.0, 0.0);
}
