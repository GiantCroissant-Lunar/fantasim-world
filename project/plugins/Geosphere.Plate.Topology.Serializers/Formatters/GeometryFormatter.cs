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
internal sealed class GeometryFormatter : IMessagePackFormatter<IGeometry?>
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
                writer.WriteArrayHeader(1 + polyline.Count * 2);
                writer.Write((byte)GeometryType.Polyline);
                foreach (var point in polyline.Points)
                {
                    writer.Write(point.X);
                    writer.Write(point.Y);
                }
                break;

            case Point3 point:
                writer.WriteArrayHeader(4);
                writer.Write((byte)GeometryType.Point3);
                writer.Write(point.X);
                writer.Write(point.Y);
                writer.Write(point.Z);
                break;

            case Segment3 segment:
                writer.WriteArrayHeader(7);
                writer.Write((byte)GeometryType.LineSegment3);
                writer.Write(segment.Start.X);
                writer.Write(segment.Start.Y);
                writer.Write(segment.Start.Z);
                writer.Write(segment.End.X);
                writer.Write(segment.End.Y);
                writer.Write(segment.End.Z);
                break;

            case Polyline3 polyline:
                writer.WriteArrayHeader(1 + polyline.Count * 3);
                writer.Write((byte)GeometryType.Polyline3);
                for (var i = 0; i < polyline.PointCount; i++)
                {
                    var point3 = polyline[i];
                    writer.Write(point3.X);
                    writer.Write(point3.Y);
                    writer.Write(point3.Z);
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
            GeometryType.Point3 => DeserializePoint3(ref reader, length),
            GeometryType.LineSegment3 => DeserializeLineSegment3(ref reader, length),
            GeometryType.Polyline3 => DeserializePolyline3(ref reader, length),
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
        if (expectedLength < 1)
            throw new InvalidOperationException($"Polyline geometry must have at least 1 element, got {expectedLength}");

        if ((expectedLength - 1) % 2 != 0)
            throw new InvalidOperationException($"Polyline geometry must have an even number of coordinate values, got {expectedLength}");

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

    private Point3 DeserializePoint3(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength != 4)
            throw new InvalidOperationException($"Point3 geometry must have 4 elements, got {expectedLength}");

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();
        return new Point3(x, y, z);
    }

    private Segment3 DeserializeLineSegment3(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength != 7)
            throw new InvalidOperationException($"LineSegment3 geometry must have 7 elements, got {expectedLength}");

        var startX = reader.ReadDouble();
        var startY = reader.ReadDouble();
        var startZ = reader.ReadDouble();
        var endX = reader.ReadDouble();
        var endY = reader.ReadDouble();
        var endZ = reader.ReadDouble();
        return new Segment3(new Point3(startX, startY, startZ), new Point3(endX, endY, endZ));
    }

    private Polyline3 DeserializePolyline3(ref MessagePackReader reader, int expectedLength)
    {
        if (expectedLength < 1)
            throw new InvalidOperationException($"Polyline3 geometry must have at least 1 element, got {expectedLength}");

        if ((expectedLength - 1) % 3 != 0)
            throw new InvalidOperationException($"Polyline3 geometry must have a multiple of 3 coordinate values, got {expectedLength}");

        var pointCount = (expectedLength - 1) / 3;
        var points = new Point3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            points[i] = new Point3(x, y, z);
        }

        return new Polyline3(points);
    }
}
