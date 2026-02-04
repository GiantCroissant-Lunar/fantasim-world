using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Spatial.Region.Contracts.Formatters;

/// <summary>
/// MessagePack formatter for Point3 type from UnifyGeometry.
/// </summary>
public sealed class Point3Formatter : IMessagePackFormatter<Point3>
{
    public void Serialize(ref MessagePackWriter writer, Point3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    public Point3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 3)
        {
            throw new MessagePackSerializationException($"Invalid Point3 format. Expected 3 elements but got {length}");
        }

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();
        return new Point3(x, y, z);
    }
}
