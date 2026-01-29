using System.Buffers;
using System.Text;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

/// <summary>
/// Canonical MessagePack encoder for deterministic hashing.
///
/// Key features:
/// - Map keys are sorted by UTF-8 byte order
/// - Produces identical bytes for equivalent data structures
/// - Used for computing content-addressed hashes
/// </summary>
public static class CanonicalMessagePackEncoder
{
    /// <summary>
    /// Encodes a dictionary as canonical MessagePack map with sorted keys.
    /// </summary>
    /// <param name="map">The dictionary to encode</param>
    /// <returns>Canonical MessagePack bytes</returns>
    public static byte[] EncodeMap(Dictionary<string, object?>? map)
    {
        if (map == null || map.Count == 0)
        {
            // Empty map: 0x80 (fixmap with 0 elements)
            return new byte[] { 0x80 };
        }

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        // Sort keys by UTF-8 byte order
        var sortedEntries = map
            .Select(e => new { KeyBytes = Encoding.UTF8.GetBytes(e.Key), e.Key, e.Value })
            .OrderBy(e => e.KeyBytes, ByteArrayComparer.Instance)
            .ToList();

        writer.WriteMapHeader(sortedEntries.Count);

        foreach (var entry in sortedEntries)
        {
            // Write key as raw UTF-8 bytes
            writer.WriteString(entry.KeyBytes);
            // Write value
            WriteValue(ref writer, entry.Value);
        }

        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Encodes a FingerprintEnvelope as canonical MessagePack array.
    /// Array encoding preserves field order (no sorting needed for arrays).
    /// </summary>
    public static byte[] EncodeFingerprintEnvelope(
        string sourceStream,
        string boundaryKind,
        ulong lastSequence,
        string generatorId,
        string generatorVersion,
        string paramsHash)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        // Write as 6-element fixarray: 0x96
        writer.WriteArrayHeader(6);

        // Position 0: source_stream (string)
        writer.Write(sourceStream);

        // Position 1: boundary_kind (string)
        writer.Write(boundaryKind);

        // Position 2: last_sequence (uint64)
        writer.Write(lastSequence);

        // Position 3: generator_id (string)
        writer.Write(generatorId);

        // Position 4: generator_version (string)
        writer.Write(generatorVersion);

        // Position 5: params_hash (string)
        writer.Write(paramsHash);

        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Writes a value to the MessagePack writer.
    /// Supports: null, bool, string, long, ulong, double, Dictionary<string, object>
    /// </summary>
    private static void WriteValue(ref MessagePackWriter writer, object? value)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var valueType = value.GetType();

        if (value is bool b)
        {
            writer.Write(b);
        }
        else if (value is string s)
        {
            writer.Write(s);
        }
        else if (value is long l)
        {
            writer.Write(l);
        }
        else if (value is int i)
        {
            writer.Write((long)i);
        }
        else if (value is ulong ul)
        {
            writer.Write(ul);
        }
        else if (value is uint ui)
        {
            writer.Write((ulong)ui);
        }
        else if (value is double d)
        {
            writer.Write(d);
        }
        else if (value is float f)
        {
            writer.Write((double)f);
        }
        else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                 valueType.GetGenericArguments()[0] == typeof(string))
        {
            // Handle Dictionary<string, object?> and Dictionary<string, object>
            var dict = (System.Collections.IDictionary)value;
            var converted = new Dictionary<string, object?>();
            foreach (var key in dict.Keys)
            {
                converted[(string)key] = dict[key];
            }
            var encoded = EncodeMap(converted);
            writer.WriteRaw(encoded);
        }
        else
        {
            // Fallback: convert to string
            writer.Write(value.ToString());
        }
    }

    /// <summary>
    /// Comparer for sorting byte arrays by ordinal byte order.
    /// </summary>
    private class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int minLen = Math.Min(x.Length, y.Length);
            for (int i = 0; i < minLen; i++)
            {
                int cmp = x[i].CompareTo(y[i]);
                if (cmp != 0) return cmp;
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}
