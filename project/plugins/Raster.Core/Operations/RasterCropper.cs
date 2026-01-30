using FantaSim.Raster.Contracts;

namespace FantaSim.Raster.Core.Operations;

/// <summary>
/// Crops a raster frame to a specified bounding box.
/// </summary>
public static class RasterCropper
{
    /// <summary>
    /// Crops a raster frame to the specified bounds.
    /// </summary>
    /// <param name="frame">The source frame to crop.</param>
    /// <param name="targetBounds">The bounds to crop to.</param>
    /// <returns>A new frame containing only the pixels within the target bounds.</returns>
    /// <exception cref="ArgumentException">Thrown if target bounds don't overlap with frame bounds.</exception>
    public static IRasterFrame Crop(IRasterFrame frame, RasterBounds targetBounds)
    {
        if (frame == null)
            throw new ArgumentNullException(nameof(frame));

        // Calculate the intersection of frame bounds and target bounds
        var intersection = Intersect(frame.Bounds, targetBounds);
        if (intersection == null)
            throw new ArgumentException("Target bounds do not overlap with frame bounds.", nameof(targetBounds));

        var intersectedBounds = intersection.Value;

        // Calculate pixel coordinates of the intersection
        var cellWidth = frame.Bounds.Width / frame.Width;
        var cellHeight = frame.Bounds.Height / frame.Height;

        var startCol = (int)Math.Floor((intersectedBounds.MinLongitude - frame.Bounds.MinLongitude) / cellWidth);
        var endCol = (int)Math.Ceiling((intersectedBounds.MaxLongitude - frame.Bounds.MinLongitude) / cellWidth);
        var startRow = (int)Math.Floor((frame.Bounds.MaxLatitude - intersectedBounds.MaxLatitude) / cellHeight);
        var endRow = (int)Math.Ceiling((frame.Bounds.MaxLatitude - intersectedBounds.MinLatitude) / cellHeight);

        // Clamp to frame dimensions
        startCol = Math.Clamp(startCol, 0, frame.Width);
        endCol = Math.Clamp(endCol, 0, frame.Width);
        startRow = Math.Clamp(startRow, 0, frame.Height);
        endRow = Math.Clamp(endRow, 0, frame.Height);

        var newWidth = endCol - startCol;
        var newHeight = endRow - startRow;

        if (newWidth <= 0 || newHeight <= 0)
            throw new ArgumentException("Cropped region has zero size.", nameof(targetBounds));

        // Extract the cropped data
        var croppedData = new double[newWidth * newHeight];

        for (int row = 0; row < newHeight; row++)
        {
            for (int col = 0; col < newWidth; col++)
            {
                var sourceRow = startRow + row;
                var sourceCol = startCol + col;
                var value = frame.GetValue(sourceRow, sourceCol);
                croppedData[row * newWidth + col] = value ?? (frame.NoDataValue ?? double.NaN);
            }
        }

        // Calculate actual bounds of cropped region
        // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
        var actualBounds = new RasterBounds(
            frame.Bounds.MinLongitude + startCol * cellWidth,  // MinLon
            frame.Bounds.MinLongitude + endCol * cellWidth,    // MaxLon
            frame.Bounds.MaxLatitude - endRow * cellHeight,    // MinLat
            frame.Bounds.MaxLatitude - startRow * cellHeight); // MaxLat

        return new ArrayRasterFrame(
            frame.Tick,
            newWidth,
            newHeight,
            actualBounds,
            frame.DataType,
            croppedData,
            frame.NoDataValue);
    }

    private static RasterBounds? Intersect(RasterBounds a, RasterBounds b)
    {
        var minLon = Math.Max(a.MinLongitude, b.MinLongitude);
        var maxLon = Math.Min(a.MaxLongitude, b.MaxLongitude);
        var minLat = Math.Max(a.MinLatitude, b.MinLatitude);
        var maxLat = Math.Min(a.MaxLatitude, b.MaxLatitude);

        if (minLon >= maxLon || minLat >= maxLat)
            return null;

        // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
        return new RasterBounds(minLon, maxLon, minLat, maxLat);
    }
}
