using System.Buffers.Binary;
using Plate.TimeDete.Determinism.Abstractions;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;

/// <summary>
/// Stable identifier representing a motion segment in plate kinematics truth.
/// </summary>
[MessagePackObject]
public readonly record struct MotionSegmentId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public MotionSegmentId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    /// <summary>
    /// Creates a new non-deterministic MotionSegmentId.
    /// </summary>
    /// <remarks>
    /// WARNING: This method uses Guid.NewGuid() which breaks determinism.
    /// Use <see cref="NewId(ISeededRng)"/> for reproducible simulation runs.
    /// </remarks>
    [Obsolete("Use NewId(ISeededRng) for deterministic simulation runs. This method breaks replay determinism.")]
    public static MotionSegmentId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a new deterministic MotionSegmentId using a seeded RNG.
    /// </summary>
    /// <param name="rng">The seeded RNG to use for ID generation.</param>
    /// <returns>A new MotionSegmentId with a deterministic value.</returns>
    public static MotionSegmentId NewId(ISeededRng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        var a = rng.NextUInt64();
        var b = rng.NextUInt64();
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[..8], a);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[8..], b);
        return new MotionSegmentId(new Guid(bytes));
    }

    public override string ToString() => _value.ToString("D");
}
