using System;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Events;

public class GenericTruthEventDraft : ITruthEventDraft
{
    private readonly Func<long, IPlateTopologyEvent> _factory;

    public GenericTruthEventDraft(
        CanonicalTick tick,
        TruthStreamIdentity stream,
        Func<long, IPlateTopologyEvent> factory)
    {
        Tick = tick;
        Stream = stream;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public CanonicalTick Tick { get; }
    public TruthStreamIdentity Stream { get; }

    public IPlateTopologyEvent ToTruthEvent(long sequence)
    {
        return _factory(sequence);
    }
}
