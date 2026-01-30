namespace FantaSim.Raster.Contracts.Export;

/// <summary>
/// Exports raster sequences to various formats.
/// RFC-V2-0028 §4.
/// </summary>
public interface IRasterSequenceExporter
{
    /// <summary>
    /// Exports a raster sequence according to the specification.
    /// </summary>
    /// <param name="sequence">The raster sequence to export.</param>
    /// <param name="spec">Export specification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RasterExportResult> ExportAsync(
        IRasterSequence sequence,
        RasterExportSpec spec,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the supported export formats.
    /// </summary>
    IReadOnlyCollection<RasterExportFormat> SupportedFormats { get; }
}

/// <summary>
/// Result of a raster export operation.
/// </summary>
public readonly record struct RasterExportResult(
    bool Success,
    IReadOnlyList<string> OutputFiles,
    IReadOnlyList<RasterExportError> Errors,
    int FramesExported
)
{
    public static RasterExportResult Empty => new(true, Array.Empty<string>(), Array.Empty<RasterExportError>(), 0);
    
    public static RasterExportResult Failed(params RasterExportError[] errors)
        => new(false, Array.Empty<string>(), errors, 0);
    
    public static RasterExportResult Succeeded(IReadOnlyList<string> files, int frames)
        => new(true, files, Array.Empty<RasterExportError>(), frames);
}

/// <summary>
/// Error during raster export.
/// </summary>
public readonly record struct RasterExportError(
    string Code,
    string Message,
    string? FilePath = null
)
{
    public static RasterExportError IOError(string message, string? path = null)
        => new("IO_ERROR", message, path);
    
    public static RasterExportError FormatError(string message, string? path = null)
        => new("FORMAT_ERROR", message, path);
    
    public static RasterExportError MissingFrame(string message, string? path = null)
        => new("MISSING_FRAME", message, path);
    
    public static RasterExportError InvalidSpec(string message)
        => new("INVALID_SPEC", message, null);
}
