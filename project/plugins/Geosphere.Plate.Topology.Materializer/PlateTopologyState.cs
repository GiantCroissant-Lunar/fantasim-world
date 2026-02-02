using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using BoundaryEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

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

    public IDictionary<PlateId, PlateEntity> Plates { get; } = new Dictionary<PlateId, PlateEntity>();

    public IDictionary<BoundaryId, BoundaryEntity> Boundaries { get; } = new Dictionary<BoundaryId, BoundaryEntity>();

    public IDictionary<JunctionId, JunctionEntity> Junctions { get; } = new Dictionary<JunctionId, JunctionEntity>();

    public long LastEventSequence { get; private set; }

    public PlateTopologyIndices Indices { get; private set; }

    public IList<InvariantViolation> Violations { get; } = new List<InvariantViolation>();

    internal void SetLastEventSequence(long sequence)
    {
        LastEventSequence = sequence;
    }

    internal void RebuildIndices()
    {
        Indices = PlateTopologyIndicesBuilder.BuildPlateAdjacency(this);
    }

    TruthStreamIdentity IPlateTopologyStateView.Identity => Identity;

    IReadOnlyDictionary<PlateId, PlateEntity> IPlateTopologyStateView.Plates => (IReadOnlyDictionary<PlateId, PlateEntity>)Plates;

    IReadOnlyDictionary<BoundaryId, BoundaryEntity> IPlateTopologyStateView.Boundaries => (IReadOnlyDictionary<BoundaryId, BoundaryEntity>)Boundaries;

    IReadOnlyDictionary<JunctionId, JunctionEntity> IPlateTopologyStateView.Junctions => (IReadOnlyDictionary<JunctionId, JunctionEntity>)Junctions;

    long IPlateTopologyStateView.LastEventSequence => LastEventSequence;
}
