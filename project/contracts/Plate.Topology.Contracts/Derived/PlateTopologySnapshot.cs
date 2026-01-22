using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Identity;
using PlateEntity = Plate.Topology.Contracts.Entities.Plate;

namespace Plate.Topology.Contracts.Derived;

public readonly record struct PlateTopologySnapshot(
    PlateTopologyMaterializationKey Key,
    long LastEventSequence,
    PlateEntity[] Plates,
    Boundary[] Boundaries,
    Junction[] Junctions);
