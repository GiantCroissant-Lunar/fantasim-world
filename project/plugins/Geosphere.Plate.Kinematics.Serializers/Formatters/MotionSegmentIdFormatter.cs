using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Kinematics.Serializers.Formatters;

internal sealed class MotionSegmentIdFormatter : IMessagePackFormatter<MotionSegmentId>
{
    public void Serialize(ref MessagePackWriter writer, MotionSegmentId value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<Guid>().Serialize(ref writer, value.Value, options);
    }

    public MotionSegmentId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var guid = options.Resolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, options);
        return new MotionSegmentId(guid);
    }
}
