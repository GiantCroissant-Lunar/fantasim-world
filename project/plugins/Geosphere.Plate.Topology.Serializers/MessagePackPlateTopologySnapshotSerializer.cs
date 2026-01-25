using System;
using System.Buffers;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;
using UnifyGeometry;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Topology.Serializers;

public static class MessagePackPlateTopologySnapshotSerializer
{
    public const int SchemaVersionV1 = 1;

    public static byte[] Serialize(PlateTopologySnapshot snapshot)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteArrayHeader(7);
        writer.Write(SchemaVersionV1);

        // Use shared Options to serialize TruthStreamIdentity
        MessagePackSerializer.Serialize(ref writer, snapshot.Key.Stream, MessagePackEventSerializer.Options);

        writer.Write(snapshot.Key.Tick);
        writer.Write(snapshot.LastEventSequence);

        SerializePlates(ref writer, snapshot.Plates);
        SerializeBoundaries(ref writer, snapshot.Boundaries);
        SerializeJunctions(ref writer, snapshot.Junctions);

        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    public static PlateTopologySnapshot Deserialize(byte[] bytes)
    {
        var reader = new MessagePackReader(bytes);
        var length = reader.ReadArrayHeader();
        if (length != 7)
            throw new InvalidOperationException($"PlateTopologySnapshot must have 7 elements, got {length}");

        var schemaVersion = reader.ReadInt32();
        if (schemaVersion != SchemaVersionV1)
            throw new InvalidOperationException($"Unsupported snapshot schemaVersion: {schemaVersion}");

        // Use shared Options to deserialize TruthStreamIdentity
        var stream = MessagePackSerializer.Deserialize<FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentity>(ref reader, MessagePackEventSerializer.Options);

        var tick = reader.ReadInt64();
        var lastEventSequence = reader.ReadInt64();

        var plates = DeserializePlates(ref reader);
        var boundaries = DeserializeBoundaries(ref reader);
        var junctions = DeserializeJunctions(ref reader);

        return new PlateTopologySnapshot(
            new PlateTopologyMaterializationKey(stream, tick),
            lastEventSequence,
            plates,
            boundaries,
            junctions);
    }

    private static void SerializePlates(ref MessagePackWriter writer, PlateEntity[] plates)
    {
        writer.WriteArrayHeader(plates.Length);
        foreach (var p in plates)
        {
            writer.WriteArrayHeader(3);
            writer.Write(p.PlateId.Value.ToString());
            writer.Write(p.IsRetired);
            if (p.RetirementReason is null)
                writer.WriteNil();
            else
                writer.Write(p.RetirementReason);
        }
    }

    private static PlateEntity[] DeserializePlates(ref MessagePackReader reader)
    {
        var count = reader.ReadArrayHeader();
        var plates = new PlateEntity[count];
        for (var i = 0; i < count; i++)
        {
            var len = reader.ReadArrayHeader();
            if (len != 3)
                throw new InvalidOperationException($"Plate must have 3 elements, got {len}");

            var id = Guid.Parse(reader.ReadString()!);
            var isRetired = reader.ReadBoolean();
            string? reason;
            if (reader.TryReadNil())
                reason = null;
            else
                reason = reader.ReadString();

            plates[i] = new PlateEntity(new PlateId(id), isRetired, reason);
        }

        return plates;
    }

    private static void SerializeBoundaries(ref MessagePackWriter writer, Boundary[] boundaries)
    {
        writer.WriteArrayHeader(boundaries.Length);
        foreach (var b in boundaries)
        {
            writer.WriteArrayHeader(7);
            writer.Write(b.BoundaryId.Value.ToString());
            writer.Write(b.PlateIdLeft.Value.ToString());
            writer.Write(b.PlateIdRight.Value.ToString());
            writer.Write((byte)b.BoundaryType);

            // Use shared Options for Geometry
            MessagePackSerializer.Serialize(ref writer, b.Geometry, MessagePackEventSerializer.Options);

            writer.Write(b.IsRetired);
            if (b.RetirementReason is null)
                writer.WriteNil();
            else
                writer.Write(b.RetirementReason);
        }
    }

    private static Boundary[] DeserializeBoundaries(ref MessagePackReader reader)
    {
        var count = reader.ReadArrayHeader();
        var boundaries = new Boundary[count];

        for (var i = 0; i < count; i++)
        {
            var len = reader.ReadArrayHeader();
            if (len != 7)
                throw new InvalidOperationException($"Boundary must have 7 elements, got {len}");

            var boundaryId = new BoundaryId(Guid.Parse(reader.ReadString()!));
            var plateLeft = new PlateId(Guid.Parse(reader.ReadString()!));
            var plateRight = new PlateId(Guid.Parse(reader.ReadString()!));
            var type = (BoundaryType)reader.ReadByte();

            // Use shared Options for Geometry
            var geometry = MessagePackSerializer.Deserialize<IGeometry?>(ref reader, MessagePackEventSerializer.Options);
            if (geometry == null) throw new InvalidOperationException("Boundary geometry cannot be null");

            var isRetired = reader.ReadBoolean();

            string? reason;
            if (reader.TryReadNil())
                reason = null;
            else
                reason = reader.ReadString();

            boundaries[i] = new Boundary(boundaryId, plateLeft, plateRight, type, geometry, isRetired, reason);
        }

        return boundaries;
    }

    private static void SerializeJunctions(ref MessagePackWriter writer, Junction[] junctions)
    {
        writer.WriteArrayHeader(junctions.Length);
        foreach (var j in junctions)
        {
            writer.WriteArrayHeader(6);
            writer.Write(j.JunctionId.Value.ToString());

            writer.WriteArrayHeader(j.BoundaryIds.Length);
            foreach (var b in j.BoundaryIds)
            {
                writer.Write(b.Value.ToString());
            }

            writer.Write(j.Location.X);
            writer.Write(j.Location.Y);
            writer.Write(j.IsRetired);

            if (j.RetirementReason is null)
                writer.WriteNil();
            else
                writer.Write(j.RetirementReason);
        }
    }

    private static Junction[] DeserializeJunctions(ref MessagePackReader reader)
    {
        var count = reader.ReadArrayHeader();
        var junctions = new Junction[count];

        for (var i = 0; i < count; i++)
        {
            var len = reader.ReadArrayHeader();
            if (len != 6)
                throw new InvalidOperationException($"Junction must have 6 elements, got {len}");

            var junctionId = new JunctionId(Guid.Parse(reader.ReadString()!));

            var bCount = reader.ReadArrayHeader();
            var boundaryIds = new BoundaryId[bCount];
            for (var b = 0; b < bCount; b++)
            {
                boundaryIds[b] = new BoundaryId(Guid.Parse(reader.ReadString()!));
            }

            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var isRetired = reader.ReadBoolean();

            string? reason;
            if (reader.TryReadNil())
                reason = null;
            else
                reason = reader.ReadString();

            junctions[i] = new Junction(junctionId, boundaryIds, new(x, y), isRetired, reason);
        }

        return junctions;
    }
}
