namespace FantaSim.Raster.Contracts.Masking;

/// <summary>
/// A mask that can be applied to a raster frame.
/// RFC-V2-0028 3.3 - Cookie-cutting / masking.
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
