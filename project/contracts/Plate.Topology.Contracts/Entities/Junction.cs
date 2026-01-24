using UnifyGeometry;

namespace Plate.Topology.Contracts.Entities;

public readonly record struct Junction(
    JunctionId JunctionId,
    BoundaryId[] BoundaryIds,
    Point2 Location,
    bool IsRetired,
    string? RetirementReason
);
