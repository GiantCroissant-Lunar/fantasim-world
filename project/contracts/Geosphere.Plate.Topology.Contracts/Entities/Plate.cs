namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

public readonly record struct Plate(
    PlateId PlateId,
    bool IsRetired,
    string? RetirementReason
);
