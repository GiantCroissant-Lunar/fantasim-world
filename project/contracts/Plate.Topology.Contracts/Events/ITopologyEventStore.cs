using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

/// <summary>
/// Contract interface for topology event store per FR-001, FR-012, FR-014.
///
/// Defines operations for appending and reading topology events from a persistent
/// event store. The contract ensures deterministic replay by ordering events by
/// Sequence within isolated streams identified by TruthStreamIdentity.
///
/// Implementations must ensure:
/// - Events are appended atomically per stream
/// - Events are read in Sequence order within a stream
/// - Streams are fully isolated by TruthStreamIdentity
/// - No cross-stream dependencies in read/write operations
/// </summary>
public interface ITopologyEventStore
{
    /// <summary>
    /// Appends a batch of events to the specified stream.
    ///
    /// All events must have the same StreamIdentity matching the stream parameter.
    /// Events must have monotonically increasing Sequence numbers for the stream.
    /// The operation should be atomic: either all events succeed or none are persisted.
    ///
    /// Sequence numbers and StreamIdentity are used for deterministic replay,
    /// ensuring events are replayed in the exact order they were appended per SC-001.
    /// </summary>
    /// <param name="stream">
    /// The truth stream identity where events will be appended.
    /// Stream isolation is enforced: events from different streams are independent.
    /// </param>
    /// <param name="events">
    /// The events to append. Each event must have a matching StreamIdentity
    /// and a unique Sequence within the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    /// <returns>
    /// Task representing the asynchronous append operation.
    /// </returns>
    Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads events from a stream starting from a specific Sequence number.
    ///
    /// Events are returned in ascending Sequence order, which is required for
    /// deterministic replay per FR-012 and SC-001. The stream identity isolates
    /// reads to only events belonging to that stream.
    /// </summary>
    /// <param name="stream">
    /// The truth stream identity to read from.
    /// Stream isolation is enforced: only events from this stream are returned.
    /// </param>
    /// <param name="fromSequenceInclusive">
    /// The starting Sequence number (inclusive). Events with Sequence >= this value
    /// are returned. Use 0 to read from the beginning of the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the asynchronous enumeration.
    /// </param>
    /// <returns>
    /// Async enumerable of events in Sequence order.
    /// </returns>
    IAsyncEnumerable<IPlateTopologyEvent> ReadAsync(
        TruthStreamIdentity stream,
        long fromSequenceInclusive,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets the highest Sequence number for a stream.
    ///
    /// Useful for determining the current stream version before appending new events.
    /// The Sequence number is monotonically increasing within each stream identity,
    /// and streams are fully isolated from each other.
    /// </summary>
    /// <param name="stream">
    /// The truth stream identity to query.
    /// Stream isolation is enforced: only sequences from this stream are considered.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    /// <returns>
    /// The highest Sequence number, or null if the stream is empty or does not exist.
    /// </returns>
    Task<long?> GetLastSequenceAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken
    );
}
