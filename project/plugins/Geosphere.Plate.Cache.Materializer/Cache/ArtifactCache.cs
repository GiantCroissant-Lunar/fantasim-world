using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed class ArtifactCache : IDerivedArtifactCache
{
    private readonly IArtifactStorage _storage;
    private readonly ArtifactCacheOptions _options;

    public ArtifactCache(IArtifactStorage storage, ArtifactCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        _storage = storage;
        _options = options;
    }

    public async Task<CacheLookupResult> GetOrCreateArtifactAsync<T>(
        TruthStreamIdentity stream,
        string productType,
        long lastSequence,
        string generatorId,
        string generatorVersion,
        Dictionary<string, object> parameters,
        IArtifactGenerator<T> generator,
        CancellationToken ct)
    {
        if (lastSequence < 0)
            throw new ArgumentException("LastSequence cannot be negative", nameof(lastSequence));

        ArgumentException.ThrowIfNullOrWhiteSpace(productType);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(generator);
        ct.ThrowIfCancellationRequested();

        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            sourceStream: stream.ToEventStreamIdString(),
            boundaryKind: "sequence",
            lastSequence: (ulong)lastSequence,
            generatorId: generatorId,
            generatorVersion: generatorVersion,
            paramsHash: paramsHash);

        var manifestKey = KeyBuilder.BuildManifestKey(stream, productType, inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, productType, inputFingerprint);

        var manifest = await _storage.GetManifestAsync(manifestKey, ct).ConfigureAwait(false);
        if (manifest.HasValue)
        {
            if (!string.Equals(manifest.Value.InputFingerprint, inputFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Manifest input_fingerprint does not match computed fingerprint");

            var payload = await _storage.GetPayloadAsync(payloadKey, ct).ConfigureAwait(false);
            if (payload == null)
                throw new InvalidOperationException("Payload missing for cached manifest");

            if (_options.VerifyOnRead)
            {
                await VerifyContentAsync(payload, manifest.Value.Storage, ct).ConfigureAwait(false);
            }

            return CacheLookupResult.Hit(payload, manifest.Value, inputFingerprint);
        }

        return CacheLookupResult.Miss(inputFingerprint);
    }

    public async Task StoreArtifactAsync(
        TruthStreamIdentity stream,
        string productType,
        long lastSequence,
        string inputFingerprint,
        string generatorId,
        string generatorVersion,
        Dictionary<string, object> parameters,
        byte[] payload,
        CancellationToken ct)
    {
        if (lastSequence < 0)
            throw new ArgumentException("LastSequence cannot be negative", nameof(lastSequence));

        ArgumentException.ThrowIfNullOrWhiteSpace(productType);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(payload);
        ct.ThrowIfCancellationRequested();

        var contentHash = ComputeSha256Hex(payload);
        var contentLength = (ulong)payload.Length;
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var boundary = Boundary.Sequence((ulong)lastSequence);
        var generator = new GeneratorInfo(generatorId, generatorVersion);
        var storageInfo = _options.Mode == StorageMode.External
            ? StorageInfo.External(contentHash, contentLength)
            : StorageInfo.Embedded(contentHash, contentLength);

        var manifest = Manifest.Create(
            productType: productType,
            inputFingerprint: inputFingerprint,
            sourceStream: stream.ToEventStreamIdString(),
            boundary: boundary,
            generator: generator,
            paramsHash: paramsHash,
            storage: storageInfo) with
        {
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
            Params = parameters
        };

        var manifestKey = KeyBuilder.BuildManifestKey(stream, productType, inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, productType, inputFingerprint);

        await _storage.StoreAsync(manifestKey, manifest, payloadKey, payload, ct).ConfigureAwait(false);
    }

    public static Task VerifyContentAsync(byte[] payload, StorageInfo storage, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ct.ThrowIfCancellationRequested();

        var hash = ComputeSha256Hex(payload);
        if (!hash.Equals(storage.ContentHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Payload content hash mismatch");

        if ((ulong)payload.Length != storage.ContentLength)
            throw new InvalidOperationException("Payload content length mismatch");

        return Task.CompletedTask;
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
}
