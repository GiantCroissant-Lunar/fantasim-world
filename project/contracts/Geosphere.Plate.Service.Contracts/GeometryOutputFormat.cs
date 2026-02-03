namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Geometry output format options.
/// </summary>
public enum GeometryOutputFormat
{
    /// <summary>
    /// Native geometry format (implementation-specific).
    /// </summary>
    Native = 0,

    /// <summary>
    /// GeoJSON format.
    /// </summary>
    GeoJson = 1,

    /// <summary>
    /// Well-Known Text (WKT) format.
    /// </summary>
    Wkt = 2,

    /// <summary>
    /// Compact binary format.
    /// </summary>
    Binary = 3
}
