using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

/// <summary>
/// High-level contract for derived artifact cache stores.
/// </summary>
public interface IArtifactCacheStore
{
    Task<Manifest?> GetManifestAsync(string key, CancellationToken ct);

    Task<byte[]?> GetPayloadAsync(string key, CancellationToken ct);

    Task StoreAsync(string manifestKey, Manifest manifest, string payloadKey, byte[] payload, CancellationToken ct);

    Task DeleteAsync(string manifestKey, string payloadKey, CancellationToken ct);

    IAsyncEnumerable<string> EnumerateKeysAsync(string prefix, CancellationToken ct);
}
