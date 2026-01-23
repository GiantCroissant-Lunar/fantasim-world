using Plate.Topology.Contracts.Identity;
using Plate.Topology.Contracts.Events;
using Plate.TimeDete.Time.Primitives;

namespace Plate.Runtime.Des.Events;

public interface ITruthEventDraft
{
    CanonicalTick Tick { get; }
    TruthStreamIdentity Stream { get; }

    // Converts to a concrete truth event type (e.g., PlateCreatedEvent).
    // Must NOT set Hash/PreviousHash; store will do that.
    // The sequence is assigned by the appender.
    IPlateTopologyEvent ToTruthEvent(long sequence);
}
