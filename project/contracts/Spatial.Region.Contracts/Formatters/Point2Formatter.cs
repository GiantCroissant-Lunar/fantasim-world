using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Spatial.Region.Contracts.Formatters;

/// <summary>
/// MessagePack formatter for Point2 type from UnifyGeometry.
/// </summary>
public sealed class Point2Formatter : IMessagePackFormatter<Point2>
{
    public void Serialize(ref MessagePackWriter writer, Point2 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public Point2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 2)
        {
            throw new MessagePackSerializationException($"Invalid Point2 format. Expected 2 elements but got {length}");
        }

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        return new Point2(x, y);
    }
}
