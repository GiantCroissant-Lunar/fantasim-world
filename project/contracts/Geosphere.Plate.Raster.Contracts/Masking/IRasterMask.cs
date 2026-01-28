using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

/// <summary>
/// A mask that can be applied to a raster frame.
/// RFC-V2-0028 §3.3 - Cookie-cutting / masking.
/// </summary>
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
/// Mask specification for plate polygon-based masking.
/// </summary>
[MessagePackObject]
public readonly record struct PlatePolygonMaskSpec(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] bool IncludeInterior,
    [property: Key(2)] double BufferDegrees
)
{
    /// <summary>
    /// Mask that includes only the interior of the polygon.
    /// </summary>
    public static PlatePolygonMaskSpec Interior(PlateId plateId, double bufferDegrees = 0)
        => new(plateId, true, bufferDegrees);
    
    /// <summary>
    /// Mask that excludes the interior (keeps exterior).
    /// </summary>
    public static PlatePolygonMaskSpec Exterior(PlateId plateId, double bufferDegrees = 0)
        => new(plateId, false, bufferDegrees);
}

/// <summary>
/// Mask specification using a geographic bounds rectangle.
/// </summary>
[MessagePackObject]
public readonly record struct BoundsMaskSpec(
    [property: Key(0)] RasterBounds Bounds,
    [property: Key(1)] bool IncludeInterior
)
{
    /// <summary>
    /// Mask that includes points inside the bounds.
    /// </summary>
    public static BoundsMaskSpec Include(RasterBounds bounds) => new(bounds, true);
    
    /// <summary>
    /// Mask that excludes points inside the bounds.
    /// </summary>
    public static BoundsMaskSpec Exclude(RasterBounds bounds) => new(bounds, false);
}
