using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Serializers;

// Helper extension method for ReadOnlySequence<T>
internal static class ReadOnlySequenceExtensions
{
    public static byte[] ToByteArray(this System.Buffers.ReadOnlySequence<byte>? sequence)
    {
        if (sequence == null || sequence.Value.Length == 0)
            return Array.Empty<byte>();

        if (sequence.Value.IsSingleSegment)
        {
            return sequence.Value.FirstSpan.ToArray();
        }

        var bytes = new byte[sequence.Value.Length];
        sequence.Value.CopyTo(bytes);
        return bytes;
    }
}

#region Helper Classes

/// <summary>
/// Custom MessagePack formatter for Domain type.
/// Domain has a private constructor which requires custom serialization.
/// </summary>
internal class DomainFormatter : IMessagePackFormatter<Domain>
{
    public void Serialize(ref MessagePackWriter writer, Domain value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public Domain Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var strValue = reader.ReadString();
        if (strValue == null)
            throw new InvalidOperationException("Domain value cannot be null");
        return Domain.Parse(strValue);
    }
}

/// <summary>
/// Helper class for CanonicalTick serialization.
/// Per RFC-V2-0010 and RFC-V2-0005, CanonicalTick is encoded as raw Int64.
/// Format: int64 (tick value)
/// </summary>
internal static class CanonicalTickHelper
{
    public static void Serialize(ref MessagePackWriter writer, CanonicalTick value)
    {
        writer.Write(value.Value);
    }

    public static CanonicalTick Deserialize(ref MessagePackReader reader)
    {
        var tickValue = reader.ReadInt64();
        return new CanonicalTick(tickValue);
    }
}

/// <summary>
/// Helper class for hash field serialization (PreviousHash, Hash).
/// Hashes are encoded as binary (bin8/bin16/bin32) per MessagePack spec.
/// Empty/null hashes are encoded as zero-length binary.
/// </summary>
internal static class HashHelper
{
    public static void Serialize(ref MessagePackWriter writer, ReadOnlyMemory<byte> value)
    {
        if (value.IsEmpty)
        {
            writer.Write(ReadOnlySpan<byte>.Empty);
        }
        else
        {
            writer.Write(value.Span);
        }
    }

    public static ReadOnlyMemory<byte> Deserialize(ref MessagePackReader reader)
    {
        var bytes = reader.ReadBytes();
        if (!bytes.HasValue || bytes.Value.Length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        return bytes.ToByteArray();
    }
}

/// <summary>
/// Custom MessagePack formatter for IGeometry using numeric discriminators.
/// Format: [GeometryType, ...type-specific data]
/// Point: [0, x, y]
/// LineSegment: [1, startX, startY, endX, endY]
/// Polyline: [2, x1, y1, x2, y2, ...]
/// </summary>
internal class GeometryFormatter : IMessagePackFormatter<IGeometry>
{
    public void Serialize(ref MessagePackWriter writer, IGeometry value, MessagePackSerializerOptions options)
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

    public IGeometry Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return Point2.Empty;
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

/// <summary>
/// Helper class for serializing TruthStreamIdentity as numeric array.
/// Format: [VariantId, BranchId, LLevel, Domain, Model]
/// Rejects null strings.
/// </summary>
internal static class StreamIdentityHelper
{
    private static readonly DomainFormatter DomainFormatter = new();

    public static void Serialize(ref MessagePackWriter writer, TruthStreamIdentity identity)
    {
        if (identity.VariantId == null)
            throw new InvalidOperationException("StreamIdentity.VariantId cannot be null");
        if (identity.BranchId == null)
            throw new InvalidOperationException("StreamIdentity.BranchId cannot be null");
        if (identity.Model == null)
            throw new InvalidOperationException("StreamIdentity.Model cannot be null");

        writer.WriteArrayHeader(5);
        writer.Write(identity.VariantId);
        writer.Write(identity.BranchId);
        writer.Write(identity.LLevel);
        DomainFormatter.Serialize(ref writer, identity.Domain, MessagePackSerializerOptions.Standard);
        writer.Write(identity.Model);
    }

    public static TruthStreamIdentity Deserialize(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 5)
            throw new InvalidOperationException($"TruthStreamIdentity must have 5 elements, got {length}");

        var variantId = reader.ReadString();
        var branchId = reader.ReadString();
        var lLevel = reader.ReadInt32();
        var domainValue = reader.ReadString();
        var model = reader.ReadString();

        if (variantId == null)
            throw new InvalidOperationException("StreamIdentity.VariantId cannot be null");
        if (branchId == null)
            throw new InvalidOperationException("StreamIdentity.BranchId cannot be null");
        if (domainValue == null)
            throw new InvalidOperationException("StreamIdentity.Domain cannot be null");
        if (model == null)
            throw new InvalidOperationException("StreamIdentity.Model cannot be null");

        return new TruthStreamIdentity(variantId, branchId, lLevel, Domain.Parse(domainValue), model);
    }
}

#endregion

#region Event Formatters

/// <summary>
/// Base formatter for events that provides payload serialization without envelope.
/// Events are serialized as numeric arrays (no string keys).
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
internal abstract class EventFormatter<TEvent> : IMessagePackFormatter<TEvent>
    where TEvent : IPlateTopologyEvent
{
    public void Serialize(ref MessagePackWriter writer, TEvent value, MessagePackSerializerOptions options)
    {
        SerializePayload(ref writer, value);
    }

    public TEvent Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return DeserializePayload(ref reader);
    }

    /// <summary>
    /// Serialize event payload as numeric array (no string keys).
    /// </summary>
    protected abstract void SerializePayload(ref MessagePackWriter writer, TEvent value);

    /// <summary>
    /// Deserialize event payload from numeric array.
    /// </summary>
    protected abstract TEvent DeserializePayload(ref MessagePackReader reader);
}

/// <summary>
/// PlateCreatedEvent formatter
/// Payload format: [EventId, PlateId, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class PlateCreatedEventFormatter : EventFormatter<PlateCreatedEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, PlateCreatedEvent value)
    {
        writer.WriteArrayHeader(7);
        writer.Write(value.EventId.ToString());
        writer.Write(value.PlateId.Value.ToString());
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override PlateCreatedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 7)
            throw new InvalidOperationException($"PlateCreatedEvent must have 7 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var plateId = new PlateId(Guid.Parse(reader.ReadString()!));
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new PlateCreatedEvent(eventId, plateId, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// PlateRetiredEvent formatter
/// Payload format: [EventId, PlateId, Reason, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class PlateRetiredEventFormatter : EventFormatter<PlateRetiredEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, PlateRetiredEvent value)
    {
        writer.WriteArrayHeader(8);
        writer.Write(value.EventId.ToString());
        writer.Write(value.PlateId.Value.ToString());
        writer.Write(value.Reason);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override PlateRetiredEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 8)
            throw new InvalidOperationException($"PlateRetiredEvent must have 8 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var plateId = new PlateId(Guid.Parse(reader.ReadString()!));
        var reason = reader.ReadString();
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new PlateRetiredEvent(eventId, plateId, reason, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// BoundaryCreatedEvent formatter
/// Payload format: [EventId, BoundaryId, PlateIdLeft, PlateIdRight, BoundaryType, Geometry, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class BoundaryCreatedEventFormatter : EventFormatter<BoundaryCreatedEvent>
{
    private static readonly GeometryFormatter GeometryFormatter = new();

    protected override void SerializePayload(ref MessagePackWriter writer, BoundaryCreatedEvent value)
    {
        writer.WriteArrayHeader(11);
        writer.Write(value.EventId.ToString());
        writer.Write(value.BoundaryId.Value.ToString());
        writer.Write(value.PlateIdLeft.Value.ToString());
        writer.Write(value.PlateIdRight.Value.ToString());
        writer.Write((byte)value.BoundaryType);
        GeometryFormatter.Serialize(ref writer, value.Geometry, MessagePackSerializerOptions.Standard);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override BoundaryCreatedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 11)
            throw new InvalidOperationException($"BoundaryCreatedEvent must have 11 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var boundaryId = new BoundaryId(Guid.Parse(reader.ReadString()!));
        var plateIdLeft = new PlateId(Guid.Parse(reader.ReadString()!));
        var plateIdRight = new PlateId(Guid.Parse(reader.ReadString()!));
        var boundaryType = (BoundaryType)reader.ReadByte();
        var geometry = GeometryFormatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new BoundaryCreatedEvent(eventId, boundaryId, plateIdLeft, plateIdRight, boundaryType, geometry, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// BoundaryTypeChangedEvent formatter
/// Payload format: [EventId, BoundaryId, OldType, NewType, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class BoundaryTypeChangedEventFormatter : EventFormatter<BoundaryTypeChangedEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, BoundaryTypeChangedEvent value)
    {
        writer.WriteArrayHeader(9);
        writer.Write(value.EventId.ToString());
        writer.Write(value.BoundaryId.Value.ToString());
        writer.Write((byte)value.OldType);
        writer.Write((byte)value.NewType);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override BoundaryTypeChangedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 9)
            throw new InvalidOperationException($"BoundaryTypeChangedEvent must have 9 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var boundaryId = new BoundaryId(Guid.Parse(reader.ReadString()!));
        var oldType = (BoundaryType)reader.ReadByte();
        var newType = (BoundaryType)reader.ReadByte();
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new BoundaryTypeChangedEvent(eventId, boundaryId, oldType, newType, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// BoundaryGeometryUpdatedEvent formatter
/// Payload format: [EventId, BoundaryId, NewGeometry, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class BoundaryGeometryUpdatedEventFormatter : EventFormatter<BoundaryGeometryUpdatedEvent>
{
    private static readonly GeometryFormatter GeometryFormatter = new();

    protected override void SerializePayload(ref MessagePackWriter writer, BoundaryGeometryUpdatedEvent value)
    {
        writer.WriteArrayHeader(8);
        writer.Write(value.EventId.ToString());
        writer.Write(value.BoundaryId.Value.ToString());
        GeometryFormatter.Serialize(ref writer, value.NewGeometry, MessagePackSerializerOptions.Standard);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override BoundaryGeometryUpdatedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 8)
            throw new InvalidOperationException($"BoundaryGeometryUpdatedEvent must have 8 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var boundaryId = new BoundaryId(Guid.Parse(reader.ReadString()!));
        var newGeometry = GeometryFormatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new BoundaryGeometryUpdatedEvent(eventId, boundaryId, newGeometry, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// BoundaryRetiredEvent formatter
/// Payload format: [EventId, BoundaryId, Reason, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class BoundaryRetiredEventFormatter : EventFormatter<BoundaryRetiredEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, BoundaryRetiredEvent value)
    {
        writer.WriteArrayHeader(8);
        writer.Write(value.EventId.ToString());
        writer.Write(value.BoundaryId.Value.ToString());
        writer.Write(value.Reason);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override BoundaryRetiredEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 8)
            throw new InvalidOperationException($"BoundaryRetiredEvent must have 8 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var boundaryId = new BoundaryId(Guid.Parse(reader.ReadString()!));
        var reason = reader.ReadString();
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new BoundaryRetiredEvent(eventId, boundaryId, reason, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// JunctionCreatedEvent formatter
/// Payload format: [EventId, JunctionId, BoundaryIdCount, [BoundaryIds], LocationX, LocationY, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// Fixed array header count: 10 elements
/// </summary>
internal class JunctionCreatedEventFormatter : EventFormatter<JunctionCreatedEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, JunctionCreatedEvent value)
    {
        // 1: eventId
        // 2: junctionId
        // 3: boundaryIds array
        // 4: locationX
        // 5: locationY
        // 6: tick
        // 7: sequence
        // 8: streamIdentity
        // 9: previousHash
        // 10: hash
        writer.WriteArrayHeader(10);
        writer.Write(value.EventId.ToString());
        writer.Write(value.JunctionId.Value.ToString());
        writer.WriteArrayHeader(value.BoundaryIds.Length);
        foreach (var boundaryId in value.BoundaryIds)
        {
            writer.Write(boundaryId.Value.ToString());
        }
        writer.Write(value.Location.X);
        writer.Write(value.Location.Y);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override JunctionCreatedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 10)
            throw new InvalidOperationException($"JunctionCreatedEvent must have 10 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var junctionId = new JunctionId(Guid.Parse(reader.ReadString()!));
        var boundaryIdCount = reader.ReadArrayHeader();
        var boundaryIds = new BoundaryId[boundaryIdCount];
        for (int i = 0; i < boundaryIdCount; i++)
        {
            boundaryIds[i] = new BoundaryId(Guid.Parse(reader.ReadString()!));
        }
        var locationX = reader.ReadDouble();
        var locationY = reader.ReadDouble();
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new JunctionCreatedEvent(eventId, junctionId, boundaryIds, new Point2(locationX, locationY), tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// JunctionUpdatedEvent formatter
/// Payload format: [EventId, JunctionId, BoundaryIdCount, [NewBoundaryIds], LocationX, LocationY, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// Location is encoded as NaN, NaN for null.
/// Fixed array header count: 10 elements
/// </summary>
internal class JunctionUpdatedEventFormatter : EventFormatter<JunctionUpdatedEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, JunctionUpdatedEvent value)
    {
        // 1: eventId
        // 2: junctionId
        // 3: boundaryIds array
        // 4: locationX
        // 5: locationY
        // 6: tick
        // 7: sequence
        // 8: streamIdentity
        // 9: previousHash
        // 10: hash
        writer.WriteArrayHeader(10);
        writer.Write(value.EventId.ToString());
        writer.Write(value.JunctionId.Value.ToString());
        writer.WriteArrayHeader(value.NewBoundaryIds.Length);
        foreach (var boundaryId in value.NewBoundaryIds)
        {
            writer.Write(boundaryId.Value.ToString());
        }
        if (value.NewLocation.HasValue)
        {
            writer.Write(value.NewLocation.Value.X);
            writer.Write(value.NewLocation.Value.Y);
        }
        else
        {
            writer.Write(double.NaN);
            writer.Write(double.NaN);
        }
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override JunctionUpdatedEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 10)
            throw new InvalidOperationException($"JunctionUpdatedEvent must have 10 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var junctionId = new JunctionId(Guid.Parse(reader.ReadString()!));
        var boundaryIdCount = reader.ReadArrayHeader();
        var newBoundaryIds = new BoundaryId[boundaryIdCount];
        for (int i = 0; i < boundaryIdCount; i++)
        {
            newBoundaryIds[i] = new BoundaryId(Guid.Parse(reader.ReadString()!));
        }
        var locationX = reader.ReadDouble();
        var locationY = reader.ReadDouble();
        var newLocation = double.IsNaN(locationX) || double.IsNaN(locationY) ? null : new Point2?(new Point2(locationX, locationY));
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new JunctionUpdatedEvent(eventId, junctionId, newBoundaryIds, newLocation, tick, sequence, streamIdentity, previousHash, hash);
    }
}

/// <summary>
/// JunctionRetiredEvent formatter
/// Payload format: [EventId, JunctionId, Reason, Tick, Sequence, StreamIdentity, PreviousHash, Hash]
/// </summary>
internal class JunctionRetiredEventFormatter : EventFormatter<JunctionRetiredEvent>
{
    protected override void SerializePayload(ref MessagePackWriter writer, JunctionRetiredEvent value)
    {
        writer.WriteArrayHeader(8);
        writer.Write(value.EventId.ToString());
        writer.Write(value.JunctionId.Value.ToString());
        writer.Write(value.Reason);
        CanonicalTickHelper.Serialize(ref writer, value.Tick);
        writer.Write(value.Sequence);
        StreamIdentityHelper.Serialize(ref writer, value.StreamIdentity);
        HashHelper.Serialize(ref writer, value.PreviousHash);
        HashHelper.Serialize(ref writer, value.Hash);
    }

    protected override JunctionRetiredEvent DeserializePayload(ref MessagePackReader reader)
    {
        var length = reader.ReadArrayHeader();
        if (length != 8)
            throw new InvalidOperationException($"JunctionRetiredEvent must have 8 elements, got {length}");

        var eventId = Guid.Parse(reader.ReadString()!);
        var junctionId = new JunctionId(Guid.Parse(reader.ReadString()!));
        var reason = reader.ReadString();
        var tick = CanonicalTickHelper.Deserialize(ref reader);
        var sequence = reader.ReadInt64();
        var streamIdentity = StreamIdentityHelper.Deserialize(ref reader);
        var previousHash = HashHelper.Deserialize(ref reader);
        var hash = HashHelper.Deserialize(ref reader);

        return new JunctionRetiredEvent(eventId, junctionId, reason, tick, sequence, streamIdentity, previousHash, hash);
    }
}

#endregion

/// <summary>
/// MessagePack serializer for plate topology events with envelope-based polymorphic API.
///
/// Envelope format: [eventType:string, payload:binary]
/// - eventType: string name of event type (e.g., "PlateCreatedEvent")
/// - payload: binary encoded event data using event-specific numeric arrays
///
/// Payload format: numeric arrays only (no string keys/maps)
/// - Domain is serialized as plain string
/// - Geometry uses numeric discriminators: Point=[0,x,y], LineSegment=[1,x1,y1,x2,y2], Polyline=[2,x1,y1,x2,y2,...]
/// - CanonicalTick is encoded as raw Int64 per RFC-V2-0010 and RFC-V2-0005
///
/// API:
/// - Serialize<T>(T event): Writes [eventType, payload<T>]
/// - Deserialize(byte[] data): Reads envelope, dispatches to appropriate formatter
/// - Deserialize<T>(byte[] data): Reads envelope, asserts eventType matches, decodes payload
/// </summary>
public static class MessagePackEventSerializer
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                new GeometryFormatter(),
                new DomainFormatter(),
                new PlateCreatedEventFormatter(),
                new PlateRetiredEventFormatter(),
                new BoundaryCreatedEventFormatter(),
                new BoundaryTypeChangedEventFormatter(),
                new BoundaryGeometryUpdatedEventFormatter(),
                new BoundaryRetiredEventFormatter(),
                new JunctionCreatedEventFormatter(),
                new JunctionUpdatedEventFormatter(),
                new JunctionRetiredEventFormatter()
            }
        ));

    // Event type name to formatter mapping for polymorphic deserialization
    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        { nameof(PlateCreatedEvent), typeof(PlateCreatedEvent) },
        { nameof(PlateRetiredEvent), typeof(PlateRetiredEvent) },
        { nameof(BoundaryCreatedEvent), typeof(BoundaryCreatedEvent) },
        { nameof(BoundaryTypeChangedEvent), typeof(BoundaryTypeChangedEvent) },
        { nameof(BoundaryGeometryUpdatedEvent), typeof(BoundaryGeometryUpdatedEvent) },
        { nameof(BoundaryRetiredEvent), typeof(BoundaryRetiredEvent) },
        { nameof(JunctionCreatedEvent), typeof(JunctionCreatedEvent) },
        { nameof(JunctionUpdatedEvent), typeof(JunctionUpdatedEvent) },
        { nameof(JunctionRetiredEvent), typeof(JunctionRetiredEvent) }
    };

    /// <summary>
    /// Serializes an event to a MessagePack byte array with envelope.
    /// Format: [eventType:string, payload:binary]
    /// </summary>
    /// <typeparam name="T">The event type to serialize.</typeparam>
    /// <param name="value">The event to serialize.</param>
    /// <returns>A byte array containing [eventType, payload].</returns>
    public static byte[] Serialize<T>(T value) where T : IPlateTopologyEvent
    {
        var payloadBytes = MessagePackSerializer.Serialize(value, Options);

        // Serialize envelope directly using MessagePack
        // Envelope is: [eventType:string, payload:binary]
        var eventType = ((IPlateTopologyEvent)value).EventType;

        // Create envelope bytes manually
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(eventType);
        writer.Write(payloadBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    public static byte[] Serialize(IPlateTopologyEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var payloadBytes = MessagePackSerializer.Serialize(value.GetType(), value, Options);
        var eventType = value.EventType;

        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(2);
        writer.Write(eventType);
        writer.Write(payloadBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Deserializes an event from a MessagePack byte array.
    /// Reads envelope [eventType, payload] and dispatches to appropriate formatter.
    /// </summary>
    /// <param name="data">The byte array containing [eventType, payload].</param>
    /// <returns>The deserialized event as IPlateTopologyEvent.</returns>
    public static IPlateTopologyEvent Deserialize(byte[] data)
    {
        var reader = new MessagePackReader(data);

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Envelope must have 2 elements, got {length}");

        var eventType = reader.ReadString();
        if (eventType == null)
            throw new InvalidOperationException("Envelope eventType cannot be null");

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Envelope payload cannot be null");

        var payloadArray = payloadBytes.ToByteArray();

        // Use ReadOnlySequence directly for deserialization
        if (!EventTypeMap.TryGetValue(eventType, out var eventTypeType))
            throw new InvalidOperationException($"Unknown event type: {eventType}");

        var eventObj = MessagePackSerializer.Deserialize(eventTypeType, payloadArray, Options);
        return (IPlateTopologyEvent)eventObj!;
    }

    /// <summary>
    /// Deserializes an event from a MessagePack byte array to a specific concrete type.
    /// Reads envelope [eventType, payload], asserts discriminator matches T, then decodes payload.
    /// </summary>
    /// <typeparam name="T">The concrete event type expected.</typeparam>
    /// <param name="data">The byte array containing [eventType, payload].</param>
    /// <returns>The deserialized event.</returns>
    public static T Deserialize<T>(byte[] data) where T : IPlateTopologyEvent
    {
        var reader = new MessagePackReader(data);

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new InvalidOperationException($"Envelope must have 2 elements, got {length}");

        var eventType = reader.ReadString();
        if (eventType == null)
            throw new InvalidOperationException("Envelope eventType cannot be null");

        if (eventType != typeof(T).Name)
            throw new InvalidOperationException($"Event type mismatch: expected {typeof(T).Name}, got {eventType}");

        var payloadBytes = reader.ReadBytes();
        if (!payloadBytes.HasValue)
            throw new InvalidOperationException("Envelope payload cannot be null");

        var payloadArray = payloadBytes.ToByteArray();
        return MessagePackSerializer.Deserialize<T>(payloadArray, Options);
    }
}
