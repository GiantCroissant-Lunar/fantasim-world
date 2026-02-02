using System;
using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for Polyline2.
/// Format: [x1, y1, x2, y2, ...]
/// </summary>
internal sealed class Polyline2Formatter : IMessagePackFormatter<Polyline2?>
{
    public void Serialize(ref MessagePackWriter writer, Polyline2? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var points = value.Points;
        writer.WriteArrayHeader(value.PointCount * 2);
        foreach (var p in points)
        {
            writer.Write(p.X);
            writer.Write(p.Y);
        }
    }

    public Polyline2? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var count = reader.ReadArrayHeader();
        if (count % 2 != 0)
        {
            throw new InvalidOperationException($"Polyline2 expected even number of elements, got {count}");
        }

        var pointCount = count / 2;
        var points = new Point2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            points[i] = new Point2(x, y);
        }

        return new Polyline2(points);
    }
}
