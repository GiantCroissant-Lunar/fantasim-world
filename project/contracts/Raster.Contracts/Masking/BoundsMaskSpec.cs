using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Raster.Contracts.Masking;

/// <summary>
/// Mask specification using a geographic bounds rectangle.
/// Domain-agnostic - suitable for any coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure data record with no behavior - safe for serialization and caching.
/// </para>
/// <para>
/// SchemaVersion is included to support cache invalidation when the spec format evolves.
/// When computing cache keys, include SchemaVersion in the params hash.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct BoundsMaskSpec(
    [property: Key(0)] int SchemaVersion,
    [property: Key(1)] RasterBounds Bounds,
    [property: Key(2)] bool IncludeInterior
)
{
    /// <summary>
    /// Mask that includes points inside the bounds.
    /// </summary>
    public static BoundsMaskSpec Include(RasterBounds bounds)
        => new(MaskSpecVersions.BoundsMaskSpecV1, bounds, true);

    /// <summary>
    /// Mask that excludes points inside the bounds.
    /// </summary>
    public static BoundsMaskSpec Exclude(RasterBounds bounds)
        => new(MaskSpecVersions.BoundsMaskSpecV1, bounds, false);
}
