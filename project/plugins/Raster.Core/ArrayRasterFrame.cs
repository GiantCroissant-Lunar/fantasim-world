using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;

namespace FantaSim.Raster.Core;

/// <summary>
/// A raster frame backed by an in-memory array of doubles.
/// This is the standard implementation for masked frames and intermediate results.
/// RFC-V2-0028 §2 compliant.
/// </summary>
/// <remarks>
/// <para>
/// This is a domain-agnostic implementation. It knows nothing about plates,
/// polygons, or topology. It simply stores a 2D grid of values with metadata.
/// </para>
/// <para>
/// For plate-specific masking, see <c>Geosphere.Plate.Raster.Masking.Plates</c>.
/// </para>
/// </remarks>
public sealed class ArrayRasterFrame : IRasterFrame
{
    private readonly double[] _data;
    private readonly double? _noDataValue;

    /// <summary>
    /// Creates a new ArrayRasterFrame.
    /// </summary>
    /// <param name="tick">The tick this frame represents.</param>
    /// <param name="width">Width in pixels (columns).</param>
    /// <param name="height">Height in pixels (rows).</param>
    /// <param name="bounds">Geographic bounds.</param>
    /// <param name="dataType">Data type of cell values.</param>
    /// <param name="data">Row-major array of values (length = width × height).</param>
    /// <param name="noDataValue">Value indicating no-data (null if not specified).</param>
    public ArrayRasterFrame(
        CanonicalTick tick,
        int width,
        int height,
        RasterBounds bounds,
        RasterDataType dataType,
        double[] data,
        double? noDataValue)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length != width * height)
            throw new ArgumentException($"Data length ({data.Length}) must equal width × height ({width * height}).", nameof(data));

        Tick = tick;
        Width = width;
        Height = height;
        Bounds = bounds;
        DataType = dataType;
        _data = data;
        _noDataValue = noDataValue;
    }

    /// <inheritdoc />
    public CanonicalTick Tick { get; }

    /// <inheritdoc />
    public int Width { get; }

    /// <inheritdoc />
    public int Height { get; }

    /// <inheritdoc />
    public RasterBounds Bounds { get; }

    /// <inheritdoc />
    public RasterDataType DataType { get; }

    /// <inheritdoc />
    public double? NoDataValue => _noDataValue;

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetRawData()
    {
        var bytes = new byte[_data.Length * sizeof(double)];
        Buffer.BlockCopy(_data, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <inheritdoc />
    public double? GetValue(int row, int col)
    {
        if (row < 0 || row >= Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (col < 0 || col >= Width)
            throw new ArgumentOutOfRangeException(nameof(col));

        var index = row * Width + col;
        var value = _data[index];

        if (IsNoDataValue(value))
            return null;

        return value;
    }

    /// <inheritdoc />
    public double? GetValueAt(double longitude, double latitude)
    {
        if (!Bounds.Contains(longitude, latitude))
            return null;

        var cellWidth = Bounds.Width / Width;
        var cellHeight = Bounds.Height / Height;

        var col = (int)((longitude - Bounds.MinLongitude) / cellWidth);
        var row = (int)((Bounds.MaxLatitude - latitude) / cellHeight);

        col = Math.Clamp(col, 0, Width - 1);
        row = Math.Clamp(row, 0, Height - 1);

        return GetValue(row, col);
    }

    /// <summary>
    /// Gets all values as a flat array (copy).
    /// </summary>
    public double[] GetAllValues()
    {
        return _data.ToArray();
    }

    /// <summary>
    /// Gets the internal data array directly (no copy).
    /// Use with caution - modifications affect the frame.
    /// </summary>
    internal ReadOnlySpan<double> GetDataSpan() => _data;

    private bool IsNoDataValue(double value)
    {
        if (!_noDataValue.HasValue)
            return double.IsNaN(value);

        var noData = _noDataValue.Value;

        if (double.IsNaN(noData))
            return double.IsNaN(value);
        if (double.IsInfinity(noData))
            return double.IsInfinity(value) && Math.Sign(noData) == Math.Sign(value);

        const double tolerance = 1e-10;
        return Math.Abs(value - noData) < tolerance;
    }
}
