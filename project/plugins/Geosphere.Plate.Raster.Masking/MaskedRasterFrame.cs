using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Raster.Contracts;

namespace FantaSim.Geosphere.Plate.Raster.Masking;

/// <summary>
/// A raster frame that has been masked.
/// Used internally by MaskedRasterSequence.
/// RFC-V2-0028 compliant.
/// </summary>
internal sealed class MaskedRasterFrame : IRasterFrame
{
    private readonly double[] _data;
    private readonly double? _noDataValue;

    public MaskedRasterFrame(
        CanonicalTick tick,
        int width,
        int height,
        RasterBounds bounds,
        RasterDataType dataType,
        double[] data,
        double? noDataValue)
    {
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

    /// <summary>
    /// Gets all values as a flat array.
    /// </summary>
    public double[] GetAllValues()
    {
        return _data.ToArray();
    }
}
