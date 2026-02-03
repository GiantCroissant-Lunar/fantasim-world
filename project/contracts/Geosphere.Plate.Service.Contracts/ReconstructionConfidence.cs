namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Confidence level for feature reconstruction.
/// </summary>
public enum ReconstructionConfidence
{
    /// <summary>
    /// High confidence reconstruction with minimal uncertainty.
    /// </summary>
    High = 0,

    /// <summary>
    /// Medium confidence with some uncertainty in rotation parameters.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Low confidence due to extrapolation or limited data.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Reconstruction involved interpolation between known time steps.
    /// </summary>
    Interpolated = 3
}
