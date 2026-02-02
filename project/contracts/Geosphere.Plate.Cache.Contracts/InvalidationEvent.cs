namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public enum InvalidationReason
{
    TopologyChanged,
    KinematicsChanged,
    Manual
}

public readonly record struct InvalidationEvent(
    InvalidationReason Reason,
    string? TopologyStreamHash,
    string? KinematicsModelId);
