using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Storage;

/// <summary>
/// Embedded artifact storage backed by a key-value store.
/// Stores manifest and payload directly in the KV store.
/// </summary>
public sealed class EmbeddedStorage : IArtifactStorage
{
    private readonly IKeyValueStore _store;
    private readonly object _lock = new();

    public EmbeddedStorage(IKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public Task<Manifest?> GetManifestAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        ct.ThrowIfCancellationRequested();

        byte[]? bytes;
        lock (_lock)
        {
            bytes = ReadBytes(Encoding.UTF8.GetBytes(key));
        }

        if (bytes == null || bytes.Length == 0)
            return Task.FromResult<Manifest?>(null);

        var manifest = ManifestSerializer.Deserialize(bytes);
        return Task.FromResult<Manifest?>(manifest);
    }

    public async Task<byte[]?> GetPayloadAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();

        byte[]? payload;
        lock (_lock)
        {
            payload = ReadBytes(Encoding.UTF8.GetBytes(key));
        }

        if (payload == null || payload.Length == 0)
            return null;

        var manifestKey = KeyBuilder.DeriveManifestKeyFromPayloadKey(key);
        var manifest = await GetManifestAsync(manifestKey, ct).ConfigureAwait(false);
        if (manifest == null)
            return null;

        VerifyPayload(manifest.Value.Storage, payload);
        return payload;
    }

    public Task StoreAsync(
        string manifestKey,
        Manifest manifest,
        string payloadKey,
        byte[] payload,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadKey);
        ArgumentNullException.ThrowIfNull(payload);

        ct.ThrowIfCancellationRequested();

        var contentHash = ComputeSha256Hex(payload);
        var contentLength = (ulong)payload.Length;

        var storageInfo = StorageInfo.Embedded(contentHash, contentLength);
        var updatedManifest = manifest with { Storage = storageInfo, External = null };
        updatedManifest.Validate();

        var manifestBytes = ManifestSerializer.Serialize(updatedManifest);
        var manifestKeyBytes = Encoding.UTF8.GetBytes(manifestKey);
        var payloadKeyBytes = Encoding.UTF8.GetBytes(payloadKey);

        using var batch = _store.CreateWriteBatch();
        batch.Put(manifestKeyBytes, manifestBytes);
        batch.Put(payloadKeyBytes, payload);

        lock (_lock)
        {
            _store.Write(batch);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string manifestKey, string payloadKey, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadKey);
        ct.ThrowIfCancellationRequested();

        using var batch = _store.CreateWriteBatch();
        batch.Delete(Encoding.UTF8.GetBytes(manifestKey));
        batch.Delete(Encoding.UTF8.GetBytes(payloadKey));

        lock (_lock)
        {
            _store.Write(batch);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> EnumerateKeysAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ct.ThrowIfCancellationRequested();

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        List<string> keys = new();

        lock (_lock)
        {
            using var iterator = _store.CreateIterator();
            iterator.Seek(prefixBytes);
            while (iterator.Valid && StartsWith(iterator.Key, prefixBytes))
            {
                keys.Add(Encoding.UTF8.GetString(iterator.Key));
                iterator.Next();
            }
        }

        foreach (var key in keys)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return key;
        }
    }

    private static void VerifyPayload(StorageInfo storage, byte[] payload)
    {
        var hash = ComputeSha256Hex(payload);
        if (!hash.Equals(storage.ContentHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Payload content hash mismatch");

        if ((ulong)payload.Length != storage.ContentLength)
            throw new InvalidOperationException("Payload content length mismatch");
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hashBytes = SHA256.HashData(bytes);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }

    private static bool StartsWith(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix)
    {
        if (value.Length < prefix.Length)
            return false;

        return value[..prefix.Length].SequenceEqual(prefix);
    }

    private byte[]? ReadBytes(byte[] key)
    {
        Span<byte> initialBuffer = stackalloc byte[1];
        if (_store.TryGet(key, initialBuffer, out var written))
        {
            return initialBuffer.Slice(0, written).ToArray();
        }

        if (written > 0)
        {
            var result = new byte[written];
            if (_store.TryGet(key, result, out var finalWritten))
            {
                return result;
            }
            throw new InvalidOperationException("Store state changed during read");
        }

        return null;
    }
}
