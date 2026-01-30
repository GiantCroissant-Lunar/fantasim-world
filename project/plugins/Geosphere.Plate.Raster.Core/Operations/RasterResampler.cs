using FantaSim.Geosphere.Plate.Raster.Contracts;

namespace FantaSim.Geosphere.Plate.Raster.Core.Operations;

/// <summary>
/// Resamples a raster frame to a different resolution.
/// </summary>
public static class RasterResampler
{
    /// <summary>
    /// Resamples a raster frame to the specified dimensions.
    /// </summary>
    /// <param name="frame">The source frame to resample.</param>
    /// <param name="targetWidth">Target width in pixels.</param>
    /// <param name="targetHeight">Target height in pixels.</param>
    /// <param name="method">Interpolation method to use.</param>
    /// <returns>A new frame with the resampled data.</returns>
    public static IRasterFrame Resample(
        IRasterFrame frame,
        int targetWidth,
        int targetHeight,
        InterpolationMethod method = InterpolationMethod.Linear)
    {
        if (frame == null)
            throw new ArgumentNullException(nameof(frame));
        if (targetWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetWidth));
        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight));

        var resampledData = new double[targetWidth * targetHeight];
        var noData = frame.NoDataValue ?? double.NaN;

        var targetCellWidth = frame.Bounds.Width / targetWidth;
        var targetCellHeight = frame.Bounds.Height / targetHeight;

        for (int row = 0; row < targetHeight; row++)
        {
            for (int col = 0; col < targetWidth; col++)
            {
                // Calculate geographic coordinates of target pixel center
                var lon = frame.Bounds.MinLongitude + (col + 0.5) * targetCellWidth;
                var lat = frame.Bounds.MaxLatitude - (row + 0.5) * targetCellHeight;

                var value = method switch
                {
                    InterpolationMethod.NearestNeighbor => SampleNearest(frame, lon, lat),
                    InterpolationMethod.Linear => SampleBilinear(frame, lon, lat),
                    _ => SampleBilinear(frame, lon, lat)
                };

                resampledData[row * targetWidth + col] = value ?? noData;
            }
        }

        return new ArrayRasterFrame(
            frame.Tick,
            targetWidth,
            targetHeight,
            frame.Bounds,
            frame.DataType,
            resampledData,
            frame.NoDataValue);
    }

    /// <summary>
    /// Resamples a raster frame by a scale factor.
    /// </summary>
    /// <param name="frame">The source frame to resample.</param>
    /// <param name="scaleFactor">Scale factor (2.0 = double resolution, 0.5 = half resolution).</param>
    /// <param name="method">Interpolation method to use.</param>
    public static IRasterFrame ResampleByFactor(
        IRasterFrame frame,
        double scaleFactor,
        InterpolationMethod method = InterpolationMethod.Linear)
    {
        var targetWidth = (int)Math.Max(1, Math.Round(frame.Width * scaleFactor));
        var targetHeight = (int)Math.Max(1, Math.Round(frame.Height * scaleFactor));
        return Resample(frame, targetWidth, targetHeight, method);
    }

    private static double? SampleNearest(IRasterFrame frame, double lon, double lat)
    {
        return frame.GetValueAt(lon, lat);
    }

    private static double? SampleBilinear(IRasterFrame frame, double lon, double lat)
    {
        if (!frame.Bounds.Contains(lon, lat))
            return null;

        var cellWidth = frame.Bounds.Width / frame.Width;
        var cellHeight = frame.Bounds.Height / frame.Height;

        // Calculate fractional pixel coordinates
        var x = (lon - frame.Bounds.MinLongitude) / cellWidth - 0.5;
        var y = (frame.Bounds.MaxLatitude - lat) / cellHeight - 0.5;

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var xFrac = x - x0;
        var yFrac = y - y0;

        // Clamp to valid range
        x0 = Math.Clamp(x0, 0, frame.Width - 1);
        x1 = Math.Clamp(x1, 0, frame.Width - 1);
        y0 = Math.Clamp(y0, 0, frame.Height - 1);
        y1 = Math.Clamp(y1, 0, frame.Height - 1);

        // Get four corner values
        var v00 = frame.GetValue(y0, x0);
        var v10 = frame.GetValue(y0, x1);
        var v01 = frame.GetValue(y1, x0);
        var v11 = frame.GetValue(y1, x1);

        // If any corner is no-data, use nearest neighbor instead
        if (!v00.HasValue || !v10.HasValue || !v01.HasValue || !v11.HasValue)
        {
            return SampleNearest(frame, lon, lat);
        }

        // Bilinear interpolation
        var top = v00.Value * (1 - xFrac) + v10.Value * xFrac;
        var bottom = v01.Value * (1 - xFrac) + v11.Value * xFrac;
        var result = top * (1 - yFrac) + bottom * yFrac;

        return result;
    }
}
