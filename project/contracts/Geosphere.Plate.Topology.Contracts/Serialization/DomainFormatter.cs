using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Serialization;

public sealed class DomainFormatter : IMessagePackFormatter<Domain>
{
    public void Serialize(ref MessagePackWriter writer, Domain value, MessagePackSerializerOptions options)
    {
        if (value.IsEmpty)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(value.Value);
    }

    public Domain Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var s = reader.ReadString();
        if (string.IsNullOrEmpty(s))
        {
            return default;
        }

        return Domain.Parse(s);
    }
}
