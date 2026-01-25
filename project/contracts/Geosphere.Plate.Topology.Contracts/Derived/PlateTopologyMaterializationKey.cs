using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

public readonly record struct PlateTopologyMaterializationKey(
    TruthStreamIdentity Stream,
    long Tick);
