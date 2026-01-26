using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Kinematics.Serializers.Formatters;

internal sealed class PlateIdFormatter : IMessagePackFormatter<PlateId>
{
    public void Serialize(ref MessagePackWriter writer, PlateId value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<Guid>().Serialize(ref writer, value.Value, options);
    }

    public PlateId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var guid = options.Resolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, options);
        return new PlateId(guid);
    }
}
