namespace FantaSim.Raster.Contracts.Rendering;

/// <summary>
/// Metadata about a render operation.
/// </summary>
public readonly record struct RasterRenderMetadata(
    string? ErrorMessage = null,
    int? FrameIndex = null,
    double? RenderTimeMs = null
);
