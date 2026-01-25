using System;
using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for Segment2.
/// Format: [start_x, start_y, end_x, end_y]
/// </summary>
internal class Segment2Formatter : IMessagePackFormatter<Segment2>
{
    public void Serialize(ref MessagePackWriter writer, Segment2 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.Start.X);
        writer.Write(value.Start.Y);
        writer.Write(value.End.X);
        writer.Write(value.End.Y);
    }

    public Segment2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            throw new InvalidOperationException("Segment2 cannot be nil");
        }

        var count = reader.ReadArrayHeader();
        if (count != 4)
        {
            throw new InvalidOperationException($"Segment2 expected 4 elements, got {count}");
        }

        var sx = reader.ReadDouble();
        var sy = reader.ReadDouble();
        var ex = reader.ReadDouble();
        var ey = reader.ReadDouble();
        return new Segment2(new Point2(sx, sy), new Point2(ex, ey));
    }
}
