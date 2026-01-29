namespace FantaSim.Geosphere.Plate.Cache.Tests.TestHelpers;

public sealed class InMemoryExternalStorageBackend
{
    private readonly Dictionary<string, byte[]> _storage = new(StringComparer.Ordinal);

    public string Put(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var uri = $"mem://{Guid.NewGuid():N}";
        _storage[uri] = payload.ToArray();
        return uri;
    }

    public Task<byte[]?> GetAsync(string uri, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_storage.TryGetValue(uri, out var payload) ? payload.ToArray() : null);
    }
}
