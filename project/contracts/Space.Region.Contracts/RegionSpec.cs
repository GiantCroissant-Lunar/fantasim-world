using MessagePack;

namespace FantaSim.Space.Region.Contracts;

/// <summary>
/// RegionSpec fully defines "what subset of world space are we querying?"
/// Per RFC-V2-0055 §3.1.
/// </summary>
[MessagePackObject]
public record RegionSpec
{
    /// <summary>
    /// Schema version. Must be 1 for RFC-V2-0055.
    /// </summary>
    [Key(0)]
    public required int Version { get; init; }

    /// <summary>
    /// Canonical coordinate space. Always "canonical_sphere" (RFC-034) in v1.
    /// </summary>
    [Key(1)]
    public required string Space { get; init; }

    /// <summary>
    /// The geometric shape of the region.
    /// </summary>
    [Key(2)]
    public required RegionShape Shape { get; init; }

    /// <summary>
    /// Coordinate anchor and basis for interpreting the shape.
    /// </summary>
    [Key(3)]
    public required RegionFrame Frame { get; init; }

    /// <summary>
    /// Optional discretization policy for derived products.
    /// When null, the generator chooses a default.
    /// See RFC-V2-0055a for indexing guidance.
    /// </summary>
    [Key(4)]
    public RegionSampling? Sampling { get; init; }

    /// <summary>
    /// Creates a SurfaceShell region (planet surface).
    /// </summary>
    /// <param name="thicknessM">Shell thickness in meters. 0.0 means ideal surface.</param>
    public static RegionSpec Surface(double thicknessM = 0.0) => new()
    {
        Version = 1,
        Space = "canonical_sphere",
        Shape = RegionShape.SurfaceShellShape(thicknessM),
        Frame = RegionFrame.PlanetCenter(),
        Sampling = null
    };

    /// <summary>
    /// Creates a SphericalShell region (radial band).
    /// </summary>
    /// <param name="rMinM">Inner radius in meters from planet center.</param>
    /// <param name="rMaxM">Outer radius in meters from planet center.</param>
    /// <param name="angularClip">Optional angular clip to restrict to a portion of the shell.</param>
    public static RegionSpec SphericalShell(double rMinM, double rMaxM, AngularClip? angularClip = null) => new()
    {
        Version = 1,
        Space = "canonical_sphere",
        Shape = RegionShape.SphericalShellShape(rMinM, rMaxM, angularClip),
        Frame = RegionFrame.PlanetCenter(),
        Sampling = null
    };
}
