using MessagePack;
using System.Runtime.Serialization;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

/// <summary>
/// Uniquely identifies a kinematics model for rotation calculations.
/// </summary>
[MessagePackObject]
public readonly record struct ModelId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public ModelId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static readonly ModelId Default = new(Guid.Empty);

    public static ModelId NewId() => new(Guid.NewGuid());

    public static ModelId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ModelId cannot be null or whitespace.", nameof(value));
        return new ModelId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}
