using System;
using MessagePack;
using MessagePack.Formatters;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Serialization;

public sealed class CanonicalTickFormatter : IMessagePackFormatter<CanonicalTick>
{
    public void Serialize(ref MessagePackWriter writer, CanonicalTick value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public CanonicalTick Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var value = reader.ReadInt64();
        return new CanonicalTick(value);
    }
}
