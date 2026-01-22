using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using BoundaryEntity = Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = Plate.Topology.Contracts.Entities.Junction;
using PlateEntity = Plate.Topology.Contracts.Entities.Plate;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

public sealed class PlateTopologyState : IPlateTopologyStateView
{
    public PlateTopologyState(TruthStreamIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
        LastEventSequence = -1;
    }

    public TruthStreamIdentity Identity { get; }

    public Dictionary<PlateId, PlateEntity> Plates { get; } = new();

    public Dictionary<BoundaryId, BoundaryEntity> Boundaries { get; } = new();

    public Dictionary<JunctionId, JunctionEntity> Junctions { get; } = new();

    public long LastEventSequence { get; private set; }

    public List<InvariantViolation> Violations { get; } = new();

    internal void SetLastEventSequence(long sequence)
    {
        LastEventSequence = sequence;
    }

    TruthStreamIdentity IPlateTopologyStateView.Identity => Identity;

    IReadOnlyDictionary<PlateId, PlateEntity> IPlateTopologyStateView.Plates => Plates;

    IReadOnlyDictionary<BoundaryId, BoundaryEntity> IPlateTopologyStateView.Boundaries => Boundaries;

    IReadOnlyDictionary<JunctionId, JunctionEntity> IPlateTopologyStateView.Junctions => Junctions;

    long IPlateTopologyStateView.LastEventSequence => LastEventSequence;
}
