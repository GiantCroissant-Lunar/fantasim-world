using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Policy for handling tick monotonicity violations during event append.
///
/// In an event-sourced system, the Sequence number is the primary ordering key.
/// However, the Tick (simulation time) may sometimes decrease due to:
/// - Undo/redo operations
/// - Parallel processing with late arrivals
/// - Clock corrections in simulation
///
/// This policy controls how the event store handles cases where
/// Tick decreases while Sequence increases.
/// </summary>
public enum TickMonotonicityPolicy
{
    /// <summary>
    /// Allow tick to decrease without any action.
    /// This is the default policy for backward compatibility.
    /// The event store orders by Sequence, not Tick, so this is safe.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Warn when tick decreases but still allow the append.
    /// Implementations should log a warning for monitoring.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Reject the append if tick decreases.
    /// Throws an InvalidOperationException.
    /// Use this for strict simulation timelines that should never go backward.
    /// </summary>
    Reject = 2
}

/// <summary>
/// Options for appending events to a topology event store.
/// </summary>
public sealed class AppendOptions
{
    /// <summary>
    /// Default options with backward-compatible behavior.
    /// </summary>
    public static readonly AppendOptions Default = new();

    /// <summary>
    /// Policy for handling tick monotonicity violations.
    /// Default is <see cref="TickMonotonicityPolicy.Allow"/>.
    /// </summary>
    public TickMonotonicityPolicy TickPolicy { get; init; } = TickMonotonicityPolicy.Allow;

    /// <summary>
    /// Optional expected head precondition for optimistic concurrency control.
    ///
    /// When set, the append will fail with <see cref="ConcurrencyConflictException"/>
    /// if the actual stream head doesn't match this precondition.
    ///
    /// Use <see cref="HeadPrecondition.Empty"/> for appending to a new/empty stream.
    /// Use <see cref="StreamHead.ToPrecondition"/> from <see cref="ITopologyEventStore.GetHeadAsync"/>
    /// for appending to an existing stream.
    ///
    /// When null (default), no concurrency check is performed. This is backward-compatible
    /// but not recommended for production use with concurrent writers.
    /// </summary>
    public HeadPrecondition? ExpectedHead { get; init; }
}

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
    ///
    /// Note: Tick monotonicity is NOT enforced by default. Ticks may decrease while
    /// sequences increase. Use the overload with AppendOptions to control tick policy.
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
    /// Appends a batch of events to the specified stream with custom options.
    ///
    /// All events must have the same StreamIdentity matching the stream parameter.
    /// Events must have monotonically increasing Sequence numbers for the stream.
    /// The operation should be atomic: either all events succeed or none are persisted.
    /// </summary>
    /// <param name="stream">
    /// The truth stream identity where events will be appended.
    /// </param>
    /// <param name="events">
    /// The events to append.
    /// </param>
    /// <param name="options">
    /// Options controlling append behavior, including tick monotonicity policy.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        AppendOptions options,
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

    /// <summary>
    /// Gets the current head state (sequence, hash, tick) of a stream per RFC-V2-0004.
    ///
    /// Use this method to obtain the precondition for optimistic concurrency control.
    /// The returned <see cref="StreamHead"/> can be converted to a <see cref="HeadPrecondition"/>
    /// via <see cref="StreamHead.ToPrecondition"/> and passed to <see cref="AppendOptions.ExpectedHead"/>.
    ///
    /// Design rationale (RFC-V2-0004/0005 review):
    /// - Returns sequence, hash, AND tick for full head metadata
    /// - Hash comparison catches corruption scenarios
    /// - Per-stream locking in implementation ensures read-modify-write atomicity in-process
    /// </summary>
    /// <param name="stream">
    /// The truth stream identity to query.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    /// <returns>
    /// The current head state, or <see cref="StreamHead.Empty"/> if stream doesn't exist.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stream head metadata is corrupted.
    /// </exception>
    Task<StreamHead> GetHeadAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken
    );
}
