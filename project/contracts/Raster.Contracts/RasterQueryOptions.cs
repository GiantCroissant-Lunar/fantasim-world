using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Raster.Contracts;

/// <summary>
/// Options for raster sequence queries.
/// RFC-V2-0028 §3.1.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct RasterQueryOptions(
    [property: Key(0)] InterpolationMethod Interpolation,
    [property: Key(1)] RasterBounds? ClipBounds,
    [property: Key(2)] double? NoDataValue
)
{
    /// <summary>
    /// Default options: nearest neighbor, no clipping, use raster's no-data value.
    /// </summary>
    public static RasterQueryOptions Default => new(
        InterpolationMethod.NearestNeighbor,
        null,
        null
    );

    /// <summary>
    /// Creates options with linear interpolation enabled.
    /// </summary>
    public static RasterQueryOptions WithLinearInterpolation(RasterBounds? clipBounds = null)
        => new(InterpolationMethod.Linear, clipBounds, null);

    /// <summary>
    /// Creates options with nearest neighbor (no interpolation).
    /// </summary>
    public static RasterQueryOptions WithNearestNeighbor(RasterBounds? clipBounds = null)
        => new(InterpolationMethod.NearestNeighbor, clipBounds, null);
}
