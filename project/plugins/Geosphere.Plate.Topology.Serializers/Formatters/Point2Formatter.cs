using System;
using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for Point2.
/// Format: [x, y]
/// </summary>
internal class Point2Formatter : IMessagePackFormatter<Point2>
{
    public void Serialize(ref MessagePackWriter writer, Point2 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public Point2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            throw new InvalidOperationException("Point2 cannot be nil");
        }

        var count = reader.ReadArrayHeader();
        if (count != 2)
        {
            throw new InvalidOperationException($"Point2 expected 2 elements, got {count}");
        }

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        return new Point2(x, y);
    }
}
