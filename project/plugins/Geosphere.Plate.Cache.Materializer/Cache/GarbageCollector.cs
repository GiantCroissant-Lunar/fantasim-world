using System.Globalization;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed class GarbageCollector
{
    private const string ManifestSuffix = ":Manifest";
    private const string PayloadSuffix = ":Payload";

    private readonly IArtifactStorage _storage;

    public GarbageCollector(IArtifactStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
    }

    public async Task CollectAsync(string streamPrefix, RetentionPolicy policy, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamPrefix);
        ArgumentNullException.ThrowIfNull(policy);
        ct.ThrowIfCancellationRequested();

        var candidates = new List<ArtifactCandidate>();
        await foreach (var key in _storage.EnumerateKeysAsync(streamPrefix, ct).ConfigureAwait(false))
        {
            if (!key.EndsWith(ManifestSuffix, StringComparison.Ordinal))
                continue;

            var manifest = await _storage.GetManifestAsync(key, ct).ConfigureAwait(false);
            if (!manifest.HasValue)
                continue;

            var payloadKey = string.Concat(key.AsSpan(0, key.Length - ManifestSuffix.Length), PayloadSuffix);
            var createdAt = ParseCreatedAt(manifest.Value.CreatedAtUtc);

            candidates.Add(new ArtifactCandidate(
                ManifestKey: key,
                PayloadKey: payloadKey,
                LastSequence: (long)manifest.Value.Boundary.LastSequence,
                CreatedAtUtc: createdAt));
        }

        if (candidates.Count == 0)
            return;

        var protectedKeys = GetProtectedKeys(candidates, policy.MinArtifactsToKeep);
        var maxSequence = candidates.Max(c => c.LastSequence);
        var now = DateTimeOffset.UtcNow;

        foreach (var candidate in candidates)
        {
            if (protectedKeys.Contains(candidate.ManifestKey))
                continue;

            if (ShouldDelete(candidate, policy, maxSequence, now))
            {
                await _storage.DeleteAsync(candidate.ManifestKey, candidate.PayloadKey, ct).ConfigureAwait(false);
            }
        }
    }

    private static bool ShouldDelete(
        ArtifactCandidate candidate,
        RetentionPolicy policy,
        long maxSequence,
        DateTimeOffset now)
    {
        var deleteBySequence = false;
        if (policy.MaxSequenceAge.HasValue)
        {
            deleteBySequence = (maxSequence - candidate.LastSequence) >= policy.MaxSequenceAge.Value;
        }

        var deleteByTime = false;
        if (policy.MaxTimeAge.HasValue && candidate.CreatedAtUtc.HasValue)
        {
            deleteByTime = candidate.CreatedAtUtc.Value <= now.Subtract(policy.MaxTimeAge.Value);
        }

        return deleteBySequence || deleteByTime;
    }

    private static HashSet<string> GetProtectedKeys(
        List<ArtifactCandidate> candidates,
        int? minArtifactsToKeep)
    {
        var protectedKeys = new HashSet<string>(StringComparer.Ordinal);
        if (!minArtifactsToKeep.HasValue || minArtifactsToKeep.Value <= 0)
            return protectedKeys;

        var ordered = candidates
            .OrderByDescending(c => c.LastSequence)
            .ThenByDescending(c => c.CreatedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(c => c.ManifestKey, StringComparer.Ordinal)
            .Take(minArtifactsToKeep.Value);

        foreach (var candidate in ordered)
        {
            protectedKeys.Add(candidate.ManifestKey);
        }

        return protectedKeys;
    }

    private static DateTimeOffset? ParseCreatedAt(string? createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(createdAtUtc))
            return null;

        if (DateTimeOffset.TryParse(
                createdAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private sealed record ArtifactCandidate(
        string ManifestKey,
        string PayloadKey,
        long LastSequence,
        DateTimeOffset? CreatedAtUtc);
}
