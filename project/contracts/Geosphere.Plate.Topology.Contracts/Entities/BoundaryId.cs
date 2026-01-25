using Plate.TimeDete.Determinism.Abstractions;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

/// <summary>
/// Stable identifier representing a boundary segment/edge between plates per FR-005.
///
/// Each boundary has a type classification and geometric representation.
/// Boundaries separate exactly two plates except for explicitly modeled world edges.
/// Once created, a BoundaryId persists through all topology changes and is never reused
/// even after retirement.
/// </summary>
[MessagePackObject]
public readonly record struct BoundaryId
{
    /// <summary>
    /// Internal UUID representation.
    /// </summary>
    private readonly Guid _value;

    /// <summary>
    /// Initializes a new instance of the BoundaryId struct with the specified UUID value.
    /// </summary>
    /// <param name="value">The UUID value.</param>
    [SerializationConstructor]
    public BoundaryId(Guid value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the underlying UUID value.
    /// </summary>
    [Key(0)]
    public Guid Value => _value;

    /// <summary>
    /// Gets a value indicating whether this BoundaryId is empty/invalid.
    /// </summary>
    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    /// <summary>
    /// Creates a new unique BoundaryId using a time-sorted UUIDv7.
    /// </summary>
    /// <returns>A new unique BoundaryId.</returns>
    public static BoundaryId NewId()
    {
        // UUIDv7 layout per RFC 9562:
        // 0-5: 48-bit Unix timestamp in milliseconds (big-endian)
        // 6-7: 16 bits with version (0b0111xxxx xxxxxxxx) and randomness
        // 8-15: 64 bits of randomness with RFC4122 variant (0b10xxxxxx on byte 8)

        var rfcBytes = new byte[16];

        // Timestamp (48 bits, big-endian)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        rfcBytes[0] = (byte)((timestamp >> 40) & 0xFF);
        rfcBytes[1] = (byte)((timestamp >> 32) & 0xFF);
        rfcBytes[2] = (byte)((timestamp >> 24) & 0xFF);
        rfcBytes[3] = (byte)((timestamp >> 16) & 0xFF);
        rfcBytes[4] = (byte)((timestamp >> 8) & 0xFF);
        rfcBytes[5] = (byte)(timestamp & 0xFF);

        // Random bytes (10 bytes total, 2 for version field + 8 for rest)
        var randBytes = new byte[10];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randBytes);
        rfcBytes[6] = randBytes[0];
        rfcBytes[7] = randBytes[1];
        Buffer.BlockCopy(randBytes, 2, rfcBytes, 8, 8);

        // Set version to 7 (bits 4-7 of byte 6: 0b0111xxxx)
        rfcBytes[6] = (byte)((rfcBytes[6] & 0x0F) | 0x70);

        // Set RFC4122 variant (bits 6-7 of byte 8: 0b10xxxxxx)
        rfcBytes[8] = (byte)((rfcBytes[8] & 0x3F) | 0x80);

        // Convert RFC4122 byte order to .NET Guid mixed-endian format:
        // Guid: Data1 (4, LE) | Data2 (2, LE) | Data3 (2, LE) | Data4 (8, BE)
        // RFC:  [0-3] (BE)    | [4-5] (BE)    | [6-7] (BE)    | [8-15] (BE)

        var guidBytes = new byte[16];
        guidBytes[0] = rfcBytes[3];
        guidBytes[1] = rfcBytes[2];
        guidBytes[2] = rfcBytes[1];
        guidBytes[3] = rfcBytes[0];
        guidBytes[4] = rfcBytes[5];
        guidBytes[5] = rfcBytes[4];
        guidBytes[6] = rfcBytes[7];
        guidBytes[7] = rfcBytes[6];
        Buffer.BlockCopy(rfcBytes, 8, guidBytes, 8, 8);

        return new BoundaryId(new Guid(guidBytes));
    }

    /// <summary>
    /// Creates a new unique BoundaryId deterministically using a seeded RNG.
    /// Use this overload in solver implementations to ensure replay determinism.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    /// <returns>A new deterministic BoundaryId.</returns>
    public static BoundaryId NewId(ISeededRng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return new BoundaryId(GenerateDeterministicGuid(rng));
    }

    /// <summary>
    /// Generates a UUIDv7-style GUID deterministically using the provided RNG.
    /// Uses RNG for all bits (timestamp + random), maintaining UUIDv7 structure.
    /// </summary>
    private static Guid GenerateDeterministicGuid(ISeededRng rng)
    {
        var rfcBytes = new byte[16];

        // Use RNG for all bytes (deterministic pseudo-timestamp + random)
        var highBits = rng.NextUInt64();
        var lowBits = rng.NextUInt64();

        // Fill bytes from RNG
        rfcBytes[0] = (byte)((highBits >> 40) & 0xFF);
        rfcBytes[1] = (byte)((highBits >> 32) & 0xFF);
        rfcBytes[2] = (byte)((highBits >> 24) & 0xFF);
        rfcBytes[3] = (byte)((highBits >> 16) & 0xFF);
        rfcBytes[4] = (byte)((highBits >> 8) & 0xFF);
        rfcBytes[5] = (byte)(highBits & 0xFF);
        rfcBytes[6] = (byte)((lowBits >> 56) & 0xFF);
        rfcBytes[7] = (byte)((lowBits >> 48) & 0xFF);
        rfcBytes[8] = (byte)((lowBits >> 40) & 0xFF);
        rfcBytes[9] = (byte)((lowBits >> 32) & 0xFF);
        rfcBytes[10] = (byte)((lowBits >> 24) & 0xFF);
        rfcBytes[11] = (byte)((lowBits >> 16) & 0xFF);
        rfcBytes[12] = (byte)((lowBits >> 8) & 0xFF);
        rfcBytes[13] = (byte)(lowBits & 0xFF);
        rfcBytes[14] = (byte)((highBits >> 56) & 0xFF);
        rfcBytes[15] = (byte)((highBits >> 48) & 0xFF);

        // Set version to 7 (bits 4-7 of byte 6: 0b0111xxxx)
        rfcBytes[6] = (byte)((rfcBytes[6] & 0x0F) | 0x70);

        // Set RFC4122 variant (bits 6-7 of byte 8: 0b10xxxxxx)
        rfcBytes[8] = (byte)((rfcBytes[8] & 0x3F) | 0x80);

        // Convert RFC4122 byte order to .NET Guid mixed-endian format
        var guidBytes = new byte[16];
        guidBytes[0] = rfcBytes[3];
        guidBytes[1] = rfcBytes[2];
        guidBytes[2] = rfcBytes[1];
        guidBytes[3] = rfcBytes[0];
        guidBytes[4] = rfcBytes[5];
        guidBytes[5] = rfcBytes[4];
        guidBytes[6] = rfcBytes[7];
        guidBytes[7] = rfcBytes[6];
        Buffer.BlockCopy(rfcBytes, 8, guidBytes, 8, 8);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Parses a BoundaryId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the UUID.</param>
    /// <returns>A BoundaryId instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid UUID.</exception>
    public static BoundaryId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("BoundaryId value cannot be null or whitespace.", nameof(value));

        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid BoundaryId format: {value}", nameof(value));

        if (guid == Guid.Empty)
            throw new ArgumentException("BoundaryId value cannot be Guid.Empty.", nameof(value));

        return new BoundaryId(guid);
    }

    /// <summary>
    /// Tries to parse a BoundaryId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the UUID.</param>
    /// <param name="boundaryId">The parsed BoundaryId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string value, out BoundaryId boundaryId)
    {
        if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            boundaryId = new BoundaryId(guid);
            return true;
        }

        boundaryId = default;
        return false;
    }

    /// <summary>
    /// Returns a string representation of the BoundaryId.
    /// </summary>
    /// <returns>A formatted UUID string.</returns>
    public override string ToString()
    {
        return _value.ToString("D"); // D format: 32 digits with hyphens
    }
}
