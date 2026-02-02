using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// MessagePack formatter for Segment2 type.
/// </summary>
public sealed class Segment2Formatter : IMessagePackFormatter<Segment2>
{
    public void Serialize(ref MessagePackWriter writer, Segment2 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        MessagePackSerializer.Serialize(ref writer, value.Start, options);
        MessagePackSerializer.Serialize(ref writer, value.End, options);
    }

    public Segment2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 2)
        {
            throw new MessagePackSerializationException($"Invalid Segment2 format. Expected 2 elements but got {length}");
        }

        var start = MessagePackSerializer.Deserialize<Point2>(ref reader, options);
        var end = MessagePackSerializer.Deserialize<Point2>(ref reader, options);
        return new Segment2(start, end);
    }
}
