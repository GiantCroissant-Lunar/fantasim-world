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

    private sealed class AgeGridPalette : IRasterPalette
    {
        public string Name => "AgeGrid";
        public uint NoDataColor => 0xFF000000; // Black with full alpha

        public bool IsContinuous => true;

        public uint GetColor(double value)
        {
            // Map value to 0-1 range (assuming 0-200 Ma for age grids)
            var normalized = Math.Clamp(value / 200.0, 0.0, 1.0);
            return GetColorForNormalized(normalized);
        }

        public uint GetColorForNormalized(double normalized)
        {
            // Blue (young) to Red (old) gradient
            var n = Math.Clamp(normalized, 0.0, 1.0);
            var r = (byte)(n * 255);
            var g = (byte)(0);
            var b = (byte)((1 - n) * 255);
            return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
        }
    }

    private sealed class ElevationPalette : IRasterPalette
    {
        public string Name => "Elevation";
        public uint NoDataColor => 0xFF00FF00; // Green with full alpha

        public bool IsContinuous => true;

        public uint GetColor(double value)
        {
            // Map value to 0-1 range (assuming -11000 to 8848 meters)
            var normalized = Math.Clamp((value + 11000) / 19848.0, 0.0, 1.0);
            return GetColorForNormalized(normalized);
        }

        public uint GetColorForNormalized(double normalized)
        {
            var n = Math.Clamp(normalized, 0.0, 1.0);
            byte r, g, b;

            if (n < 0.5)
            {
                // Blue to white (ocean)
                var t = n * 2;
                r = g = b = (byte)(t * 255);
            }
            else
            {
                // White to brown (land)
                var t = (n - 0.5) * 2;
                r = (byte)(139 + (255 - 139) * (1 - t));
                g = (byte)(69 + (255 - 69) * (1 - t));
                b = (byte)(19 + (255 - 19) * (1 - t));
            }

            return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
        }
    }

    private sealed class GrayscalePalette : IRasterPalette
    {
        public string Name => "Grayscale";
        public uint NoDataColor => 0xFF000000; // Black with full alpha

        public bool IsContinuous => true;

        public uint GetColor(double value)
        {
            var normalized = Math.Clamp(value, 0.0, 1.0);
            return GetColorForNormalized(normalized);
        }

        public uint GetColorForNormalized(double normalized)
        {
            var n = Math.Clamp(normalized, 0.0, 1.0);
            var gray = (byte)(n * 255);
            return (uint)((255 << 24) | (gray << 16) | (gray << 8) | gray);
        }
    }

    private sealed class ViridisPalette : IRasterPalette
    {
        public string Name => "Viridis";
        public uint NoDataColor => 0xFF000000; // Black with full alpha

        public bool IsContinuous => true;

        public uint GetColor(double value)
        {
            var normalized = Math.Clamp(value, 0.0, 1.0);
            return GetColorForNormalized(normalized);
        }

        public uint GetColorForNormalized(double normalized)
        {
            // Simplified viridis approximation
            var n = Math.Clamp(normalized, 0.0, 1.0);
            byte r, g, b;

            if (n < 0.25)
            {
                var t = n * 4;
                r = (byte)(68 + (33 - 68) * t);
                g = (byte)(1 + (144 - 1) * t);
                b = (byte)(84 + (140 - 84) * t);
            }
            else if (n < 0.5)
            {
                var t = (n - 0.25) * 4;
                r = (byte)(33 + (59 - 33) * t);
                g = (byte)(144 + (82 - 144) * t);
                b = (byte)(140 + (81 - 140) * t);
            }
            else if (n < 0.75)
            {
                var t = (n - 0.5) * 4;
                r = (byte)(59 + (253 - 59) * t);
                g = (byte)(82 + (231 - 82) * t);
                b = (byte)(81 + (37 - 81) * t);
            }
            else
            {
                var t = (n - 0.75) * 4;
                r = (byte)(253 + (253 - 253) * t);
                g = (byte)(231 + (191 - 231) * t);
                b = (byte)(37 + (111 - 37) * t);
            }

            return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
        }
    }
}
