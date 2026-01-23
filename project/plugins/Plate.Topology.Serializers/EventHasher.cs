using System;
using System.Buffers;
using System.Security.Cryptography;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Serializers;

/// <summary>
/// Interface for computing cryptographic hashes of events.
/// Used to establish a hash-chain across events in a stream.
/// </summary>
public interface IEventHasher
{
    /// <summary>
    /// Computes the hash of a canonical preimage byte array.
    /// </summary>
    /// <param name="preimageBytes">The canonical bytes to hash.</param>
    /// <returns>The hash as a byte array (32 bytes for SHA-256).</returns>
    byte[] Hash(ReadOnlySpan<byte> preimageBytes);

    /// <summary>
    /// Computes the hash of an event given its components.
    /// The hash preimage includes: Tick, StreamIdentity, PreviousHash, and payload bytes.
    /// The Hash field itself is NOT included (would be circular).
    /// </summary>
    /// <param name="tick">The canonical simulation tick.</param>
    /// <param name="streamIdentity">The truth stream identity.</param>
    /// <param name="previousHash">The hash of the previous event (empty for genesis).</param>
    /// <param name="payloadBytes">The MessagePack-encoded payload bytes.</param>
    /// <returns>The computed hash (32 bytes).</returns>
    byte[] ComputeEventHash(
        CanonicalTick tick,
        TruthStreamIdentity streamIdentity,
        ReadOnlyMemory<byte> previousHash,
        ReadOnlySpan<byte> payloadBytes);
}

/// <summary>
/// Interface for serializing events to canonical bytes.
/// Separates concerns: ICanonicalEventSerializer handles encoding, IEventHasher handles hashing.
/// </summary>
public interface ICanonicalEventSerializer
{
    /// <summary>
    /// Serializes an event to canonical bytes for persistence.
    /// Includes ALL fields: Tick, Sequence, StreamIdentity, PreviousHash, Hash, and payload.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <returns>Canonical MessagePack bytes.</returns>
    byte[] SerializeCanonical(IPlateTopologyEvent @event);

    /// <summary>
    /// Serializes an event to canonical bytes for hashing.
    /// EXCLUDES the Hash field (to avoid circular dependency).
    /// Includes: Tick, StreamIdentity, PreviousHash, and event-specific payload.
    ///
    /// Hash preimage structure (MessagePack array):
    /// [0] Tick (Int64)
    /// [1] StreamIdentity (array of 5 elements)
    /// [2] PreviousHash (binary, empty for genesis)
    /// [3] PayloadBytes (binary, event-specific fields)
    /// </summary>
    /// <param name="event">The event to serialize for hashing.</param>
    /// <returns>Canonical preimage bytes (Hash field NOT included).</returns>
    byte[] SerializeCanonicalForHash(IPlateTopologyEvent @event);
}

/// <summary>
/// Default implementation of ICanonicalEventSerializer.
/// Uses MessagePack for canonical encoding.
/// </summary>
public sealed class CanonicalEventSerializer : ICanonicalEventSerializer
{
    /// <summary>
    /// Shared instance for convenience.
    /// </summary>
    public static readonly CanonicalEventSerializer Instance = new();

    /// <inheritdoc/>
    public byte[] SerializeCanonical(IPlateTopologyEvent @event)
    {
        // Use MessagePack to serialize the full event
        // This delegates to the event's own formatter which includes all fields
        return MessagePackSerializer.Serialize(@event);
    }

    /// <inheritdoc/>
    public byte[] SerializeCanonicalForHash(IPlateTopologyEvent @event)
    {
        // Build the preimage: [Tick, StreamIdentity, PreviousHash, PayloadBytes]
        // NOTE: Hash field is intentionally EXCLUDED to avoid circular dependency
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        // Write array header for 4 elements
        writer.WriteArrayHeader(4);

        // 1. Tick (as raw Int64)
        writer.Write(@event.Tick.Value);

        // 2. StreamIdentity (as array of 5 elements)
        writer.WriteArrayHeader(5);
        writer.Write(@event.StreamIdentity.VariantId);
        writer.Write(@event.StreamIdentity.BranchId);
        writer.Write(@event.StreamIdentity.LLevel);
        writer.Write(@event.StreamIdentity.Domain.Value);
        writer.Write(@event.StreamIdentity.Model);

        // 3. PreviousHash (as binary)
        if (@event.PreviousHash.IsEmpty)
        {
            writer.Write(ReadOnlySpan<byte>.Empty);
        }
        else
        {
            writer.Write(@event.PreviousHash.Span);
        }

        // 4. PayloadBytes (as binary - the event-specific data)
        // Each event type provides its own GetPayloadBytes() method
        var payloadBytes = @event switch
        {
            PlateCreatedEvent e => e.GetPayloadBytes(),
            PlateDestroyedEvent e => e.GetPayloadBytes(),
            BoundaryCreatedEvent e => e.GetPayloadBytes(),
            BoundaryDestroyedEvent e => e.GetPayloadBytes(),
            BoundaryMotionSetEvent e => e.GetPayloadBytes(),
            _ => throw new NotSupportedException($"Event type {@event.GetType().Name} does not support hash preimage serialization.")
        };
        writer.Write(payloadBytes);

        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }
}

/// <summary>
/// SHA-256 implementation of IEventHasher.
/// Computes hashes using the SHA-256 algorithm (32 bytes output).
/// </summary>
public sealed class Sha256EventHasher : IEventHasher
{
    /// <summary>
    /// Shared instance for convenience.
    /// </summary>
    public static readonly Sha256EventHasher Instance = new();

    /// <inheritdoc/>
    public byte[] Hash(ReadOnlySpan<byte> preimageBytes)
    {
        return SHA256.HashData(preimageBytes);
    }

    /// <inheritdoc/>
    public byte[] ComputeEventHash(
        CanonicalTick tick,
        TruthStreamIdentity streamIdentity,
        ReadOnlyMemory<byte> previousHash,
        ReadOnlySpan<byte> payloadBytes)
    {
        // Build the preimage: [Tick, StreamIdentity, PreviousHash, PayloadBytes]
        // We use MessagePack to ensure deterministic encoding
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        // Write array header for 4 elements
        writer.WriteArrayHeader(4);

        // 1. Tick (as raw Int64)
        writer.Write(tick.Value);

        // 2. StreamIdentity (as array of 5 elements)
        writer.WriteArrayHeader(5);
        writer.Write(streamIdentity.VariantId);
        writer.Write(streamIdentity.BranchId);
        writer.Write(streamIdentity.LLevel);
        writer.Write(streamIdentity.Domain.Value);
        writer.Write(streamIdentity.Model);

        // 3. PreviousHash (as binary)
        if (previousHash.IsEmpty)
        {
            writer.Write(ReadOnlySpan<byte>.Empty);
        }
        else
        {
            writer.Write(previousHash.Span);
        }

        // 4. PayloadBytes (as binary - the actual event-specific data)
        writer.Write(payloadBytes);

        writer.Flush();

        // Hash the preimage
        return SHA256.HashData(buffer.WrittenSpan);
    }
}

/// <summary>
/// Utility class for building event chains with proper hash computation.
/// </summary>
public static class EventChainBuilder
{
    private static readonly IEventHasher DefaultHasher = Sha256EventHasher.Instance;

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="event">The event to hash (PreviousHash should already be set).</param>
    /// <param name="hasher">Optional hasher (defaults to SHA-256).</param>
    /// <returns>A new event with the computed Hash.</returns>
    public static PlateCreatedEvent WithComputedHash(
        this PlateCreatedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static PlateRetiredEvent WithComputedHash(
        this PlateRetiredEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static BoundaryCreatedEvent WithComputedHash(
        this BoundaryCreatedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static BoundaryTypeChangedEvent WithComputedHash(
        this BoundaryTypeChangedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static BoundaryGeometryUpdatedEvent WithComputedHash(
        this BoundaryGeometryUpdatedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static BoundaryRetiredEvent WithComputedHash(
        this BoundaryRetiredEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static JunctionCreatedEvent WithComputedHash(
        this JunctionCreatedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static JunctionUpdatedEvent WithComputedHash(
        this JunctionUpdatedEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Computes the hash for an event and returns a new event with the hash set.
    /// </summary>
    public static JunctionRetiredEvent WithComputedHash(
        this JunctionRetiredEvent @event,
        IEventHasher? hasher = null)
    {
        hasher ??= DefaultHasher;
        var payloadBytes = GetPayloadBytes(@event);
        var hash = hasher.ComputeEventHash(@event.Tick, @event.StreamIdentity, @event.PreviousHash, payloadBytes);
        return @event with { Hash = hash };
    }

    /// <summary>
    /// Gets the payload bytes for an event (event-specific fields only, not envelope fields).
    /// </summary>
    private static byte[] GetPayloadBytes(PlateCreatedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.PlateId.Value.ToString());
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(PlateRetiredEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(3);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.PlateId.Value.ToString());
        writer.Write(@event.Reason);
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(BoundaryCreatedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(5);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.BoundaryId.Value.ToString());
        writer.Write(@event.PlateIdLeft.Value.ToString());
        writer.Write(@event.PlateIdRight.Value.ToString());
        writer.Write((byte)@event.BoundaryType);
        // Note: Geometry is not included in payload hash to keep it simple for now
        // In a full implementation, you'd serialize the geometry deterministically
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(BoundaryTypeChangedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(4);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.BoundaryId.Value.ToString());
        writer.Write((byte)@event.OldType);
        writer.Write((byte)@event.NewType);
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(BoundaryGeometryUpdatedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.BoundaryId.Value.ToString());
        // Note: Geometry is not included in payload hash to keep it simple for now
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(BoundaryRetiredEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(3);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.BoundaryId.Value.ToString());
        writer.Write(@event.Reason);
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(JunctionCreatedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(4);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.JunctionId.Value.ToString());
        writer.WriteArrayHeader(@event.BoundaryIds.Length);
        foreach (var boundaryId in @event.BoundaryIds)
        {
            writer.Write(boundaryId.Value.ToString());
        }
        writer.Write(@event.Location.X);
        writer.Write(@event.Location.Y);
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(JunctionUpdatedEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(4);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.JunctionId.Value.ToString());
        writer.WriteArrayHeader(@event.NewBoundaryIds.Length);
        foreach (var boundaryId in @event.NewBoundaryIds)
        {
            writer.Write(boundaryId.Value.ToString());
        }
        if (@event.NewLocation.HasValue)
        {
            writer.Write(@event.NewLocation.Value.X);
            writer.Write(@event.NewLocation.Value.Y);
        }
        else
        {
            writer.Write(double.NaN);
            writer.Write(double.NaN);
        }
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    private static byte[] GetPayloadBytes(JunctionRetiredEvent @event)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(3);
        writer.Write(@event.EventId.ToString());
        writer.Write(@event.JunctionId.Value.ToString());
        writer.Write(@event.Reason);
        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }
}

/// <summary>
/// Validates hash chains in event streams.
/// </summary>
public static class EventChainValidator
{
    /// <summary>
    /// Validates that a sequence of events forms a valid hash chain.
    /// </summary>
    /// <param name="events">The events to validate.</param>
    /// <returns>True if the chain is valid; false otherwise.</returns>
    public static bool ValidateChain(ReadOnlySpan<IPlateTopologyEvent> events)
    {
        if (events.Length == 0)
            return true;

        // Genesis event: PreviousHash should be empty
        if (!events[0].PreviousHash.IsEmpty)
            return false;

        // Each subsequent event's PreviousHash must match the previous event's Hash
        for (int i = 1; i < events.Length; i++)
        {
            var prevHash = events[i - 1].Hash;
            var currPrevHash = events[i].PreviousHash;

            if (!prevHash.Span.SequenceEqual(currPrevHash.Span))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a sequence of events forms a valid hash chain.
    /// </summary>
    /// <param name="events">The events to validate.</param>
    /// <returns>True if the chain is valid; false otherwise.</returns>
    public static bool ValidateChain(IReadOnlyList<IPlateTopologyEvent> events)
    {
        if (events.Count == 0)
            return true;

        // Genesis event: PreviousHash should be empty
        if (!events[0].PreviousHash.IsEmpty)
            return false;

        // Each subsequent event's PreviousHash must match the previous event's Hash
        for (int i = 1; i < events.Count; i++)
        {
            var prevHash = events[i - 1].Hash;
            var currPrevHash = events[i].PreviousHash;

            if (!prevHash.Span.SequenceEqual(currPrevHash.Span))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the hash of the last event in the chain (for use as PreviousHash of next event).
    /// </summary>
    /// <param name="events">The events.</param>
    /// <returns>The hash of the last event, or empty if no events.</returns>
    public static ReadOnlyMemory<byte> GetLastHash(IReadOnlyList<IPlateTopologyEvent> events)
    {
        if (events.Count == 0)
            return ReadOnlyMemory<byte>.Empty;

        return events[^1].Hash;
    }
}
