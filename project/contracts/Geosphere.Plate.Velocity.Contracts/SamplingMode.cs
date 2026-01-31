namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Sampling mode for boundary velocity analysis.
/// </summary>
public enum SamplingMode
{
    /// <summary>Sample along geodesic arc length on the sphere.</summary>
    ArcLength,

    /// <summary>Sample along straight chords between vertices.</summary>
    ChordLength,

    /// <summary>Sample at geometry vertices only (no interpolation).</summary>
    VertexOnly
}
