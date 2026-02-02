using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// MessagePack formatter for Polyline2 type.
/// </summary>
public sealed class Polyline2Formatter : IMessagePackFormatter<Polyline2?>
{
    public void Serialize(ref MessagePackWriter writer, Polyline2? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        var pointsList = new List<Point2>();
        foreach (var point in value.Points)
        {
            pointsList.Add(point);
        }
        writer.WriteArrayHeader(pointsList.Count);
        foreach (var point in pointsList)
        {
            MessagePackSerializer.Serialize(ref writer, point, options);
        }
    }

    public Polyline2? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var length = reader.ReadArrayHeader();
        var points = new List<Point2>(length);
        for (int i = 0; i < length; i++)
        {
            points.Add(MessagePackSerializer.Deserialize<Point2>(ref reader, options));
        }
        return new Polyline2(points);
    }
}
