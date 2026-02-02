using MessagePack;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

[MessagePackObject]
public record GridSpec
{
    /// <summary>
    /// Grid resolution in degrees (for Regular grids).
    /// E.g., 1.0 = 1-degree grid, 0.5 = half-degree grid.
    /// </summary>
    [Key(0)]
    public required double ResolutionDeg { get; init; }

    /// <summary>
    /// Whether grid nodes are at cell centers (true) or cell edges (false).
    /// Default: true (cell-centered, matching GPlately convention).
    /// </summary>
    [Key(1)]
    public bool CellCentered { get; init; } = true;

    /// <summary>
    /// Registration convention: "gridline" or "pixel".
    /// Gridline: nodes at exact lat/lon values.
    /// Pixel: nodes at cell centers offset by half-resolution.
    /// </summary>
    [Key(2)]
    public required GridRegistration Registration { get; init; }

    /// <summary>
    /// Number of latitude nodes (computed from extent + resolution).
    /// </summary>
    [Key(3)]
    public required int NLat { get; init; }

    /// <summary>
    /// Number of longitude nodes (computed from extent + resolution).
    /// </summary>
    [Key(4)]
    public required int NLon { get; init; }

    /// <summary>
    /// Total number of grid nodes: NLat * NLon.
    /// </summary>
    [IgnoreMember]
    public int NodeCount => NLat * NLon;

    public static GridSpec Global(double resolutionDeg, GridRegistration registration)
    {
        if (resolutionDeg <= 0) throw new ArgumentOutOfRangeException(nameof(resolutionDeg), "Resolution must be positive");

        int nLat, nLon;

        // Global extent covers 180 degrees lat, 360 degrees lon
        if (registration == GridRegistration.Pixel)
        {
            // Pixel registered (cell centers)
            // Example 1.0 deg: -89.5 to 89.5 (180 nodes), -179.5 to 179.5 (360 nodes)
            nLat = (int)Math.Round(180.0 / resolutionDeg);
            nLon = (int)Math.Round(360.0 / resolutionDeg);
        }
        else // GridRegistration.Gridline
        {
            // Gridline registered (cell edges/nodes)
            // Example 1.0 deg: -90 to 90 (181 nodes), -180 to 180 (361 nodes)
            nLat = (int)Math.Round(180.0 / resolutionDeg) + 1;
            nLon = (int)Math.Round(360.0 / resolutionDeg) + 1;
        }

        return new GridSpec
        {
            ResolutionDeg = resolutionDeg,
            Registration = registration,
            CellCentered = registration == GridRegistration.Pixel,
            NLat = nLat,
            NLon = nLon
        };
    }
}
