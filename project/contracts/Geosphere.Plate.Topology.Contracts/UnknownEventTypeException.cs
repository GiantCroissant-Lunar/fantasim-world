namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Exception thrown when an unknown event type ID is encountered during deserialization.
/// </summary>
public sealed class UnknownEventTypeException : InvalidOperationException
{
    /// <summary>
    /// Gets the unknown event type ID that caused this exception.
    /// </summary>
    public string UnknownEventTypeId { get; }

    public UnknownEventTypeException(string unknownEventTypeId)
        : base($"Unknown event type ID: {unknownEventTypeId}")
    {
        UnknownEventTypeId = unknownEventTypeId;
    }
}
