using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public sealed class DesContext
{
    public required TruthStreamIdentity Stream { get; init; }
    public required CanonicalTick CurrentTick { get; init; }

    // Read-only world access
    public required IPlateTopologyIndexedStateView State { get; init; }

    // Helper hooks
    public required IDesScheduler Scheduler { get; init; }
}
