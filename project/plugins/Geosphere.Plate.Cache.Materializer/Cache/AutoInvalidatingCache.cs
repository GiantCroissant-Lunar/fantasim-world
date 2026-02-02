using FantaSim.Geosphere.Plate.Cache.Contracts;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed class AutoInvalidatingCache : IDerivedProductCache, IDisposable
{
    private readonly IDerivedProductCache _inner;
    private readonly IDisposable? _subscription;
    private bool _disposed;

    public AutoInvalidatingCache(IDerivedProductCache inner, IInvalidationNotifier? notifier)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;

        _subscription = notifier?.Subscribe(OnInvalidation);
    }

    public ICacheMetrics Metrics => _inner.Metrics;

    public Task<DerivedProductLookupResult<T>> GetOrComputeAsync<T>(
        DerivedProductKey key,
        IDerivedProductGenerator<T> generator,
        CancellationToken ct) =>
        _inner.GetOrComputeAsync(key, generator, ct);

    public void InvalidateOnTopologyChange(string topologyStreamHash) =>
        _inner.InvalidateOnTopologyChange(topologyStreamHash);

    public void InvalidateOnKinematicsChange(string modelId) =>
        _inner.InvalidateOnKinematicsChange(modelId);

    public void Invalidate(string productInstanceId) =>
        _inner.Invalidate(productInstanceId);

    public void Clear() =>
        _inner.Clear();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _subscription?.Dispose();
    }

    private void OnInvalidation(InvalidationEvent evt)
    {
        switch (evt.Reason)
        {
            case InvalidationReason.TopologyChanged:
                if (!string.IsNullOrWhiteSpace(evt.TopologyStreamHash))
                    _inner.InvalidateOnTopologyChange(evt.TopologyStreamHash);
                break;

            case InvalidationReason.KinematicsChanged:
                if (!string.IsNullOrWhiteSpace(evt.KinematicsModelId))
                    _inner.InvalidateOnKinematicsChange(evt.KinematicsModelId);
                break;

            case InvalidationReason.Manual:
            default:
                break;
        }
    }
}
