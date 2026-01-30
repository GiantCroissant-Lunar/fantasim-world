namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Represents the current head state of an event stream per RFC-V2-0004.
///
/// Contains sequence, hash, and tick which together uniquely identify
/// the stream state. Used for optimistic concurrency control via
/// <see cref="HeadPrecondition"/>.
///
/// RFC-V2-0004 specifies head metadata as {lastSeq, lastHash, lastTick}.
/// </summary>
public readonly struct StreamHead : IEquatable<StreamHead>
{
    /// <summary>
    /// Hash size in bytes (SHA-256).
    /// </summary>
    public const int HashSizeBytes = 32;

    /// <summary>
    /// Zero hash representing an empty/genesis stream.
    /// </summary>
    public static readonly byte[] ZeroHash = new byte[HashSizeBytes];

    /// <summary>
    /// Head state for an empty (non-existent) stream.
    /// </summary>
    public static StreamHead Empty => new(-1, ZeroHash, -1);

    private readonly byte[] _hash;

    /// <summary>
    /// The highest sequence number in the stream, or -1 if empty.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// The hash of the last event record, or zero hash if empty.
    /// Returns a defensive copy to prevent mutation.
    /// </summary>
    public byte[] Hash => (byte[])_hash.Clone();

    /// <summary>
    /// The tick of the last event, or -1 if empty.
    /// Per RFC-V2-0004, this is stored in head metadata for fast access.
    /// </summary>
    public long LastTick { get; }

    /// <summary>
    /// Creates a new stream head state.
    /// </summary>
    /// <param name="sequence">The highest sequence number, or -1 if empty.</param>
    /// <param name="hash">The hash of the last event record (32 bytes). Will be cloned.</param>
    /// <param name="lastTick">The tick of the last event, or -1 if empty.</param>
    public StreamHead(long sequence, byte[] hash, long lastTick)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (hash.Length != HashSizeBytes)
            throw new ArgumentException($"Hash must be {HashSizeBytes} bytes", nameof(hash));

        Sequence = sequence;
        _hash = (byte[])hash.Clone(); // Defensive copy
        LastTick = lastTick;
    }

    /// <summary>
    /// Creates a new stream head state (backward-compatible overload without tick).
    /// LastTick will be set to -1 (unknown).
    /// </summary>
    [Obsolete("Use the overload with lastTick parameter for RFC-V2-0004 compliance")]
    public StreamHead(long sequence, byte[] hash) : this(sequence, hash, -1)
    {
    }

    /// <summary>
    /// Whether this represents an empty stream.
    /// </summary>
    public bool IsEmpty => Sequence == -1;

    /// <summary>
    /// Converts to a <see cref="HeadPrecondition"/> for use in append operations.
    /// </summary>
    public HeadPrecondition ToPrecondition() => new(Sequence, _hash);

    /// <summary>
    /// Internal access to hash without cloning (for comparison only).
    /// </summary>
    internal ReadOnlySpan<byte> HashSpan => _hash.AsSpan();

    public bool Equals(StreamHead other)
    {
        return Sequence == other.Sequence &&
               LastTick == other.LastTick &&
               _hash.AsSpan().SequenceEqual(other._hash);
    }

    public override bool Equals(object? obj) => obj is StreamHead other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Sequence);
        hashCode.Add(LastTick);
        foreach (var b in _hash)
            hashCode.Add(b);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(StreamHead left, StreamHead right) => left.Equals(right);
    public static bool operator !=(StreamHead left, StreamHead right) => !left.Equals(right);
}
