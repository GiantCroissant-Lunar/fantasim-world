using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Raster.Contracts.Rendering;

/// <summary>
/// Renders raster sequences for visualization.
/// Integrates with the rendering pipeline for map/cartography layers.
/// RFC-V2-0028 compliant.
/// </summary>
public interface IRasterSequenceRenderer
{
    /// <summary>
    /// Renders a raster frame at the specified tick.
    /// </summary>
    /// <param name="sequence">The raster sequence to render.</param>
    /// <param name="tick">The tick to render at.</param>
    /// <param name="options">Rendering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered image data.</returns>
    Task<RasterRenderResult> RenderAsync(
        IRasterSequence sequence,
        CanonicalTick tick,
        RasterRenderOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a specific frame.
    /// </summary>
    /// <param name="frame">The frame to render.</param>
    /// <param name="options">Rendering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered image data.</returns>
    Task<RasterRenderResult> RenderFrameAsync(
        IRasterFrame frame,
        RasterRenderOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a raster render operation.
/// </summary>
public readonly record struct RasterRenderResult(
    int Width,
    int Height,
    byte[] ImageData,
    string Format,
    RasterRenderMetadata Metadata
)
{
    /// <summary>
    /// Creates a failed render result.
    /// </summary>
    public static RasterRenderResult Failed(string error)
        => new(0, 0, Array.Empty<byte>(), "none", new RasterRenderMetadata(error));
}

/// <summary>
/// Metadata about a render operation.
/// </summary>
public readonly record struct RasterRenderMetadata(
    string? ErrorMessage = null,
    int? FrameIndex = null,
    double? RenderTimeMs = null
);

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
