namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Confidence level for velocity calculation.
/// </summary>
public enum VelocityConfidence
{
    /// <summary>
    /// High confidence with minimal uncertainty.
    /// </summary>
    High = 0,

    /// <summary>
    /// Medium confidence with some uncertainty.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Low confidence due to boundary proximity or data limitations.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Extrapolated velocity beyond known time range.
    /// </summary>
    Extrapolated = 3
}
