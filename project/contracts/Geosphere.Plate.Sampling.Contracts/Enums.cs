using MessagePack;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

public enum SamplingDomainType
{
    /// <summary>Regular lat/lon grid.</summary>
    Regular,

    /// <summary>Equal-area tessellation (e.g., Healpix, icosahedral).</summary>
    EqualArea,

    /// <summary>Explicit user-supplied point set.</summary>
    Explicit,

    /// <summary>Points along plate boundaries (boundary-following).</summary>
    BoundaryFollowing
}

public enum GridRegistration
{
    /// <summary>Nodes on exact lat/lon gridlines (GMT "gridline").</summary>
    Gridline,

    /// <summary>Nodes at cell centers, offset by half-resolution (GMT "pixel").</summary>
    Pixel
}

public enum InterpolationMethod
{
    /// <summary>Nearest-neighbour: value from closest grid node.</summary>
    NearestNeighbour,

    /// <summary>Bilinear: weighted average of 4 surrounding nodes.</summary>
    Bilinear,

    /// <summary>No interpolation: only exact grid node values.</summary>
    None
}

public enum ScalarFieldId
{
    Age,
    SpeedMagnitude,
    PlateId,
    // Add others as needed
}

public enum VectorFieldId
{
    Velocity,
    // Add others as needed
}
