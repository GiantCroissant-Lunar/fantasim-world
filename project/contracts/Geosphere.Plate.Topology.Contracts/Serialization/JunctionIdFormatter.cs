using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Serialization;

public sealed class JunctionIdFormatter : IMessagePackFormatter<JunctionId>
{
    public void Serialize(ref MessagePackWriter writer, JunctionId value, MessagePackSerializerOptions options)
    {
        var formatter = options.Resolver.GetFormatterWithVerify<Guid>();
        formatter.Serialize(ref writer, value.Value, options);
    }

    public JunctionId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var formatter = options.Resolver.GetFormatterWithVerify<Guid>();
        var guid = formatter.Deserialize(ref reader, options);
        return new JunctionId(guid);
    }
}
