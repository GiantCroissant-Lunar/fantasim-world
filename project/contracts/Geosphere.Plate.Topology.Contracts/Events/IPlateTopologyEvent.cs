using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Base interface for all plate topology events per FR-006, FR-015.
///
/// Events represent immutable changes to plate topology truth. Each event contains
/// all information required to reconstruct topology state without external
/// dependencies or solver execution.
///
/// Event envelope structure:
/// - EventId: Unique identifier for the event (UUIDv7 for sortability)
/// - EventType: Discriminator for polymorphic deserialization
/// - Tick: Canonical simulation time (RFC-V2-0010)
/// - Sequence: Ordering within stream (deterministic replay)
/// - StreamIdentity: Which truth stream this belongs to
///
/// Concrete event types (per FR-008):
/// - Creation events: PlateCreated, BoundaryCreated, JunctionCreated
/// - Lifecycle events: BoundaryRetired, JunctionRetired, PlateRetired (optional)
/// - State change events: BoundaryTypeChanged, BoundaryGeometryUpdated, JunctionUpdated
/// - Topology evolution (future): PlateSplit, PlateMerge, BoundaryReSegmented
/// </summary>
public interface IPlateTopologyEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    ///
    /// Uses UUIDv7 for time-sorted uniqueness, supporting efficient indexing and
    /// debugging of event streams. The event ID must be stable and unique across
    /// all events in all streams.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    ///
    /// This string identifies the concrete event type and is used by serializers
    /// to reconstruct the correct event implementation from stored bytes.
    /// Event types should be stable and well-documented for compatibility.
    ///
    /// Examples: "PlateCreated", "BoundaryCreated", "JunctionCreated",
    /// "BoundaryTypeChanged", "BoundaryGeometryUpdated", "BoundaryRetired",
    /// "JunctionUpdated", "JunctionRetired", "PlateRetired"
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the canonical simulation tick when this event occurred.
    ///
    /// Per RFC-V2-0010, all events are indexed by CanonicalTick. This is the
    /// absolute, monotonic, unitless time coordinate for the simulation.
    /// Tick 0 is world genesis; negative ticks are invalid.
    ///
    /// For deterministic replay, the tick must be preserved exactly as
    /// emitted by the solver.
    /// </summary>
    CanonicalTick Tick { get; }

    /// <summary>
    /// Gets the sequence number of this event within its stream.
    ///
    /// Events within a stream are ordered by this sequence number for deterministic
    /// replay per FR-001 and FR-012. The sequence must be monotonically increasing
    /// within each stream identity, starting from zero or a configured offset.
    ///
    /// Sequence ordering is critical for:
    /// - Replay determinism (SC-001)
    /// - Conflict detection and resolution
    /// - Stream versioning and branching
    /// </summary>
    long Sequence { get; }

    /// <summary>
    /// Gets the truth stream identity this event belongs to.
    ///
    /// Per FR-001 and FR-014, events are isolated by the canonical identity tuple
    /// (VariantId, BranchId, L, Domain, M). Events from different stream identities
    /// are independent and must not interfere with each other.
    ///
    /// This enables:
    /// - Multiple world variants (e.g., "science", "wuxing")
    /// - Branches for parallel exploration
    /// - Governance at different L-levels
    /// - Multiple models/governing equations (M0, M1, ...)
    /// </summary>
    TruthStreamIdentity StreamIdentity { get; }

    /// <summary>
    /// Gets the hash of the previous event in the chain.
    ///
    /// This field establishes a cryptographic chain across events in a stream.
    /// For the genesis event (first event in a stream), this is empty.
    /// For all subsequent events, this must equal the Hash of the preceding event.
    ///
    /// This enables:
    /// - Tamper detection (any modification breaks the chain)
    /// - Deterministic verification of event ordering
    /// - Efficient integrity checks without replaying full history
    /// </summary>
    ReadOnlyMemory<byte> PreviousHash { get; }

    /// <summary>
    /// Gets the cryptographic hash of this event.
    ///
    /// The hash is computed over a canonical preimage that includes:
    /// - Tick (canonical simulation time)
    /// - StreamIdentity (truth stream tuple)
    /// - PreviousHash (chain link to prior event)
    /// - Payload bytes (event-specific data, MessagePack-encoded)
    ///
    /// The Hash field itself is NOT included in the preimage (would be circular).
    /// Algorithm: SHA-256 (producing 32 bytes).
    ///
    /// This field is computed, never set by callers directly.
    /// </summary>
    ReadOnlyMemory<byte> Hash { get; }
}
