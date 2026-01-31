namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Identifies the reference frame for motion path coordinates (RFC-V2-0049).
/// </summary>
public enum ReferenceFrameId
{
    /// <summary>
    /// Coordinates are relative to the deep mantle (absolute motion).
    /// </summary>
    Mantle = 0,

    /// <summary>
    /// Coordinates are relative to a specific plate (relative motion).
    /// </summary>
    FixedPlate = 1
}
