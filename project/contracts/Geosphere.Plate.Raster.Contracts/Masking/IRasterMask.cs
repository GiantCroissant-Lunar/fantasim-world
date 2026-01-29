using MessagePack;

namespace FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

/// <summary>
/// Current schema version for mask specifications.
/// Increment when the serialization format changes in a breaking way.
/// </summary>
public static class MaskSpecVersions
{
    /// <summary>
    /// Version 1: Initial schema with Bounds and IncludeInterior.
    /// </summary>
    public const int BoundsMaskSpecV1 = 1;
}

/// <summary>
/// A mask that can be applied to a raster frame.
/// RFC-V2-0028 §3.3 - Cookie-cutting / masking.
/// </summary>
/// <remarks>
/// This interface is domain-agnostic. Plate-specific masking implementations
/// live in the composition layer (Geosphere.Plate.Raster.Masking plugin).
/// </remarks>
public interface IRasterMask
{
    /// <summary>
    /// Applies the mask to a raster frame, returning the masked data.
    /// Pixels outside the mask are set to the no-data value.
    /// </summary>
    /// <param name="sourceFrame">The source raster frame to mask.</param>
    /// <param name="noDataValue">Value to use for masked-out pixels.</param>
    IRasterFrame ApplyMask(IRasterFrame sourceFrame, double noDataValue);

    /// <summary>
    /// Checks if a point is inside the mask region.
    /// </summary>
    bool Contains(double longitude, double latitude);
}

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
