using System;
using System.Buffers;
using System.Security.Cryptography;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Serializers;

public static class MessagePackEventRecordSerializer
{
    public const int SchemaVersionV1 = 1;
    public const int HashSizeBytes = 32;

    private static readonly byte[] ZeroHash = new byte[HashSizeBytes];

    public static byte[] SerializeRecord(
        int schemaVersion,
        long tick,
        byte[] previousHash,
        byte[] hash,
        byte[] eventBytes)
    {
        ArgumentNullException.ThrowIfNull(previousHash);
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(eventBytes);

        if (previousHash.Length != HashSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(previousHash), $"previousHash must be {HashSizeBytes} bytes");
        if (hash.Length != HashSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(hash), $"hash must be {HashSizeBytes} bytes");

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteArrayHeader(5);
        writer.Write(schemaVersion);
        writer.Write(tick);
        writer.Write(previousHash);
        writer.Write(hash);
        writer.Write(eventBytes);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    public static byte[] ComputeHashV1(
        int schemaVersion,
        long tick,
        byte[] previousHash,
        byte[] eventBytes)
    {
        ArgumentNullException.ThrowIfNull(previousHash);
        ArgumentNullException.ThrowIfNull(eventBytes);

        if (previousHash.Length != HashSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(previousHash), $"previousHash must be {HashSizeBytes} bytes");

        // Canonical preimage for v1 hashing: [schemaVersion, tick, previousHash, eventBytes]
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(4);
        writer.Write(schemaVersion);
        writer.Write(tick);
        writer.Write(previousHash);
        writer.Write(eventBytes);
        writer.Flush();

        return SHA256.HashData(buffer.WrittenSpan);
    }

    public static EventRecordV1 DeserializeRecord(byte[] recordBytes)
    {
        ArgumentNullException.ThrowIfNull(recordBytes);

        var reader = new MessagePackReader(recordBytes);
        var length = reader.ReadArrayHeader();
        if (length != 5)
            throw new InvalidOperationException($"EventRecord must have 5 elements, got {length}");

        var schemaVersion = reader.ReadInt32();
        var tick = reader.ReadInt64();

        var previousHash = ReadFixedHash(ref reader, "previousHash");
        var hash = ReadFixedHash(ref reader, "hash");

        var eventBytes = reader.ReadBytes();
        if (!eventBytes.HasValue)
            throw new InvalidOperationException("EventRecord eventBytes cannot be null");

        return new EventRecordV1(schemaVersion, tick, previousHash, hash, eventBytes.Value.ToArray());
    }

    public static bool TryDeserializeRecord(byte[] recordBytes, out EventRecordV1 record)
    {
        ArgumentNullException.ThrowIfNull(recordBytes);

        try
        {
            var reader = new MessagePackReader(recordBytes);
            var length = reader.ReadArrayHeader();
            if (length != 5)
            {
                record = default;
                return false;
            }

            record = DeserializeRecord(recordBytes);
            return true;
        }
        catch
        {
            record = default;
            return false;
        }
    }

    public static byte[] GetZeroHash() => (byte[])ZeroHash.Clone();

    private static byte[] ReadFixedHash(ref MessagePackReader reader, string name)
    {
        var bytes = reader.ReadBytes();
        if (!bytes.HasValue)
            throw new InvalidOperationException($"EventRecord {name} cannot be null");

        var arr = bytes.Value.ToArray();
        if (arr.Length != HashSizeBytes)
            throw new InvalidOperationException($"EventRecord {name} must be {HashSizeBytes} bytes, got {arr.Length}");

        return arr;
    }

    public readonly record struct EventRecordV1(
        int SchemaVersion,
        long Tick,
        byte[] PreviousHash,
        byte[] Hash,
        byte[] EventBytes);
}
