using Plate.TimeDete.Determinism.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Stable identifier representing an event in the topology stream per FR-006.
///
/// EventIds use UUIDv7 format for sortability. Solvers SHOULD use NewId(ISeededRng)
/// to ensure deterministic replay per RFC-099 guidance.
/// </summary>
public readonly record struct EventId
{
    /// <summary>
    /// Internal UUID representation.
    /// </summary>
    private readonly Guid _value;

    /// <summary>
    /// Initializes a new instance of the EventId struct with the specified UUID value.
    /// </summary>
    /// <param name="value">The UUID value.</param>
    public EventId(Guid value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the underlying UUID value.
    /// </summary>
    public Guid Value => _value;

    /// <summary>
    /// Gets a value indicating whether this EventId is empty/invalid.
    /// </summary>
    public bool IsEmpty => _value == Guid.Empty;

    /// <summary>
    /// Creates a new unique EventId using a time-sorted UUIDv7.
    ///
    /// WARNING: This overload uses system time and cryptographic RNG, producing
    /// non-deterministic IDs. Use NewId(ISeededRng) in solver implementations
    /// for replay determinism.
    /// </summary>
    /// <returns>A new unique EventId.</returns>
    public static EventId NewId()
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

        return new EventId(new Guid(guidBytes));
    }

    /// <summary>
    /// Creates a new unique EventId deterministically using a seeded RNG.
    /// Use this overload in solver implementations to ensure replay determinism per RFC-099.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    /// <returns>A new deterministic EventId.</returns>
    public static EventId NewId(ISeededRng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return new EventId(GenerateDeterministicGuid(rng));
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
    /// Creates an EventId from a raw GUID value.
    /// </summary>
    public static EventId FromGuid(Guid value) => new(value);

    /// <summary>
    /// Parses an EventId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the UUID.</param>
    /// <returns>An EventId instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid UUID.</exception>
    public static EventId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("EventId value cannot be null or whitespace.", nameof(value));

        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid EventId format: {value}", nameof(value));

        if (guid == Guid.Empty)
            throw new ArgumentException("EventId value cannot be Guid.Empty.", nameof(value));

        return new EventId(guid);
    }

    /// <summary>
    /// Tries to parse an EventId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the UUID.</param>
    /// <param name="eventId">The parsed EventId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string value, out EventId eventId)
    {
        if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            eventId = new EventId(guid);
            return true;
        }

        eventId = default;
        return false;
    }

    /// <summary>
    /// Implicit conversion from EventId to Guid for backward compatibility.
    /// </summary>
    public static implicit operator Guid(EventId eventId) => eventId._value;

    /// <summary>
    /// Explicit conversion from Guid to EventId.
    /// </summary>
    public static explicit operator EventId(Guid value) => new(value);

    /// <summary>
    /// Returns a string representation of the EventId.
    /// </summary>
    /// <returns>A formatted UUID string.</returns>
    public override string ToString()
    {
        return _value.ToString("D"); // D format: 32 digits with hyphens
    }
}
