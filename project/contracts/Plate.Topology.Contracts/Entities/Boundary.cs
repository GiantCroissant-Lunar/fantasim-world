using UnifyGeometry;

namespace Plate.Topology.Contracts.Entities;

public readonly record struct Boundary(
    BoundaryId BoundaryId,
    PlateId PlateIdLeft,
    PlateId PlateIdRight,
    BoundaryType BoundaryType,
    IGeometry Geometry,
    bool IsRetired,
    string? RetirementReason
);
