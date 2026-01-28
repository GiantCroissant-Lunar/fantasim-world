using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;

/// <summary>
/// Exception thrown when polygonization fails due to invalid topology.
/// </summary>
public class PolygonizationException : Exception
{
    /// <summary>
    /// Diagnostics about why polygonization failed.
    /// </summary>
    public PolygonizationDiagnostics Diagnostics { get; }

    public PolygonizationException(string message, PolygonizationDiagnostics diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics;
    }

    public PolygonizationException(string message, PolygonizationDiagnostics diagnostics, Exception innerException)
        : base(message, innerException)
    {
        Diagnostics = diagnostics;
    }
}
