using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

namespace FantaSim.Geosphere.Plate.Raster.Core;

/// <summary>
/// A mask based on geographic bounds.
/// Pixels inside/outside the specified bounds are masked.
/// RFC-V2-0028 §3.3 compliant.
/// </summary>
/// <remarks>
/// This is a domain-agnostic mask. It knows nothing about plates or topology.
/// For plate-specific masks, see <c>Geosphere.Plate.Raster.Masking.Plates</c>.
/// </remarks>
public sealed class BoundsRasterMask : IRasterMask
{
    private readonly RasterBounds _bounds;
    private readonly bool _includeInterior;

    /// <summary>
    /// Creates a new BoundsRasterMask.
    /// </summary>
    /// <param name="bounds">The geographic bounds to use for masking.</param>
    /// <param name="includeInterior">True to include pixels inside bounds, false to exclude them.</param>
    public BoundsRasterMask(RasterBounds bounds, bool includeInterior)
    {
        _bounds = bounds;
        _includeInterior = includeInterior;
    }

    /// <summary>
    /// The bounds used for masking.
    /// </summary>
    public RasterBounds Bounds => _bounds;

    /// <summary>
    /// Whether pixels inside the bounds are included (true) or excluded (false).
    /// </summary>
    public bool IncludeInterior => _includeInterior;

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

        return new ArrayRasterFrame(
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
