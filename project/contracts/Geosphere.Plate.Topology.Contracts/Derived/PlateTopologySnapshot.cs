using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

public readonly record struct PlateTopologySnapshot(
    PlateTopologyMaterializationKey Key,
    long LastEventSequence,
    PlateEntity[] Plates,
    Boundary[] Boundaries,
    Junction[] Junctions);
