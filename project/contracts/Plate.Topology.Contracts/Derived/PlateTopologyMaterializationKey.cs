using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Derived;

public readonly record struct PlateTopologyMaterializationKey(
    TruthStreamIdentity Stream,
    long Tick);
