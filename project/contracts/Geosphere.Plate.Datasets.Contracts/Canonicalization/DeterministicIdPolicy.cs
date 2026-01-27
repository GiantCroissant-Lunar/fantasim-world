using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Canonicalization;

public static class DeterministicIdPolicy
{
    public static Guid DeriveEventId(string datasetId, string assetId, long sequence)
    {
        return DeriveStableGuid(datasetId, assetId, sequence.ToString(CultureInfo.InvariantCulture));
    }

    public static Guid DeriveStableId(string datasetId, string assetId, string kind, string key)
    {
        return DeriveStableGuid(datasetId, assetId, kind, key);
    }

    private static Guid DeriveStableGuid(params string[] parts)
    {
        if (parts.Length == 0)
            throw new ArgumentException("At least one part is required.", nameof(parts));

        foreach (var p in parts)
            ArgumentNullException.ThrowIfNull(p);

        var material = string.Join("\n", parts);
        var bytes = Encoding.UTF8.GetBytes(material);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(bytes, hash);

        Span<byte> rfcBytes = stackalloc byte[16];
        hash[..16].CopyTo(rfcBytes);

        rfcBytes[6] = (byte)((rfcBytes[6] & 0x0F) | 0x40);
        rfcBytes[8] = (byte)((rfcBytes[8] & 0x3F) | 0x80);

        Span<byte> guidBytes = stackalloc byte[16];
        guidBytes[0] = rfcBytes[3];
        guidBytes[1] = rfcBytes[2];
        guidBytes[2] = rfcBytes[1];
        guidBytes[3] = rfcBytes[0];
        guidBytes[4] = rfcBytes[5];
        guidBytes[5] = rfcBytes[4];
        guidBytes[6] = rfcBytes[7];
        guidBytes[7] = rfcBytes[6];
        rfcBytes[8..].CopyTo(guidBytes[8..]);

        return new Guid(guidBytes);
    }
}
