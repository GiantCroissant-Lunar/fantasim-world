namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Exception thrown when attempting to get the type ID for an unregistered event type.
/// </summary>
public sealed class UnregisteredEventTypeException : InvalidOperationException
{
    /// <summary>
    /// Gets the unregistered event type that caused this exception.
    /// </summary>
    public Type UnregisteredType { get; }

    public UnregisteredEventTypeException(Type unregisteredType)
        : base($"Event type is not registered in EventTypeRegistry: {unregisteredType.FullName}")
    {
        UnregisteredType = unregisteredType;
    }
}
