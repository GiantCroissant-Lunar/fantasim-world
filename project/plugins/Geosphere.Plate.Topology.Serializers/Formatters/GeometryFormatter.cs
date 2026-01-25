using System;
using MessagePack;
using MessagePack.Formatters;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for IGeometry using numeric discriminators.
/// Format: [GeometryType, ...type-specific data]
/// Point: [0, x, y]
/// LineSegment: [1, startX, startY, endX, endY]
/// Polyline: [2, x1, y1, x2, y2, ...]
/// </summary>
internal class GeometryFormatter : IMessagePackFormatter<IGeometry?>
{
    public void Serialize(ref MessagePackWriter writer, IGeometry? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        switch (value)
        {
            case Point2 point:
                writer.WriteArrayHeader(3);
                writer.Write((byte)GeometryType.Point);
                writer.Write(point.X);
                writer.Write(point.Y);
                break;

            case Segment2 segment:
                writer.WriteArrayHeader(5);
                writer.Write((byte)GeometryType.LineSegment);
                writer.Write(segment.Start.X);
                writer.Write(segment.Start.Y);
                writer.Write(segment.End.X);
                writer.Write(segment.End.Y);
                break;

            case Polyline2 polyline:
                writer.WriteArrayHeader(2 + polyline.Count * 2);
                writer.Write((byte)GeometryType.Polyline);
                foreach (var point in polyline.Points)
                {
                    writer.Write(point.X);
                    writer.Write(point.Y);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown geometry type: {value.GetType()}");
        }
    }

    public IGeometry? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var length = reader.ReadArrayHeader();
        if (length < 1)
            throw new InvalidOperationException("Invalid geometry data: empty array");

        var geometryType = (GeometryType)reader.ReadByte();

        return geometryType switch
        {
            GeometryType.Point => DeserializePoint(ref reader, length),
            GeometryType.LineSegment => DeserializeLineSegment(ref reader, length),
            GeometryType.Polyline => DeserializePolyline(ref reader, length),
            _ => throw new InvalidOperationException($"Unknown geometry type: {geometryType}")
        };
    }

    private Point2 DeserializePoint(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength != 3)
            throw new InvalidOperationException($"Point geometry must have 3 elements, got {expectedLength}");

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        return new Point2(x, y);
    }

    private Segment2 DeserializeLineSegment(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength != 5)
            throw new InvalidOperationException($"LineSegment geometry must have 5 elements, got {expectedLength}");

        var startX = reader.ReadDouble();
        var startY = reader.ReadDouble();
        var endX = reader.ReadDouble();
        var endY = reader.ReadDouble();
        return new Segment2(new Point2(startX, startY), new Point2(endX, endY));
    }

    private Polyline2 DeserializePolyline(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength < 2)
            throw new InvalidOperationException($"Polyline geometry must have at least 2 elements, got {expectedLength}");

        var pointCount = (expectedLength - 1) / 2;
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
