using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Contracts;

/// <summary>
/// A single raster frame at a specific tick.
/// RFC-V2-0028 §2.
/// </summary>
public interface IRasterFrame
{
    /// <summary>
    /// The tick this raster frame represents.
    /// </summary>
    CanonicalTick Tick { get; }
    
    /// <summary>
    /// Width of the raster in pixels (columns).
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// Height of the raster in pixels (rows).
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// Geographic bounds of this raster frame.
    /// </summary>
    RasterBounds Bounds { get; }
    
    /// <summary>
    /// Data type of cell values.
    /// </summary>
    RasterDataType DataType { get; }
    
    /// <summary>
    /// No-data value (null if not specified).
    /// </summary>
    double? NoDataValue { get; }
    
    /// <summary>
    /// Gets the raw pixel data as a read-only span of bytes.
    /// The data is row-major order (row by row, left to right).
    /// </summary>
    ReadOnlySpan<byte> GetRawData();
    
    /// <summary>
    /// Gets the value at a specific cell as a double.
    /// Returns null if the cell contains the no-data value.
    /// </summary>
    /// <param name="row">Row index (0-based, top to bottom).</param>
    /// <param name="col">Column index (0-based, left to right).</param>
    double? GetValue(int row, int col);
    
    /// <summary>
    /// Gets the value at a specific geographic coordinate.
    /// Returns null if out of bounds or no-data.
    /// Uses bilinear interpolation within the cell.
    /// </summary>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="latitude">Latitude in degrees.</param>
    double? GetValueAt(double longitude, double latitude);
}
