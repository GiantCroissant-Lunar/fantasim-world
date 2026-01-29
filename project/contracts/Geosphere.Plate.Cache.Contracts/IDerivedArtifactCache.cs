using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IDerivedArtifactCache
{
    Task<CacheLookupResult> GetOrCreateArtifactAsync<T>(
        TruthStreamIdentity stream,
        string productType,
        long lastSequence,
        string generatorId,
        string generatorVersion,
        Dictionary<string, object> parameters,
        IArtifactGenerator<T> generator,
        CancellationToken ct);
}
