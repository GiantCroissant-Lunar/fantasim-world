namespace FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities;

/// <summary>
/// Parsed representation of stream capabilities stored in Meta:Caps.
///
/// Storage format (9 bytes):
/// - Byte 0: Schema version (currently 0x01)
/// - Bytes 1-8: Flags as UInt64 little-endian
///
/// Flag bits:
/// - Bit 0: TickMonotoneFromGenesis
/// - Bit 1: TickPolicyRejectFromGenesis
/// - Bits 2-63: Reserved (must be 0)
/// </summary>
public readonly struct StreamCapabilities : IEquatable<StreamCapabilities>
{
    /// <summary>
    /// Current schema version for capabilities storage.
    /// </summary>
    public const byte CurrentSchemaVersion = 0x01;

    /// <summary>
    /// Storage size in bytes: 1 (version) + 8 (flags).
    /// </summary>
    public const int StorageSize = 9;

    // Flag bit positions
    private const int BitTickMonotoneFromGenesis = 0;
    private const int BitTickPolicyRejectFromGenesis = 1;

    private readonly ulong _flags;

    /// <summary>
    /// Creates capabilities with specified flags.
    /// </summary>
    public StreamCapabilities(ulong flags)
    {
        _flags = flags;
    }

    /// <summary>
    /// Empty capabilities (no flags set).
    /// </summary>
    public static StreamCapabilities None => new(0);

    /// <summary>
    /// Creates capabilities for a genesis stream with Reject tick policy.
    /// Sets both TickMonotoneFromGenesis and TickPolicyRejectFromGenesis.
    /// </summary>
    public static StreamCapabilities GenesisWithRejectPolicy =>
        new((1UL << BitTickMonotoneFromGenesis) | (1UL << BitTickPolicyRejectFromGenesis));

    /// <summary>
    /// True if the stream has proven tick monotonicity from genesis.
    /// </summary>
    public bool IsTickMonotoneFromGenesis => (_flags & (1UL << BitTickMonotoneFromGenesis)) != 0;

    /// <summary>
    /// True if the stream was created with TickMonotonicityPolicy.Reject.
    /// </summary>
    public bool IsTickPolicyRejectFromGenesis => (_flags & (1UL << BitTickPolicyRejectFromGenesis)) != 0;

    /// <summary>
    /// Raw flags value.
    /// </summary>
    public ulong Flags => _flags;

    /// <summary>
    /// Serializes capabilities to bytes.
    /// </summary>
    /// <returns>9-byte array: [version, flags(8 bytes LE)]</returns>
    public byte[] ToBytes()
    {
        var bytes = new byte[StorageSize];
        bytes[0] = CurrentSchemaVersion;
        BitConverter.TryWriteBytes(bytes.AsSpan(1), _flags); // Little-endian on most platforms
        if (!BitConverter.IsLittleEndian)
        {
            // Ensure little-endian
            Array.Reverse(bytes, 1, 8);
        }
        return bytes;
    }

    /// <summary>
    /// Deserializes capabilities from bytes.
    /// </summary>
    /// <param name="bytes">At least 9 bytes.</param>
    /// <returns>Parsed capabilities, or None if invalid/unsupported.</returns>
    public static StreamCapabilities FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < StorageSize)
            return None;

        var version = bytes[0];
        if (version != CurrentSchemaVersion)
            return None; // Unknown version - treat as no capabilities

        ulong flags;
        if (BitConverter.IsLittleEndian)
        {
            flags = BitConverter.ToUInt64(bytes.Slice(1, 8));
        }
        else
        {
            // Convert from little-endian
            Span<byte> temp = stackalloc byte[8];
            bytes.Slice(1, 8).CopyTo(temp);
            temp.Reverse();
            flags = BitConverter.ToUInt64(temp);
        }

        return new StreamCapabilities(flags);
    }

    /// <summary>
    /// Returns a new capabilities with TickMonotoneFromGenesis set.
    /// </summary>
    public StreamCapabilities WithTickMonotoneFromGenesis() =>
        new(_flags | (1UL << BitTickMonotoneFromGenesis));

    /// <summary>
    /// Returns a new capabilities with TickPolicyRejectFromGenesis set.
    /// </summary>
    public StreamCapabilities WithTickPolicyRejectFromGenesis() =>
        new(_flags | (1UL << BitTickPolicyRejectFromGenesis));

    public bool Equals(StreamCapabilities other) => _flags == other._flags;
    public override bool Equals(object? obj) => obj is StreamCapabilities other && Equals(other);
    public override int GetHashCode() => _flags.GetHashCode();
    public static bool operator ==(StreamCapabilities left, StreamCapabilities right) => left.Equals(right);
    public static bool operator !=(StreamCapabilities left, StreamCapabilities right) => !left.Equals(right);

    public override string ToString() =>
        $"StreamCapabilities(Monotone={IsTickMonotoneFromGenesis}, RejectPolicy={IsTickPolicyRejectFromGenesis})";
}
