using MessagePack;
using MessagePack.Formatters;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Serializers.Formatters;

internal sealed class CanonicalTickFormatter : IMessagePackFormatter<CanonicalTick>
{
    public void Serialize(ref MessagePackWriter writer, CanonicalTick value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public CanonicalTick Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new CanonicalTick(reader.ReadInt64());
    }
}
