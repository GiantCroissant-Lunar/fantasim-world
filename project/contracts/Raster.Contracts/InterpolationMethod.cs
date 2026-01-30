namespace FantaSim.Raster.Contracts;

/// <summary>
/// Interpolation method for deriving raster values between frames.
/// RFC-V2-0028 §3.1.
/// </summary>
public enum InterpolationMethod
{
    /// <summary>
    /// Nearest neighbor - use the closest frame without interpolation.
    /// Default behavior per RFC-V2-0028.
    /// </summary>
    NearestNeighbor,
    
    /// <summary>
    /// Linear interpolation between two frames.
    /// </summary>
    Linear,
    
    /// <summary>
    /// Cubic interpolation for smoother transitions.
    /// </summary>
    Cubic
}
