using MessagePack;
using UnifyGeometry; // For Point3

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

[MessagePackObject]
public record SamplingDomain
{
    // TODO: Implement proper hashing logic to generate DomainId
    // For now, we will rely on constructor or factory to set it.

    /// <summary>
    /// Unique identifier for this sampling domain configuration.
    /// Deterministic hash of all fields below.
    /// </summary>
    [Key(0)]
    public required string DomainId { get; init; }

    /// <summary>
    /// The type of sampling domain.
    /// </summary>
    [Key(1)]
    public required SamplingDomainType DomainType { get; init; }

    /// <summary>
    /// Spatial extent (lat/lon bounding box). Null means global.
    /// </summary>
    [Key(2)]
    public LatLonExtent? Extent { get; init; }

    /// <summary>
    /// The grid specification (required for Regular and EqualArea types).
    /// </summary>
    [Key(3)]
    public GridSpec? Grid { get; init; }

    /// <summary>
    /// Explicit point set (required for Explicit type).
    /// </summary>
    [Key(4)]
    public Point3[]? Points { get; init; }

    /// <summary>
    /// Mask: only sample where this predicate returns true.
    /// Null means no masking (sample everywhere in extent).
    /// </summary>
    [Key(5)]
    public string? MaskPredicateId { get; init; }

    public static SamplingDomain Global(double resolutionDeg, GridRegistration registration = GridRegistration.Pixel)
    {
        var grid = GridSpec.Global(resolutionDeg, registration);

        // Simple distinct string for now, will implement proper hash later if needed or in a builder
        // Using a composite string as ID is often easier for debugging than a hash
        string id = $"global-{resolutionDeg:F6}-{registration}".ToLowerInvariant();

        return new SamplingDomain
        {
            DomainId = id,
            DomainType = SamplingDomainType.Regular,
            Extent = new LatLonExtent { MinLat = -90, MaxLat = 90, MinLon = -180, MaxLon = 180 },
            Grid = grid
        };
    }
}
