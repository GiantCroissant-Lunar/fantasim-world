namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Represents an expected head state for optimistic concurrency control.
///
/// Used by <see cref="ITopologyEventStore.AppendAsync"/> to guard against
/// concurrent writers. If the actual head doesn't match, a
/// <see cref="ConcurrencyConflictException"/> is thrown.
///
/// Design rationale (RFC-V2-0005 review):
/// - RocksDB doesn't provide atomic compare-and-swap in basic API
/// - Per-stream locking + precondition check provides in-process safety
/// - Clear upgrade path to TransactionDB for multi-process writers
/// </summary>
public readonly struct HeadPrecondition : IEquatable<HeadPrecondition>
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
    /// Precondition for appending to an empty (non-existent) stream.
    /// </summary>
    public static HeadPrecondition Empty => new(-1, ZeroHash);

    private readonly byte[] _hash;

    /// <summary>
    /// Expected head sequence number. Use -1 for empty streams.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Expected head hash (32 bytes SHA-256).
    /// Returns a defensive copy to prevent mutation.
    /// </summary>
    public byte[] Hash => (byte[])_hash.Clone();

    /// <summary>
    /// Creates a new head precondition.
    /// </summary>
    /// <param name="sequence">Expected sequence number, or -1 for empty stream.</param>
    /// <param name="hash">Expected hash (32 bytes). Will be cloned.</param>
    public HeadPrecondition(long sequence, byte[] hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (hash.Length != HashSizeBytes)
            throw new ArgumentException($"Hash must be {HashSizeBytes} bytes", nameof(hash));

        Sequence = sequence;
        _hash = (byte[])hash.Clone(); // Defensive copy
    }

    /// <summary>
    /// Validates the precondition structure.
    /// </summary>
    public bool IsValid => Sequence >= -1 && _hash is { Length: HashSizeBytes };

    /// <summary>
    /// Internal access to hash without cloning (for comparison only).
    /// </summary>
    internal ReadOnlySpan<byte> HashSpan => _hash.AsSpan();

    public bool Equals(HeadPrecondition other)
    {
        return Sequence == other.Sequence && _hash.AsSpan().SequenceEqual(other._hash);
    }

    public override bool Equals(object? obj) => obj is HeadPrecondition other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Sequence);
        foreach (var b in _hash)
            hashCode.Add(b);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(HeadPrecondition left, HeadPrecondition right) => left.Equals(right);
    public static bool operator !=(HeadPrecondition left, HeadPrecondition right) => !left.Equals(right);
}
