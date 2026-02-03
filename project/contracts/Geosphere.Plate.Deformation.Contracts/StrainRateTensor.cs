using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Deformation.Contracts;

[MessagePackObject]
public record StrainRateTensor
{
    /// <summary>
    /// East-East component (εₑₑ): rate of stretching in the east direction.
    /// Units: per canonical tick.
    /// </summary>
    [Key(0)]
    public required double Eee { get; init; }

    /// <summary>
    /// North-North component (εₙₙ): rate of stretching in the north direction.
    /// Units: per canonical tick.
    /// </summary>
    [Key(1)]
    public required double Enn { get; init; }

    /// <summary>
    /// East-North component (εₑₙ = εₙₑ): shear strain rate.
    /// Units: per canonical tick.
    /// </summary>
    [Key(2)]
    public required double Een { get; init; }

    /// <summary>
    /// Dilatation rate: trace of the tensor (εₑₑ + εₙₙ).
    /// Positive = extension, negative = compression.
    /// </summary>
    [IgnoreMember]
    public double DilatationRate => Eee + Enn;

    /// <summary>
    /// Second invariant: √(0.5 * (εₑₑ² + εₙₙ² + 2·εₑₙ²)).
    /// Always non-negative. Measures total deformation intensity.
    /// </summary>
    [IgnoreMember]
    public double SecondInvariant =>
        Math.Sqrt(0.5 * (Eee * Eee + Enn * Enn + 2.0 * Een * Een));
}
