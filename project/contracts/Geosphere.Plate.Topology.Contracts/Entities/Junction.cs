using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

/// <summary>
/// A junction where boundaries meet on a spherical body surface.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sphere-by-default:</b> Location is stored as a <see cref="SurfacePoint"/>
/// (unit surface normal + radius), not as 2D planar coordinates. This ensures
/// correctness anywhere on the sphere, including poles and antimeridian.
/// </para>
/// <para>
/// BoundaryIds are stored as <see cref="ImmutableArray{T}"/> for determinism
/// and immutability. Arrays compare by reference which causes subtle bugs.
/// </para>
/// </remarks>
public readonly record struct Junction(
    JunctionId JunctionId,
    ImmutableArray<BoundaryId> BoundaryIds,
    SurfacePoint Location,
    bool IsRetired,
    string? RetirementReason
);
