namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Sense of strike-slip motion (RFC-V2-0048 §5.4).
/// </summary>
public enum StrikeSlipSense
{
    /// <summary>Right-lateral strike-slip motion.</summary>
    RightLateral,

    /// <summary>Left-lateral strike-slip motion.</summary>
    LeftLateral,

    /// <summary>No strike-slip motion (purely convergent/divergent).</summary>
    None
}
