using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Storage;

/// <summary>
/// Storage contract for derived artifacts (manifest + payload).
/// </summary>
public interface IArtifactStorage
{
    Task<Manifest?> GetManifestAsync(string key, CancellationToken ct);

    Task<byte[]?> GetPayloadAsync(string key, CancellationToken ct);

    Task StoreAsync(
        string manifestKey,
        Manifest manifest,
        string payloadKey,
        byte[] payload,
        CancellationToken ct);

    Task DeleteAsync(string manifestKey, string payloadKey, CancellationToken ct);

    IAsyncEnumerable<string> EnumerateKeysAsync(string prefix, CancellationToken ct);
}
