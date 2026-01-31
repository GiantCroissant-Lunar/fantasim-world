using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Uncertainty bounds for rate calculations (RFC-V2-0048 §4.6).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct RateUncertainty(
    [property: Key(0)] double NormalRateSigma,
    [property: Key(1)] double TangentialRateSigma,
    [property: Key(2)] double SpeedSigma,
    [property: Key(3)] double NormalRateConfidence95Lower,
    [property: Key(4)] double NormalRateConfidence95Upper,
    [property: Key(5)] double TangentialRateConfidence95Lower,
    [property: Key(6)] double TangentialRateConfidence95Upper
)
{
    /// <summary>
    /// Returns the 95% confidence interval for normal rate as a tuple.
    /// </summary>
    [IgnoreMember]
    public (double Lower, double Upper) NormalRateConfidence95 => (NormalRateConfidence95Lower, NormalRateConfidence95Upper);

    /// <summary>
    /// Returns the 95% confidence interval for tangential rate as a tuple.
    /// </summary>
    [IgnoreMember]
    public (double Lower, double Upper) TangentialRateConfidence95 => (TangentialRateConfidence95Lower, TangentialRateConfidence95Upper);

    /// <summary>
    /// Creates uncertainty from standard deviations (assumes normal distribution).
    /// </summary>
    public static RateUncertainty FromSigmas(double normalSigma, double tangentialSigma, double speedSigma)
    {
        const double z95 = 1.96;
        return new RateUncertainty(
            NormalRateSigma: normalSigma,
            TangentialRateSigma: tangentialSigma,
            SpeedSigma: speedSigma,
            NormalRateConfidence95Lower: -z95 * normalSigma,
            NormalRateConfidence95Upper: z95 * normalSigma,
            TangentialRateConfidence95Lower: -z95 * tangentialSigma,
            TangentialRateConfidence95Upper: z95 * tangentialSigma
        );
    }
}
