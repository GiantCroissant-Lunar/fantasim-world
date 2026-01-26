using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Kinematics.Serializers.Formatters;

internal sealed class DomainFormatter : IMessagePackFormatter<Domain>
{
    public void Serialize(ref MessagePackWriter writer, Domain value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public Domain Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var str = reader.ReadString();
        if (str is null)
            throw new InvalidOperationException("Domain value cannot be null");
        return Domain.Parse(str);
    }
}
