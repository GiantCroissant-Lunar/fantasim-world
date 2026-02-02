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
