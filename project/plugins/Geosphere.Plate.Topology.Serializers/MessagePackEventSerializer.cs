using System;
using System.Buffers;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers;

// Helper extension method for ReadOnlySequence<T>
internal static class ReadOnlySequenceExtensions
{
    public static byte[] ToByteArray(this System.Buffers.ReadOnlySequence<byte> sequence)
    {
        if (sequence.Length == 0)
            return Array.Empty<byte>();

        if (sequence.IsSingleSegment)
        {
            return sequence.FirstSpan.ToArray();
        }

        var bytes = new byte[sequence.Length];
        sequence.CopyTo(bytes);
        return bytes;
    }
}

/// <summary>
/// MessagePack serializer for plate topology events with envelope-based polymorphic API.
///
/// Envelope format: [eventType:string, payload:binary]
/// - eventType: string name of event type (e.g., "PlateCreatedEvent")
/// - payload: binary encoded event data using event-specific numeric arrays
///
/// Payload format: numeric arrays only (no string keys/maps)
/// - Domain is serialized as plain string
/// - Geometry uses numeric discriminators
/// - CanonicalTick is encoded as raw Int64
///
/// API:
/// - Serialize<T>(T event): Writes [eventType, payload<T>]
/// - Deserialize(byte[] data): Reads envelope, dispatches to appropriate formatter
/// - Deserialize<T>(byte[] data): Reads envelope, asserts eventType matches, decodes payload
/// </summary>
public static class MessagePackEventSerializer
{
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                new GeometryFormatter(),
                new Point2Formatter(),
                new Segment2Formatter(),
                new Polyline2Formatter(),
                new DomainFormatter(),
                new CanonicalTickFormatter(),
                new PlateIdFormatter(),
                new BoundaryIdFormatter(),
                new JunctionIdFormatter(),
                new EventIdFormatter(),

                // Generated Formatters
                new FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentityMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.PlateCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.PlateRetiredEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryTypeChangedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryGeometryUpdatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryRetiredEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionUpdatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionRetiredEventMessagePackFormatter()
            },
            new IFormatterResolver[]
            {
                NativeGuidResolver.Instance,
                BuiltinResolver.Instance,
                StandardResolver.Instance
            }
        ));

    // Event type name to formatter mapping for polymorphic deserialization
    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        { nameof(PlateCreatedEvent), typeof(PlateCreatedEvent) },
        { nameof(PlateRetiredEvent), typeof(PlateRetiredEvent) },
        { nameof(BoundaryCreatedEvent), typeof(BoundaryCreatedEvent) },
        { nameof(BoundaryTypeChangedEvent), typeof(BoundaryTypeChangedEvent) },
        { nameof(BoundaryGeometryUpdatedEvent), typeof(BoundaryGeometryUpdatedEvent) },
        { nameof(BoundaryRetiredEvent), typeof(BoundaryRetiredEvent) },
        { nameof(JunctionCreatedEvent), typeof(JunctionCreatedEvent) },
        { nameof(JunctionUpdatedEvent), typeof(JunctionUpdatedEvent) },
        { nameof(JunctionRetiredEvent), typeof(JunctionRetiredEvent) }
    };

    /// <summary>
    /// Serializes an event to a MessagePack byte array with envelope.
    /// Format: [eventType:string, payload:binary]
    /// </summary>
    /// <typeparam name="T">The event type to serialize.</typeparam>
    /// <param name="value">The event to serialize.</param>
    /// <returns>A byte array containing [eventType, payload].</returns>
    public static byte[] Serialize<T>(T value) where T : IPlateTopologyEvent
    {
        var payloadBytes = MessagePackSerializer.Serialize(value, Options);

        // Serialize envelope directly using MessagePack
        // Envelope is: [eventType:string, payload:binary]
        var eventType = ((IPlateTopologyEvent)value).EventType;

        // Create envelope bytes manually
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(eventType);
        writer.Write(payloadBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    public static byte[] Serialize(IPlateTopologyEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var payloadBytes = MessagePackSerializer.Serialize(value.GetType(), value, Options);
        var eventType = value.EventType;

        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(eventType);
        writer.Write(payloadBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Deserializes an event from a MessagePack byte array.
    /// Reads envelope [eventType, payload] and dispatches to appropriate formatter.
    /// </summary>
    /// <param name="data">The byte array containing [eventType, payload].</param>
    /// <returns>The deserialized event as IPlateTopologyEvent.</returns>
    public static IPlateTopologyEvent Deserialize(byte[] data)
    {
        var reader = new MessagePackReader(data);

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Envelope must have 2 elements, got {length}");

        var eventType = reader.ReadString();
        if (eventType == null)
            throw new InvalidOperationException("Envelope eventType cannot be null");

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Envelope payload cannot be null");

        var payloadArray = payloadBytes.Value.ToByteArray();

        // Use ReadOnlySequence directly for deserialization
        if (!EventTypeMap.TryGetValue(eventType, out var eventTypeType))
            throw new InvalidOperationException($"Unknown event type: {eventType}");

        var eventObj = MessagePackSerializer.Deserialize(eventTypeType, payloadArray, Options);
        return (IPlateTopologyEvent)eventObj!;
    }

    /// <summary>
    /// Deserializes a specific event type, validating the envelope matches.
    /// </summary>
    public static T Deserialize<T>(byte[] data) where T : IPlateTopologyEvent
    {
        var reader = new MessagePackReader(data);
        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Invalid event envelope. Expected 2 elements, got {length}");

        var eventType = reader.ReadString();
        // We verify event type matches T (assuming T.EventType matches class name)
        // Actually T is the type, we should check if eventType maps to T.

        // This check is a bit loose but sufficient for now.
        // Ideally we check ((IPlateTopologyEvent)default(T)).EventType but T might not have default.
        // For strictness we could rely on EventTypeMap.
        if (!EventTypeMap.TryGetValue(eventType!, out var type) || !typeof(T).IsAssignableFrom(type))
        {
             // It's okay if T is IPlateTopologyEvent (handled above)
             // But if T is PlateCreatedEvent and eventType is "PlateRetiredEvent", this should fail.
             // Warning: strict check might fail if T is interface.
             // If T is concrete:
             if (typeof(T).Name != eventType)
             {
                 throw new InvalidOperationException($"Event type mismatch. Expected {typeof(T).Name}, got {eventType}");
             }
        }

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Event payload cannot be null");

        var payload = payloadBytes.Value.ToByteArray();
        return MessagePackSerializer.Deserialize<T>(payload, Options);
    }
}
