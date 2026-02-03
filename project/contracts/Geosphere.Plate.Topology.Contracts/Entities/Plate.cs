using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

/// <summary>
/// A tectonic plate entity.
/// </summary>
[UnifyModel]
public readonly record struct Plate(
    PlateId PlateId,
    bool IsRetired,
    string? RetirementReason
);
