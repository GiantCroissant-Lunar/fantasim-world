using FantaSim.Geosphere.Plate.Raster.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Masking;

/// <summary>
/// Simple raster frame wrapper for query results and temporary frame data.
/// </summary>
internal sealed class SimpleRasterFrame : IRasterFrame
{
    private readonly byte[] _data;
    private readonly CanonicalTick _tick;

    public SimpleRasterFrame(
        int width,
        int height,
        RasterBounds bounds,
        RasterDataType dataType,
        byte[] data,
        double? noDataValue,
        CanonicalTick? tick = null)
    {
        Width = width;
        Height = height;
        Bounds = bounds;
        DataType = dataType;
        _data = data;
        NoDataValue = noDataValue;
        _tick = tick ?? CanonicalTick.Genesis;
    }

    public CanonicalTick Tick => _tick;
    public int Width { get; }
    public int Height { get; }
    public RasterBounds Bounds { get; }
    public RasterDataType DataType { get; }
    public double? NoDataValue { get; }

    public ReadOnlySpan<byte> GetRawData() => _data;

    public double? GetValue(int row, int col)
    {
        if (row < 0 || row >= Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (col < 0 || col >= Width)
            throw new ArgumentOutOfRangeException(nameof(col));

        var index = row * Width + col;
        var bytes = new byte[sizeof(double)];
        Array.Copy(_data, index * sizeof(double), bytes, 0, sizeof(double));
        var value = BitConverter.ToDouble(bytes, 0);

        if (NoDataValue.HasValue)
        {
            var noData = NoDataValue.Value;
            if (double.IsNaN(noData) && double.IsNaN(value))
                return null;
            if (Math.Abs(value - noData) < 1e-10)
                return null;
        }
        else if (double.IsNaN(value))
        {
            return null;
        }

        return value;
    }

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
}
