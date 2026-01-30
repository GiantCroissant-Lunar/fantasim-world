using MessagePack;

namespace FantaSim.Raster.Contracts;

/// <summary>
/// Metadata for a raster sequence (consistent across all frames).
/// </summary>
[MessagePackObject]
public readonly record struct RasterMetadata(
    [property: Key(0)] int Width,
    [property: Key(1)] int Height,
    [property: Key(2)] RasterBounds Bounds,
    [property: Key(3)] RasterDataType DataType,
    [property: Key(4)] double? NoDataValue,
    [property: Key(5)] string? CoordinateSystem,
    [property: Key(6)] string? Units
)
{
    /// <summary>
    /// Cell width in degrees.
    /// </summary>
    [IgnoreMember]
    public double CellWidth => Bounds.Width / Width;

    /// <summary>
    /// Cell height in degrees.
    /// </summary>
    [IgnoreMember]
    public double CellHeight => Bounds.Height / Height;

    /// <summary>
    /// Total number of cells.
    /// </summary>
    [IgnoreMember]
    public int CellCount => Width * Height;

    /// <summary>
    /// Size of each cell in bytes.
    /// </summary>
    [IgnoreMember]
    public int BytesPerCell => DataType switch
    {
        RasterDataType.UInt8 or RasterDataType.Int8 => 1,
        RasterDataType.UInt16 or RasterDataType.Int16 => 2,
        RasterDataType.UInt32 or RasterDataType.Int32 or RasterDataType.Float32 => 4,
        RasterDataType.Float64 => 8,
        _ => 4
    };

    /// <summary>
    /// Total size of raster data in bytes.
    /// </summary>
    [IgnoreMember]
    public int DataSizeBytes => CellCount * BytesPerCell;
}
