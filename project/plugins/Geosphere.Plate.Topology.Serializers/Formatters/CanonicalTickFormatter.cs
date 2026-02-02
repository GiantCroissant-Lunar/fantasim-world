using System;
using MessagePack;
using MessagePack.Formatters;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for CanonicalTick.
/// Per RFC-V2-0010 and RFC-V2-0005, CanonicalTick is encoded as raw Int64.
/// </summary>
internal sealed class CanonicalTickFormatter : IMessagePackFormatter<CanonicalTick>
{
    public void Serialize(ref MessagePackWriter writer, CanonicalTick value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public CanonicalTick Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var tickValue = reader.ReadInt64();
        return new CanonicalTick(tickValue);
    }
}
