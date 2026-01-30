using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Exception thrown when an optimistic concurrency check fails during event append.
///
/// This indicates that the stream's head state changed between reading the head
/// and attempting to append. The caller should:
/// 1. Re-read the current head
/// 2. Recompute events with updated sequences
/// 3. Retry the append with the new precondition
///
/// Design rationale (RFC-V2-0005 review):
/// - Explicit exception type enables targeted catch/retry logic
/// - Includes both expected and actual state for diagnostics
/// - Follows optimistic concurrency patterns from event sourcing literature
/// </summary>
public sealed class ConcurrencyConflictException : InvalidOperationException
{
    /// <summary>
    /// The stream where the conflict occurred.
    /// </summary>
    public TruthStreamIdentity Stream { get; }

    /// <summary>
    /// The expected head state that was passed to AppendAsync.
    /// </summary>
    public HeadPrecondition Expected { get; }

    /// <summary>
    /// The actual head state found in the store.
    /// </summary>
    public StreamHead Actual { get; }

    /// <summary>
    /// Creates a new concurrency conflict exception.
    /// </summary>
    public ConcurrencyConflictException(
        TruthStreamIdentity stream,
        HeadPrecondition expected,
        StreamHead actual)
        : base(FormatMessage(stream, expected, actual))
    {
        Stream = stream;
        Expected = expected;
        Actual = actual;
    }

    private static string FormatMessage(
        TruthStreamIdentity stream,
        HeadPrecondition expected,
        StreamHead actual)
    {
        return $"Concurrency conflict on stream {stream}: " +
               $"expected head (seq={expected.Sequence}, hash={FormatHash(expected.Hash)}), " +
               $"actual head (seq={actual.Sequence}, hash={FormatHash(actual.Hash)})";
    }

    private static string FormatHash(byte[]? hash)
    {
        if (hash == null || hash.Length == 0)
            return "<null>";
        if (hash.Length < 4)
            return Convert.ToHexString(hash).ToLowerInvariant();
        return Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant() + "...";
    }
}
