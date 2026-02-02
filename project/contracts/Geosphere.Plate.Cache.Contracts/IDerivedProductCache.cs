namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IDerivedProductCache
{
    Task<DerivedProductLookupResult<T>> GetOrComputeAsync<T>(
        DerivedProductKey key,
        IDerivedProductGenerator<T> generator,
        CancellationToken ct);

    void InvalidateOnTopologyChange(string topologyStreamHash);

    void InvalidateOnKinematicsChange(string modelId);

    void Invalidate(string productInstanceId);

    void Clear();

    ICacheMetrics Metrics { get; }
}
