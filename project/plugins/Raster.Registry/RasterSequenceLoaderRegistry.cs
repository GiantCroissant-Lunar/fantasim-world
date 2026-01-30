using System.Collections.Concurrent;
using FantaSim.Raster.Contracts.Loading;

namespace FantaSim.Raster.Registry;

/// <summary>
/// Default implementation of IRasterSequenceLoaderRegistry.
/// Thread-safe registry for raster sequence loaders.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class RasterSequenceLoaderRegistry : IRasterSequenceLoaderRegistry
{
    private readonly ConcurrentDictionary<string, IRasterSequenceLoader> _loaders;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new RasterSequenceLoaderRegistry.
    /// </summary>
    public RasterSequenceLoaderRegistry()
    {
        _loaders = new ConcurrentDictionary<string, IRasterSequenceLoader>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void Register(IRasterSequenceLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);

        foreach (var format in loader.SupportedFormats)
        {
            _loaders[format.ToLowerInvariant()] = loader;
        }
    }

    /// <inheritdoc />
    public IRasterSequenceLoader? GetLoader(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return null;

        return _loaders.TryGetValue(format.ToLowerInvariant(), out var loader) ? loader : null;
    }

    /// <summary>
    /// Gets all registered loaders.
    /// </summary>
    public IReadOnlyCollection<IRasterSequenceLoader> GetAllLoaders()
    {
        lock (_lock)
        {
            return _loaders.Values.Distinct().ToList();
        }
    }

    /// <summary>
    /// Gets all supported formats.
    /// </summary>
    public IReadOnlyCollection<string> GetSupportedFormats()
    {
        lock (_lock)
        {
            return _loaders.Keys.ToList();
        }
    }
}
