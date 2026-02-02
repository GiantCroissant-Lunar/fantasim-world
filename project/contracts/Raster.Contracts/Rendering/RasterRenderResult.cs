namespace FantaSim.Raster.Contracts.Rendering;

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
