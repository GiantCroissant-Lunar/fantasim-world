namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

/// <summary>
/// Winding convention for polygon rings.
/// RFC-V2-0041 §7.
/// </summary>
public enum WindingConvention
{
    /// <summary>Outer rings are CCW, holes are CW (GeoJSON convention).</summary>
    CounterClockwise,

    /// <summary>Outer rings are CW, holes are CCW.</summary>
    Clockwise
}

/// <summary>
/// Options for plate polygonization.
/// RFC-V2-0041 §7.
/// </summary>
public readonly record struct PolygonizationOptions(
    WindingConvention Winding = WindingConvention.CounterClockwise,
    double SnapTolerance = 1e-9,
    bool AllowPartialPolygonization = false
);
