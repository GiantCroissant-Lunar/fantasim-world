using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;
using Plate.TimeDete.Time.Primitives;

namespace Plate.Runtime.Des.Runtime;

public sealed class DesContext
{
    public required TruthStreamIdentity Stream { get; init; }
    public required CanonicalTick CurrentTick { get; init; }

    // Read-only world access
    public required IPlateTopologyIndexedStateView State { get; init; }

    // Helper hooks
    public required IDesScheduler Scheduler { get; init; }
}
