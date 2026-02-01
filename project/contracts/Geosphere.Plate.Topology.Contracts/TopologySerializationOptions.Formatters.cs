using MessagePack;
using MessagePack.Formatters;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// MessagePack formatter for Point2 type.
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

/// <summary>
/// MessagePack formatter for Polyline2 type.
/// </summary>
public sealed class Polyline2Formatter : IMessagePackFormatter<Polyline2>
{
    public void Serialize(ref MessagePackWriter writer, Polyline2 value, MessagePackSerializerOptions options)
    {
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

    public Polyline2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        var points = new List<Point2>(length);
        for (int i = 0; i < length; i++)
        {
            points.Add(MessagePackSerializer.Deserialize<Point2>(ref reader, options));
        }
        return new Polyline2(points);
    }
}