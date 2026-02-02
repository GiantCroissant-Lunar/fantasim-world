namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IInvalidationNotifier
{
    IDisposable Subscribe(Action<InvalidationEvent> handler);
}
