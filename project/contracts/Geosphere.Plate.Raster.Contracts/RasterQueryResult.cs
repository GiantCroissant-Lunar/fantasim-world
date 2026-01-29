using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Contracts;

/// <summary>
/// Result of a raster sequence query at a specific tick.
/// RFC-V2-0028 §3.1.
/// </summary>
[MessagePackObject]
public readonly record struct RasterQueryResult(
    [property: Key(0)] CanonicalTick TargetTick,
    [property: Key(1)] CanonicalTick? SourceFrameTick,
    [property: Key(2)] bool IsInterpolated,
    [property: Key(3)] double? InterpolationWeight,
    [property: Key(4)] RasterFrameData? FrameData
)
{
    /// <summary>
    /// True if a valid result was found.
    /// </summary>
    [IgnoreMember]
    public bool HasData => FrameData.HasValue;

    /// <summary>
    /// Creates a result for an exact frame match (no interpolation).
    /// </summary>
    public static RasterQueryResult Exact(CanonicalTick tick, RasterFrameData data)
        => new(tick, tick, false, null, data);

    /// <summary>
    /// Creates a result for an interpolated value between two frames.
    /// </summary>
    public static RasterQueryResult Interpolated(
        CanonicalTick targetTick,
        CanonicalTick sourceFrameTick,
        double weight,
        RasterFrameData data)
        => new(targetTick, sourceFrameTick, true, weight, data);

    /// <summary>
    /// Creates a "not found" result.
    /// </summary>
    public static RasterQueryResult NotFound(CanonicalTick targetTick)
        => new(targetTick, null, false, null, null);
}

/// <summary>
/// Serializable raster frame data returned from queries.
/// </summary>
[MessagePackObject]
public readonly record struct RasterFrameData(
    [property: Key(0)] int Width,
    [property: Key(1)] int Height,
    [property: Key(2)] RasterBounds Bounds,
    [property: Key(3)] RasterDataType DataType,
    [property: Key(4)] double? NoDataValue,
    [property: Key(5)] byte[] RawData
);
