using MessagePack;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

[MessagePackObject]
public record LatLonExtent
{
    [Key(0)] public required double MinLat { get; init; }
    [Key(1)] public required double MaxLat { get; init; }
    [Key(2)] public required double MinLon { get; init; }
    [Key(3)] public required double MaxLon { get; init; }
}
