using MessagePack;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Surface footprint for ExtrudedPatch regions.
/// Per RFC-V2-0055 §3.4.3.
/// </summary>
[MessagePackObject]
public record SurfaceFootprint
{
    /// <summary>
    /// Footprint kind: "polygon", "plate", or "cell_set".
    /// </summary>
    [Key(0)]
    public required string Kind { get; init; }

    /// <summary>
    /// Polygon vertices on the unit sphere (when Kind = "polygon").
    /// </summary>
    [Key(1)]
    public Point3[]? PolygonVertices { get; init; }

    /// <summary>
    /// PlateId (when Kind = "plate"). Uses plate boundary as footprint.
    /// </summary>
    [Key(2)]
    public string? PlateId { get; init; }

    /// <summary>
    /// Cell IDs from a spatial index (when Kind = "cell_set").
    /// Index kind is specified in RegionSampling.
    /// </summary>
    [Key(3)]
    public string[]? CellIds { get; init; }

    /// <summary>
    /// Creates a polygon footprint.
    /// </summary>
    public static SurfaceFootprint Polygon(Point3[] vertices) => new()
    {
        Kind = "polygon",
        PolygonVertices = vertices
    };

    /// <summary>
    /// Creates a plate footprint.
    /// </summary>
    public static SurfaceFootprint Plate(string plateId) => new()
    {
        Kind = "plate",
        PlateId = plateId
    };

    /// <summary>
    /// Creates a cell set footprint.
    /// </summary>
    public static SurfaceFootprint CellSet(string[] cellIds) => new()
    {
        Kind = "cell_set",
        CellIds = cellIds
    };
}
