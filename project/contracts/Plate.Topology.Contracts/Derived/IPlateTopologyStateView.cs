using PlateEntity = Plate.Topology.Contracts.Entities.Plate;
using BoundaryEntity = Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = Plate.Topology.Contracts.Entities.Junction;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Derived;

public interface IPlateTopologyStateView
{
    TruthStreamIdentity Identity { get; }

    IReadOnlyDictionary<PlateId, PlateEntity> Plates { get; }

    IReadOnlyDictionary<BoundaryId, BoundaryEntity> Boundaries { get; }

    IReadOnlyDictionary<JunctionId, JunctionEntity> Junctions { get; }

    long LastEventSequence { get; }
}
