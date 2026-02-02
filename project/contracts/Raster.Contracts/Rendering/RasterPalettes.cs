namespace FantaSim.Raster.Contracts.Rendering;

/// <summary>
/// Predefined palette types.
/// </summary>
public static class RasterPalettes
{
    /// <summary>
    /// Traditional age grid palette (blue to red).
    /// </summary>
    public static readonly IRasterPalette AgeGrid = new AgeGridPalette();

    /// <summary>
    /// Elevation palette (blue to white to brown).
    /// </summary>
    public static readonly IRasterPalette Elevation = new ElevationPalette();

    /// <summary>
    /// Grayscale palette.
    /// </summary>
    public static readonly IRasterPalette Grayscale = new GrayscalePalette();

    /// <summary>
    /// Viridis palette (perceptually uniform).
    /// </summary>
    public static readonly IRasterPalette Viridis = new ViridisPalette();
}
