using Plate.Topology.Contracts.Geometry;

namespace Plate.Topology.Contracts.Entities;

public readonly record struct Junction(
    JunctionId JunctionId,
    BoundaryId[] BoundaryIds,
    Point2D Location,
    bool IsRetired,
    string? RetirementReason
);
