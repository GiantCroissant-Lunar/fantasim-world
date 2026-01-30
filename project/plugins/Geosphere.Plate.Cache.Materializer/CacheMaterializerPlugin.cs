using System;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using Microsoft.Extensions.DependencyInjection;
using PluginArchi.Extensibility.Abstractions;
using ServiceArchi.Contracts;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Cache.Materializer;

/// <summary>
/// Plugin entry point for the Derived Artifact Cache Materializer.
///
/// Registers:
/// - IDerivedArtifactCache: Cache lookup and storage for derived artifacts
/// - IArtifactStorage: Low-level artifact storage abstraction
/// </summary>
[Plugin("fantasim.geosphere.plate.cache.materializer", Name = "Derived Artifact Cache", Tags = "geosphere,cache,materializer,derived")]
public sealed class CacheMaterializerPlugin : ILifecyclePlugin
{
    private IRegistry? _registry;
    private ArtifactCache? _cache;
    private IArtifactStorage? _storage;

    public IPluginDescriptor Descriptor { get; } = new PluginDescriptor
    {
        Id = "fantasim.geosphere.plate.cache.materializer",
        Name = "Derived Artifact Cache",
        Description = "Caching layer for derived artifacts with content-addressed fingerprinting.",
        Tags = new[] { "geosphere", "cache", "materializer", "derived" },
        Version = new Version(1, 0, 0)
    };

    public ValueTask InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        // 1. Resolve the ServiceArchi Registry from the host context
        var registry = context.Services.GetService<IRegistry>();
        if (registry == null)
        {
            return ValueTask.CompletedTask;
        }

        _registry = registry;

        // 2. Resolve options or use defaults (Embedded mode with verification)
        var options = context.Services.GetService<ArtifactCacheOptions>()
            ?? new ArtifactCacheOptions(StorageMode.Embedded);

        // 3. Resolve or create storage backend
        _storage = context.Services.GetService<IArtifactStorage>();
        if (_storage == null)
        {
            // Try to create embedded storage using KV store
            var kvStore = context.Services.GetService<IKeyValueStore>();
            if (kvStore != null)
            {
                _storage = new EmbeddedStorage(kvStore);
            }
            else
            {
                // No storage backend available
                return ValueTask.CompletedTask;
            }
        }

        // 4. Create and register the cache
        _cache = new ArtifactCache(_storage, options);
        registry.Register<IDerivedArtifactCache>(_cache);
        registry.Register<IArtifactStorage>(_storage);

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken ct = default)
    {
        _registry?.UnregisterAll<IArtifactStorage>();
        _registry?.UnregisterAll<IDerivedArtifactCache>();

        _cache = null;
        _storage = null;
        _registry = null;

        return ValueTask.CompletedTask;
    }
}
