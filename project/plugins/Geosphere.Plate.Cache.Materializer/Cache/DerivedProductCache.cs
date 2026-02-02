using System.Diagnostics;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed class DerivedProductCache : IDerivedProductCache
{
    private readonly L1MemoryCache _l1Cache = new();
    private readonly ArtifactCache _artifactCache;
    private readonly CacheMetrics _metrics = new();
    private readonly IDerivedProductAuditSink? _auditSink;

    public DerivedProductCache(ArtifactCache artifactCache, IDerivedProductAuditSink? auditSink = null)
    {
        ArgumentNullException.ThrowIfNull(artifactCache);
        _artifactCache = artifactCache;
        _auditSink = auditSink;
    }

    public ICacheMetrics Metrics => _metrics;

    public async Task<DerivedProductLookupResult<T>> GetOrComputeAsync<T>(
        DerivedProductKey key,
        IDerivedProductGenerator<T> generator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ct.ThrowIfCancellationRequested();

        var instanceId = key.ToInstanceId();

        if (_l1Cache.TryGet<T>(instanceId, out var l1Value, out var l1Provenance))
        {
            _metrics.RecordHit();
            return DerivedProductLookupResult<T>.Hit(l1Value!, l1Provenance!.Value, instanceId);
        }

        var parameters = new Dictionary<string, object>
        {
            ["policy_hash"] = key.PolicyHash
        };

        var artifactResult = await _artifactCache.GetOrCreateArtifactAsync(
            stream: key.Stream,
            productType: key.ProductType,
            lastSequence: key.LastSequence,
            generatorId: generator.GeneratorId,
            generatorVersion: generator.GeneratorVersion,
            parameters: parameters,
            generator: new GeneratorAdapter<T>(generator),
            ct: ct).ConfigureAwait(false);

        if (artifactResult.IsHit)
        {
            _metrics.RecordHit();

            var payload = artifactResult.Payload ?? throw new InvalidOperationException("Artifact cache hit returned null payload");
            var value = generator.Deserialize(payload);

            var provenance = ExtractProvenance(
                instanceId: instanceId,
                productType: key.ProductType,
                policyHash: key.PolicyHash,
                manifest: artifactResult.Manifest);

            _l1Cache.Set(instanceId, value, provenance);

            return DerivedProductLookupResult<T>.Hit(value, provenance, instanceId);
        }

        _metrics.RecordMiss();
        var sw = Stopwatch.StartNew();

        var computed = await generator.ComputeAsync(ct).ConfigureAwait(false);

        sw.Stop();
        _metrics.RecordComputeTime(sw.ElapsedMilliseconds);

        var computedProvenance = new DerivedProductProvenance
        {
            ProductInstanceId = instanceId,
            ProductType = key.ProductType,
            SourceTruthHashes = new[] { key.Stream.ToStreamKey() },
            PolicyHash = key.PolicyHash,
            GeneratorId = generator.GeneratorId,
            GeneratorVersion = generator.GeneratorVersion,
            ComputedAt = DateTimeOffset.UtcNow,
            ComputationTimeMs = sw.ElapsedMilliseconds
        };

        _l1Cache.Set(instanceId, computed, computedProvenance);

        var inputFingerprint = artifactResult.InputFingerprint
            ?? throw new InvalidOperationException("Artifact cache miss returned null input fingerprint");

        var payloadBytes = generator.Serialize(computed);

        await _artifactCache.StoreArtifactAsync(
            stream: key.Stream,
            productType: key.ProductType,
            lastSequence: key.LastSequence,
            inputFingerprint: inputFingerprint,
            generatorId: generator.GeneratorId,
            generatorVersion: generator.GeneratorVersion,
            parameters: parameters,
            payload: payloadBytes,
            ct: ct).ConfigureAwait(false);

        _auditSink?.Record(new DerivedProductAuditRecord(
            ProducedAtUtc: DateTimeOffset.UtcNow.ToString("o"),
            Key: key,
            Provenance: computedProvenance));

        return DerivedProductLookupResult<T>.Computed(computed, computedProvenance, instanceId);
    }

    public void InvalidateOnTopologyChange(string topologyStreamHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topologyStreamHash);

        var removed = _l1Cache.Invalidate(k => k.Contains(topologyStreamHash, StringComparison.Ordinal));
        _metrics.RecordInvalidation(removed);
    }

    public void InvalidateOnKinematicsChange(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        var removed = _l1Cache.Invalidate(k => k.Contains(modelId, StringComparison.Ordinal));
        _metrics.RecordInvalidation(removed);
    }

    public void Invalidate(string productInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productInstanceId);

        var removed = _l1Cache.Invalidate(productInstanceId) ? 1 : 0;
        _metrics.RecordInvalidation(removed);
    }

    public void Clear()
    {
        var removed = _l1Cache.Clear();
        _metrics.RecordInvalidation(removed);
    }

    private static DerivedProductProvenance ExtractProvenance(
        string instanceId,
        string productType,
        string policyHash,
        Manifest? manifest)
    {
        DateTimeOffset computedAt;

        var createdAtUtc = manifest?.CreatedAtUtc;
        if (createdAtUtc != null && DateTimeOffset.TryParse(createdAtUtc, out var parsed))
        {
            computedAt = parsed;
        }
        else
        {
            computedAt = DateTimeOffset.UtcNow;
        }

        var sourceTruthHashes = manifest.HasValue
            ? new[] { manifest.Value.SourceStream }
            : Array.Empty<string>();

        var generatorId = manifest.HasValue ? manifest.Value.Generator.Id : "";
        var generatorVersion = manifest.HasValue ? manifest.Value.Generator.Version : "";

        return new DerivedProductProvenance
        {
            ProductInstanceId = instanceId,
            ProductType = productType,
            SourceTruthHashes = sourceTruthHashes,
            PolicyHash = policyHash,
            GeneratorId = generatorId,
            GeneratorVersion = generatorVersion,
            ComputedAt = computedAt,
            ComputationTimeMs = 0
        };
    }

    private sealed class GeneratorAdapter<T> : IArtifactGenerator<T>
    {
        private readonly IDerivedProductGenerator<T> _inner;

        public GeneratorAdapter(IDerivedProductGenerator<T> inner)
        {
            _inner = inner;
        }

        public string GeneratorId => _inner.GeneratorId;

        public string GeneratorVersion => _inner.GeneratorVersion;

        public Task<T> GenerateAsync(ArtifactGenerationContext context, CancellationToken ct) =>
            _inner.ComputeAsync(ct);

        public byte[] Serialize(T artifact) => _inner.Serialize(artifact);

        public T Deserialize(byte[] data) => _inner.Deserialize(data);
    }
}
