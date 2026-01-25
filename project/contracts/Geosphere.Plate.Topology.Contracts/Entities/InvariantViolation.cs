namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

public readonly record struct InvariantViolation(
    string Invariant,
    string Message,
    long? Sequence
);
