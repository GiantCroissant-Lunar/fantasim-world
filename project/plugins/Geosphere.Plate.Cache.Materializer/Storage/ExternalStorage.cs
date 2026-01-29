using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Storage;

/// <summary>
/// External artifact storage with manifest stored in KV store and payload fetched externally.
/// </summary>
public sealed class ExternalStorage : IArtifactStorage
{
    private readonly IKeyValueStore _store;
    private readonly object _lock = new();

    public ExternalStorage(IKeyValueStore store)
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

        var manifestKey = KeyBuilder.DeriveManifestKeyFromPayloadKey(key);
        var manifest = await GetManifestAsync(manifestKey, ct).ConfigureAwait(false);
        if (manifest == null)
            return null;

        var storage = manifest.Value.Storage;
        if (storage.Mode != StorageMode.External)
            throw new InvalidOperationException("Manifest storage mode is not External");

        var external = manifest.Value.External;
        if (external == null)
            throw new InvalidOperationException("External storage info is missing");

        var payload = await FetchExternalPayloadAsync(external.Value, ct).ConfigureAwait(false);
        if (payload == null || payload.Length == 0)
            return null;

        VerifyPayload(storage, payload);
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

        if (manifest.External == null)
            throw new ArgumentException("External storage info is required for external storage", nameof(manifest));

        var contentHash = ComputeSha256Hex(payload);
        var contentLength = (ulong)payload.Length;

        var storageInfo = StorageInfo.External(contentHash, contentLength);
        var updatedManifest = manifest with { Storage = storageInfo };
        updatedManifest.Validate();

        var manifestBytes = ManifestSerializer.Serialize(updatedManifest);
        var manifestKeyBytes = Encoding.UTF8.GetBytes(manifestKey);

        using var batch = _store.CreateWriteBatch();
        batch.Put(manifestKeyBytes, manifestBytes);

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

    private static Task<byte[]?> FetchExternalPayloadAsync(ExternalStorageInfo external, CancellationToken ct)
    {
        // Placeholder for external fetch implementation (S3/Azure Blob/filesystem).
        // Implementers should use external.Uri and optional ETag/Backend to retrieve bytes.
        ct.ThrowIfCancellationRequested();
        throw new NotImplementedException($"External fetch not implemented for {external.Uri}");
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
