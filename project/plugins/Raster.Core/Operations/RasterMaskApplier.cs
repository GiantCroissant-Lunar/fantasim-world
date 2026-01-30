using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Masking;

namespace FantaSim.Raster.Core.Operations;

/// <summary>
/// Applies a mask to a raster frame.
/// </summary>
/// <remarks>
/// This is a stateless operation helper. The actual masking logic is in the mask implementations.
/// </remarks>
public static class RasterMaskApplier
{
    /// <summary>
    /// Applies a mask to a raster frame, returning a new frame with masked pixels set to no-data.
    /// </summary>
    /// <param name="frame">The source frame to mask.</param>
    /// <param name="mask">The mask to apply.</param>
    /// <param name="noDataValue">Value to use for masked-out pixels (default: NaN).</param>
    /// <returns>A new frame with the mask applied.</returns>
    public static IRasterFrame Apply(IRasterFrame frame, IRasterMask mask, double noDataValue = double.NaN)
    {
        if (frame == null)
            throw new ArgumentNullException(nameof(frame));
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));

        return mask.ApplyMask(frame, noDataValue);
    }
}
