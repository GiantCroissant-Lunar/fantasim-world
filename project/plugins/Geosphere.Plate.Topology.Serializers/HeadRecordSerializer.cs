using System.Buffers;
using System.Buffers.Binary;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Serializers;

/// <summary>
/// Serializes/deserializes stream head metadata per RFC-V2-0004.
///
/// Head record format (MessagePack array):
/// [lastSeq (int64), lastHash (bin32), lastTick (int64)]
///
/// This replaces the previous MVP format that only stored lastSeq (8 bytes).
/// The new format enables fast head reads without touching event records,
/// and supports full optimistic concurrency checks.
/// </summary>
public static class HeadRecordSerializer
{
    public const int HashSizeBytes = 32;

    /// <summary>
    /// Serializes a head record to MessagePack bytes.
    /// </summary>
    /// <param name="lastSeq">The last sequence number in the stream.</param>
    /// <param name="lastHash">The hash of the last event record (32 bytes).</param>
    /// <param name="lastTick">The tick of the last event.</param>
    /// <returns>MessagePack-encoded head record.</returns>
    public static byte[] Serialize(long lastSeq, byte[] lastHash, long lastTick)
    {
        ArgumentNullException.ThrowIfNull(lastHash);
        if (lastHash.Length != HashSizeBytes)
            throw new ArgumentException($"Hash must be {HashSizeBytes} bytes", nameof(lastHash));

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteArrayHeader(3);
        writer.Write(lastSeq);
        writer.Write(lastHash);
        writer.Write(lastTick);
        writer.Flush();

        return buffer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Deserializes a head record from MessagePack bytes.
    /// </summary>
    /// <param name="data">The MessagePack-encoded head record.</param>
    /// <param name="lastSeq">The last sequence number.</param>
    /// <param name="lastHash">The hash of the last event record.</param>
    /// <param name="lastTick">The tick of the last event.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out long lastSeq, out byte[] lastHash, out long lastTick)
    {
        lastSeq = -1;
        lastHash = Array.Empty<byte>();
        lastTick = -1;

        try
        {
            var reader = new MessagePackReader(data.ToArray());
            var arrayLength = reader.ReadArrayHeader();

            if (arrayLength != 3)
                return false;

            lastSeq = reader.ReadInt64();
            var hashBytes = reader.ReadBytes();
            if (!hashBytes.HasValue || hashBytes.Value.Length != HashSizeBytes)
                return false;

            lastHash = hashBytes.Value.ToArray();
            lastTick = reader.ReadInt64();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the data looks like an old-format head (8-byte sequence only).
    /// Used for migration/compatibility detection.
    /// </summary>
    public static bool IsLegacyFormat(ReadOnlySpan<byte> data)
    {
        return data.Length == 8;
    }

    /// <summary>
    /// Reads a legacy 8-byte head value (sequence only).
    /// </summary>
    public static long ReadLegacySequence(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new ArgumentException("Legacy head must be exactly 8 bytes", nameof(data));
        return BinaryPrimitives.ReadInt64BigEndian(data);
    }
}
