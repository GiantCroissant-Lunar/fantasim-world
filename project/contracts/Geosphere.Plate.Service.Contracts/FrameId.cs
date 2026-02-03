using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Frame identifier for velocity reference frames.
/// </summary>
[MessagePackObject]
public readonly record struct FrameId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public FrameId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static FrameId NewId() => new(Guid.NewGuid());

    public static FrameId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FrameId cannot be null or whitespace.", nameof(value));
        return new FrameId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}
