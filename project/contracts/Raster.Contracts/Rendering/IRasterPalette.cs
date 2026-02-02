namespace FantaSim.Raster.Contracts.Rendering;

/// <summary>
/// Defines a color palette for raster visualization.
/// Maps raster values to colors.
/// RFC-V2-0028 §3.2 - Styling.
/// </summary>
public interface IRasterPalette
{
    /// <summary>
    /// Gets the palette name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a color for a given raster value.
    /// </summary>
    /// <param name="value">The raster value to map.</param>
    /// <returns>The color as ARGB (Alpha, Red, Green, Blue).</returns>
    uint GetColor(double value);

    /// <summary>
    /// Gets a color for a normalized value (0-1).
    /// </summary>
    /// <param name="normalizedValue">The normalized value (0-1).</param>
    /// <returns>The color as ARGB (Alpha, Red, Green, Blue).</returns>
    uint GetColorForNormalized(double normalizedValue);

    /// <summary>
    /// Gets the color for no-data values.
    /// </summary>
    uint NoDataColor { get; }

    /// <summary>
    /// Whether the palette supports continuous value mapping.
    /// </summary>
    bool IsContinuous { get; }
}
