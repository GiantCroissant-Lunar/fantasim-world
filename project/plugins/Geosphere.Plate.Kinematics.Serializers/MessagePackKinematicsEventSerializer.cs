using System.Buffers;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Serializers.Formatters;

namespace FantaSim.Geosphere.Plate.Kinematics.Serializers;

internal static class ReadOnlySequenceExtensions
{
    public static byte[] ToByteArray(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.Length == 0)
            return Array.Empty<byte>();

        if (sequence.IsSingleSegment)
            return sequence.FirstSpan.ToArray();

        var bytes = new byte[checked((int)sequence.Length)];
        sequence.CopyTo(bytes);
        return bytes;
    }
}

public static class MessagePackKinematicsEventSerializer
{
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                new DomainFormatter(),
                new CanonicalTickFormatter(),
                new PlateIdFormatter(),
                new MotionSegmentIdFormatter(),

                // Generated formatters
                new FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentityMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Kinematics.Contracts.Events.PlateMotionModelAssignedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Kinematics.Contracts.Events.MotionSegmentUpsertedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Kinematics.Contracts.Events.MotionSegmentRetiredEventMessagePackFormatter()
            },
            new IFormatterResolver[]
            {
                NativeGuidResolver.Instance,
                BuiltinResolver.Instance,
                StandardResolver.Instance
            }
        ));

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        { nameof(PlateMotionModelAssignedEvent), typeof(PlateMotionModelAssignedEvent) },
        { nameof(MotionSegmentUpsertedEvent), typeof(MotionSegmentUpsertedEvent) },
        { nameof(MotionSegmentRetiredEvent), typeof(MotionSegmentRetiredEvent) }
    };

    public static byte[] Serialize(IPlateKinematicsEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var payloadBytes = MessagePackSerializer.Serialize(value.GetType(), value, Options);
        var eventType = value.EventType;

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(eventType);
        writer.Write(payloadBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    public static IPlateKinematicsEvent Deserialize(byte[] data)
    {
        var reader = new MessagePackReader(data);

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Envelope must have 2 elements, got {length}");

        var eventType = reader.ReadString();
        if (eventType is null)
            throw new InvalidOperationException("Envelope eventType cannot be null");

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Envelope payload cannot be null");

        var payloadArray = payloadBytes.Value.ToByteArray();

        if (!EventTypeMap.TryGetValue(eventType, out var eventTypeType))
            throw new InvalidOperationException($"Unknown event type: {eventType}");

        var eventObj = MessagePackSerializer.Deserialize(eventTypeType, payloadArray, Options);
        return (IPlateKinematicsEvent)eventObj!;
    }

    public static T Deserialize<T>(byte[] data) where T : IPlateKinematicsEvent
    {
        var reader = new MessagePackReader(data);
        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Invalid event envelope. Expected 2 elements, got {length}");

        var eventType = reader.ReadString();
        if (eventType is null)
            throw new InvalidOperationException("Envelope eventType cannot be null");

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Event payload cannot be null");

        var payload = payloadBytes.Value.ToByteArray();
        return MessagePackSerializer.Deserialize<T>(payload, Options);
    }
}
