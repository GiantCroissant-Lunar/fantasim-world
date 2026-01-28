namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Exception thrown when combinatorial map construction fails.
/// </summary>
public class CMapBuildException : Exception
{
    public CMapBuildException(string message) : base(message) { }
    public CMapBuildException(string message, Exception innerException) : base(message, innerException) { }
}
