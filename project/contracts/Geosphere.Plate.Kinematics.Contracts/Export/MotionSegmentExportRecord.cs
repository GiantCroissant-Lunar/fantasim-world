using MessagePack;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Export;

/// <summary>
/// Flattened record for Parquet export of motion segment data.
/// </summary>
[UnifyModel]
public sealed class MotionSegmentExportRecord
{
    public Guid EventId { get; init; }
    public string PlateId { get; init; } = string.Empty;
    public Guid SegmentId { get; init; }
    public long TickA { get; init; }
    public long TickB { get; init; }
    public double RotationAngleDegrees { get; init; }
    public double PoleLatitude { get; init; }
    public double PoleLongitude { get; init; }
    public long Tick { get; init; }
    public long Sequence { get; init; }
    public string StreamIdentity { get; init; } = string.Empty;
    public byte[] PreviousHash { get; init; } = [];
    public byte[] Hash { get; init; } = [];

    [SerializationConstructor]
    public MotionSegmentExportRecord(
        Guid eventId,
        string plateId,
        Guid segmentId,
        long tickA,
        long tickB,
        double rotationAngleDegrees,
        double poleLatitude,
        double poleLongitude,
        long tick,
        long sequence,
        string streamIdentity,
        byte[] previousHash,
        byte[] hash)
    {
        EventId = eventId;
        PlateId = plateId;
        SegmentId = segmentId;
        TickA = tickA;
        TickB = tickB;
        RotationAngleDegrees = rotationAngleDegrees;
        PoleLatitude = poleLatitude;
        PoleLongitude = poleLongitude;
        Tick = tick;
        Sequence = sequence;
        StreamIdentity = streamIdentity;
        PreviousHash = previousHash;
        Hash = hash;
    }

    public MotionSegmentExportRecord() { }
}
