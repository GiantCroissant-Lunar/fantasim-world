namespace FantaSim.Raster.Contracts.Rendering;

/// <summary>
/// Options for rendering raster sequences.
/// RFC-V2-0028 §3.2 - Styling.
/// </summary>
public class RasterRenderOptions
{
    /// <summary>
    /// Default render options.
    /// </summary>
    public static readonly RasterRenderOptions Default = new();

    /// <summary>
    /// Color palette to use for rendering.
    /// </summary>
    public IRasterPalette? Palette { get; set; }

    /// <summary>
    /// Opacity (0.0 to 1.0).
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// Minimum value for color mapping (auto if null).
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Maximum value for color mapping (auto if null).
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// Output image format (e.g., "png", "jpeg", "webp").
    /// </summary>
    public string OutputFormat { get; set; } = "png";

    /// <summary>
    /// Whether to interpolate between pixels.
    /// </summary>
    public bool Interpolate { get; set; } = true;

    /// <summary>
    /// Whether to apply anti-aliasing.
    /// </summary>
    public bool AntiAlias { get; set; } = true;

    /// <summary>
    /// Custom color for no-data values (ARGB format).
    /// </summary>
    public uint? NoDataColor { get; set; }

    /// <summary>
    /// Creates a copy of this options instance.
    /// </summary>
    public RasterRenderOptions Clone()
    {
        return new RasterRenderOptions
        {
            Palette = Palette,
            Opacity = Opacity,
            MinValue = MinValue,
            MaxValue = MaxValue,
            OutputFormat = OutputFormat,
            Interpolate = Interpolate,
            AntiAlias = AntiAlias,
            NoDataColor = NoDataColor
        };
    }
}
