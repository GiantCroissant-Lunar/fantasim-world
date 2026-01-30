using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

namespace FantaSim.Raster.Contracts.Loading;

/// <summary>
/// Loads raster sequences from dataset assets.
/// RFC-V2-0028 §2.
/// </summary>
public interface IRasterSequenceLoader
{
    /// <summary>
    /// Checks if this loader can handle the given asset format.
    /// </summary>
    bool CanLoad(string format);
    
    /// <summary>
    /// Loads a raster sequence from a dataset asset.
    /// </summary>
    /// <param name="asset">The raster sequence asset from the manifest.</param>
    /// <param name="dataset">The parent dataset for resolving paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IRasterSequence> LoadAsync(
        RasterSequenceAsset asset,
        IPlatesDataset dataset,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Supported file formats (e.g., "geotiff", "netcdf", "asc").
    /// </summary>
    IReadOnlyCollection<string> SupportedFormats { get; }
}

/// <summary>
/// Registry for raster sequence loaders.
/// </summary>
public interface IRasterSequenceLoaderRegistry
{
    /// <summary>
    /// Registers a loader for specific formats.
    /// </summary>
    void Register(IRasterSequenceLoader loader);
    
    /// <summary>
    /// Gets the appropriate loader for a format.
    /// </summary>
    /// <returns>The loader, or null if no loader supports the format.</returns>
    IRasterSequenceLoader? GetLoader(string format);
}
