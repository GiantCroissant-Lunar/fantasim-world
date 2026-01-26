using System.Runtime.InteropServices;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Mechanics;

[UnifyModel]
[StructLayout(LayoutKind.Auto)]
public readonly record struct OrbitalElements(
    [property: UnifyProperty(0)] double SemiMajorAxisM,
    [property: UnifyProperty(1)] double Eccentricity,
    [property: UnifyProperty(2)] double InclinationRad,
    [property: UnifyProperty(3)] double LongitudeOfAscendingNodeRad,
    [property: UnifyProperty(4)] double ArgumentOfPeriapsisRad,
    [property: UnifyProperty(5)] double MeanAnomalyAtEpochRad,
    [property: UnifyProperty(6)] double EpochTimeS
)
{
    /// <summary>Validate orbital elements are physically meaningful.</summary>
    public bool IsValid()
    {
        return SemiMajorAxisM > 0
            && Eccentricity >= 0 && Eccentricity < 1
            && InclinationRad >= 0 && InclinationRad <= Math.PI;
    }
}
