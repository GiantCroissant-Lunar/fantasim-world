using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Products;

/// <summary>
/// A boundary incident at a junction (RFC-V2-0042 §6.1).
/// </summary>
/// <remarks>
/// <para>
/// Represents a directed boundary segment terminating at a junction point.
/// The angle field establishes deterministic cyclic ordering around the junction.
/// </para>
/// <para>
/// <b>Sphere-correct:</b> Angle is measured in the local tangent plane at the junction,
/// CCW from North (not from global +X). This is computed using a <see cref="Frame3"/>
/// constructed at the junction's surface point.
/// </para>
/// </remarks>
[MessagePackObject]
public readonly record struct JunctionIncident(
    /// <summary>The boundary terminating at this junction.</summary>
    [property: Key(0)] BoundaryId BoundaryId,

    /// <summary>True if this is the boundary's start point; false if endpoint.</summary>
    [property: Key(1)] bool IsStartpoint,

    /// <summary>
    /// Angle in the local tangent plane at the junction (radians, CCW from North).
    /// </summary>
    /// <remarks>
    /// This angle is computed by projecting the boundary direction onto the tangent plane
    /// at the junction's surface point and measuring from North. It is stable across
    /// the entire sphere, unlike global angles which break at the poles.
    /// </remarks>
    [property: Key(2)] double TangentAngle,

    /// <summary>Plate on the left side of this incident (looking along boundary direction).</summary>
    [property: Key(3)] PlateId LeftPlateId,

    /// <summary>Plate on the right side of this incident (looking along boundary direction).</summary>
    [property: Key(4)] PlateId RightPlateId
)
{
    /// <summary>
    /// Computes the tangent angle for a boundary direction at a junction.
    /// </summary>
    /// <param name="junctionPosition">The surface point where the junction is located.</param>
    /// <param name="boundaryDirection">The 3D direction vector along the boundary (outward from junction).</param>
    /// <returns>Angle in radians, CCW from North in the tangent plane.</returns>
    public static double ComputeTangentAngle(SurfacePoint junctionPosition, Vector3d boundaryDirection)
    {
        // Create local tangent frame at the junction
        var frame = junctionPosition.CreateTangentFrame();

        // Project boundary direction onto tangent plane and compute angle
        return frame.ComputeTangentAngle(boundaryDirection);
    }

    /// <summary>
    /// Compares two incidents by their tangent angle for deterministic ordering.
    /// </summary>
    public static int CompareByAngle(JunctionIncident a, JunctionIncident b)
    {
        var angleCmp = a.TangentAngle.CompareTo(b.TangentAngle);
        return angleCmp != 0 ? angleCmp : a.BoundaryId.Value.CompareTo(b.BoundaryId.Value);
    }
}
