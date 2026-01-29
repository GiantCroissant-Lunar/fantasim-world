using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Raster.Masking;

/// <summary>
/// A mask based on geographic bounds.
/// Pixels inside/outside the specified bounds are masked.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class BoundsRasterMask : IRasterMask
{
    private readonly RasterBounds _bounds;
    private readonly bool _includeInterior;

    /// <summary>
    /// Creates a new BoundsRasterMask.
    /// </summary>
    public BoundsRasterMask(RasterBounds bounds, bool includeInterior)
    {
        _bounds = bounds;
        _includeInterior = includeInterior;
    }

    /// <inheritdoc />
    public IRasterFrame ApplyMask(IRasterFrame sourceFrame, double noDataValue)
    {
        if (sourceFrame == null)
            throw new ArgumentNullException(nameof(sourceFrame));

        var rawData = sourceFrame.GetRawData();
        var sourceData = new double[rawData.Length / sizeof(double)];
        Buffer.BlockCopy(rawData.ToArray(), 0, sourceData, 0, rawData.Length);

        var maskedData = new double[sourceData.Length];

        for (int row = 0; row < sourceFrame.Height; row++)
        {
            for (int col = 0; col < sourceFrame.Width; col++)
            {
                var index = row * sourceFrame.Width + col;
                var value = sourceData[index];

                // Calculate geographic coordinates
                var lon = sourceFrame.Bounds.MinLongitude +
                    (col / (double)sourceFrame.Width) * sourceFrame.Bounds.Width;
                var lat = sourceFrame.Bounds.MaxLatitude -
                    (row / (double)sourceFrame.Height) * sourceFrame.Bounds.Height;

                // Check if point is in mask region
                var inMask = Contains(lon, lat);
                var shouldInclude = _includeInterior ? inMask : !inMask;

                maskedData[index] = shouldInclude ? value : noDataValue;
            }
        }

        return new MaskedRasterFrame(
            sourceFrame.Tick,
            sourceFrame.Width,
            sourceFrame.Height,
            sourceFrame.Bounds,
            sourceFrame.DataType,
            maskedData,
            noDataValue);
    }

    /// <inheritdoc />
    public bool Contains(double longitude, double latitude)
    {
        return _bounds.Contains(longitude, latitude);
    }
}
