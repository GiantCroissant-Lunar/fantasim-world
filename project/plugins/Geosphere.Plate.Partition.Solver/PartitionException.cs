using System.Runtime.Serialization;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Exception thrown when a plate partition operation fails.
/// Provides detailed diagnostic information about the failure cause.
/// RFC-V2-0047 §9.
/// </summary>
[Serializable]
public sealed class PartitionException : Exception
{
    /// <summary>
    /// Gets the type of partition failure that occurred.
    /// </summary>
    public PartitionFailureType FailureType { get; }

    /// <summary>
    /// Gets additional diagnostic details about the failure.
    /// </summary>
    public IReadOnlyDictionary<string, string> Diagnostics { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionException"/> class.
    /// </summary>
    public PartitionException()
        : this(PartitionFailureType.Unknown, "An unknown partition error occurred.", null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionException"/> class with a message.
    /// </summary>
    public PartitionException(string message)
        : this(PartitionFailureType.Unknown, message, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionException"/> class with a message and inner exception.
    /// </summary>
    public PartitionException(string message, Exception innerException)
        : this(PartitionFailureType.Unknown, message, innerException, null)
    {
    }

    /// <summary>
    /// Initializes a new instance with a specific failure type and message.
    /// </summary>
    public PartitionException(PartitionFailureType failureType, string message)
        : this(failureType, message, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance with full diagnostic information.
    /// </summary>
    public PartitionException(
        PartitionFailureType failureType,
        string message,
        Exception? innerException,
        IReadOnlyDictionary<string, string>? diagnostics)
        : base(message, innerException)
    {
        FailureType = failureType;
        Diagnostics = diagnostics ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Serialization constructor for ISerializable support.
    /// </summary>
    private PartitionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        FailureType = (PartitionFailureType)(info.GetInt32(nameof(FailureType)));
        Diagnostics = info.GetValue(nameof(Diagnostics), typeof(Dictionary<string, string>)) as IReadOnlyDictionary<string, string>
            ?? new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(info);
        info.AddValue(nameof(FailureType), (int)FailureType);
        info.AddValue(nameof(Diagnostics), new Dictionary<string, string>(Diagnostics));
        base.GetObjectData(info, context);
    }
}

/// <summary>
/// Specifies the type of failure that occurred during partition operations.
/// </summary>
public enum PartitionFailureType
{
    /// <summary>Failure type is unknown or unspecified.</summary>
    Unknown,

    /// <summary>Topology is invalid (e.g., open boundaries).</summary>
    InvalidTopology,

    /// <summary>Non-manifold junctions detected.</summary>
    NonManifoldJunction,

    /// <summary>Polygonization algorithm failed.</summary>
    PolygonizationFailed,

    /// <summary>Materialization of topology state failed.</summary>
    MaterializationFailed,

    /// <summary>Validation of partition result failed.</summary>
    ValidationFailed,

    /// <summary>Cache operation failed.</summary>
    CacheError
}
