using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;

namespace FantaSim.Raster.GeoTiff;

/// <summary>
/// Implementation of IRasterFrame for GeoTiff data.
/// Supports lazy loading of cell data.
/// </summary>
public sealed class GeoTiffRasterFrame : IRasterFrame, IDisposable
{
    private readonly Lazy<double[]> _data;
    private readonly double? _noDataValue;
    private bool _disposed;

    /// <summary>
    /// Creates a new GeoTiffRasterFrame with pre-loaded data.
    /// </summary>
    public GeoTiffRasterFrame(
        CanonicalTick tick,
        int width,
        int height,
        RasterBounds bounds,
        RasterDataType dataType,
        double[] data,
        double? noDataValue = null)
    {
        Tick = tick;
        Width = width;
        Height = height;
        Bounds = bounds;
        DataType = dataType;
        _noDataValue = noDataValue;
        _data = new Lazy<double[]>(() => data);
    }

    /// <summary>
    /// Creates a new GeoTiffRasterFrame with lazy data loading.
    /// </summary>
    public GeoTiffRasterFrame(
        CanonicalTick tick,
        int width,
        int height,
        RasterBounds bounds,
        RasterDataType dataType,
        Func<double[]> dataLoader,
        double? noDataValue = null)
    {
        Tick = tick;
        Width = width;
        Height = height;
        Bounds = bounds;
        DataType = dataType;
        _noDataValue = noDataValue;
        _data = new Lazy<double[]>(dataLoader);
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
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Convert double[] to byte[]
        var data = _data.Value;
        var bytes = new byte[data.Length * sizeof(double)];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <inheritdoc />
    public double? GetValue(int row, int col)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (row < 0 || row >= Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (col < 0 || col >= Width)
            throw new ArgumentOutOfRangeException(nameof(col));

        var index = row * Width + col;
        var value = _data.Value[index];

        // Check for no-data value using proper comparison
        if (IsNoDataValue(value))
        {
            return null;
        }

        return value;
    }

    /// <inheritdoc />
    public double? GetValueAt(double longitude, double latitude)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check bounds
        if (!Bounds.Contains(longitude, latitude))
            return null;

        // Calculate cell indices
        var cellWidth = Bounds.Width / Width;
        var cellHeight = Bounds.Height / Height;

        var col = (int)((longitude - Bounds.MinLongitude) / cellWidth);
        var row = (int)((Bounds.MaxLatitude - latitude) / cellHeight);

        // Clamp to valid range
        col = Math.Clamp(col, 0, Width - 1);
        row = Math.Clamp(row, 0, Height - 1);

        return GetValue(row, col);
    }

    /// <summary>
    /// Checks if the given value matches the no-data value.
    /// Uses tolerance-based comparison for floating point values.
    /// </summary>
    private bool IsNoDataValue(double value)
    {
        if (!_noDataValue.HasValue)
            return double.IsNaN(value);

        // For floating point no-data, use tolerance comparison
        var noData = _noDataValue.Value;

        // Handle special cases
        if (double.IsNaN(noData))
            return double.IsNaN(value);
        if (double.IsInfinity(noData))
            return double.IsInfinity(value) && Math.Sign(noData) == Math.Sign(value);

        // Tolerance-based comparison for normal values
        const double tolerance = 1e-10;
        return Math.Abs(value - noData) < tolerance;
    }

    /// <summary>
    /// Gets a copy of all values as a flat array.
    /// </summary>
    public double[] GetAllValues()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.Value.ToArray();
    }

    /// <summary>
    /// Gets a region of the raster as a 2D array.
    /// </summary>
    public double[,] GetRegion(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (rowStart < 0 || rowEnd > Height || rowStart >= rowEnd)
            throw new ArgumentOutOfRangeException(nameof(rowStart));
        if (colStart < 0 || colEnd > Width || colStart >= colEnd)
            throw new ArgumentOutOfRangeException(nameof(colStart));

        var rowCount = rowEnd - rowStart;
        var colCount = colEnd - colStart;
        var result = new double[rowCount, colCount];

        var data = _data.Value;

        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                var srcIndex = (rowStart + r) * Width + (colStart + c);
                result[r, c] = data[srcIndex];
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the underlying data array (for internal use).
    /// </summary>
    internal double[] GetDataArray()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.Value;
    }

    /// <summary>
    /// Gets a read-only span view of the data without copying (for internal use).
    /// </summary>
    internal ReadOnlySpan<double> GetDataSpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.Value.AsSpan();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
