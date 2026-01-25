using System;
using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for EventId.
/// Encoded as raw Guid (binary).
/// </summary>
internal class EventIdFormatter : IMessagePackFormatter<EventId>
{
    public void Serialize(ref MessagePackWriter writer, EventId value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<Guid>().Serialize(ref writer, value.Value, options);
    }

    public EventId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var guid = options.Resolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, options);
        return new EventId(guid);
    }
}
