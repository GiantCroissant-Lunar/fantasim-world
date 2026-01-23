using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using BoundaryEntity = Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = Plate.Topology.Contracts.Entities.Junction;
using PlateEntity = Plate.Topology.Contracts.Entities.Plate;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

public sealed class PlateTopologyState : IPlateTopologyIndexedStateView
{
    public PlateTopologyState(TruthStreamIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
        LastEventSequence = -1;
        Indices = PlateTopologyIndicesBuilder.BuildPlateAdjacency(this);
    }

    public TruthStreamIdentity Identity { get; }

    public Dictionary<PlateId, PlateEntity> Plates { get; } = new();

    public Dictionary<BoundaryId, BoundaryEntity> Boundaries { get; } = new();

    public Dictionary<JunctionId, JunctionEntity> Junctions { get; } = new();

    public long LastEventSequence { get; private set; }

    public PlateTopologyIndices Indices { get; private set; }

    public List<InvariantViolation> Violations { get; } = new();

    internal void SetLastEventSequence(long sequence)
    {
        LastEventSequence = sequence;
    }

    internal void RebuildIndices()
    {
        Indices = PlateTopologyIndicesBuilder.BuildPlateAdjacency(this);
    }

    TruthStreamIdentity IPlateTopologyStateView.Identity => Identity;

    IReadOnlyDictionary<PlateId, PlateEntity> IPlateTopologyStateView.Plates => Plates;

    IReadOnlyDictionary<BoundaryId, BoundaryEntity> IPlateTopologyStateView.Boundaries => Boundaries;

    IReadOnlyDictionary<JunctionId, JunctionEntity> IPlateTopologyStateView.Junctions => Junctions;

    long IPlateTopologyStateView.LastEventSequence => LastEventSequence;
}
