using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Feature set identifier for batch reconstruction operations.
/// </summary>
[MessagePackObject]
public readonly record struct FeatureSetId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public FeatureSetId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static FeatureSetId NewId() => new(Guid.NewGuid());

    public static FeatureSetId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FeatureSetId cannot be null or whitespace.", nameof(value));
        return new FeatureSetId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}
