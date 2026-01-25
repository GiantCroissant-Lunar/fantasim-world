using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using BoundaryEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

public interface IPlateTopologyStateView
{
    TruthStreamIdentity Identity { get; }

    IReadOnlyDictionary<PlateId, PlateEntity> Plates { get; }

    IReadOnlyDictionary<BoundaryId, BoundaryEntity> Boundaries { get; }

    IReadOnlyDictionary<JunctionId, JunctionEntity> Junctions { get; }

    long LastEventSequence { get; }
}
