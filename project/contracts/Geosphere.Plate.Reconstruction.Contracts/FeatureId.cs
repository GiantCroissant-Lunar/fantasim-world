using MessagePack;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

[MessagePackObject]
public readonly record struct FeatureId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public FeatureId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static FeatureId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FeatureId value cannot be null or whitespace.", nameof(value));

        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid FeatureId format: {value}", nameof(value));

        if (guid == Guid.Empty)
            throw new ArgumentException("FeatureId value cannot be Guid.Empty.", nameof(value));

        return new FeatureId(guid);
    }

    public static bool TryParse(string value, out FeatureId featureId)
    {
        if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            featureId = new FeatureId(guid);
            return true;
        }

        featureId = default;
        return false;
    }

    public override string ToString() => _value.ToString("D");
}
