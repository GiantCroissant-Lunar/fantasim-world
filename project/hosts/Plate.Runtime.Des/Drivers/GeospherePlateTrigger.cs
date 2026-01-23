using System;
using System.Collections.Generic;
using FantaSim.World.Contracts.Time;
using Plate.Runtime.Des.Events;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Identity;

namespace Plate.Runtime.Des.Drivers;

public sealed class GeospherePlateTrigger : ITrigger
{
    private readonly TruthStreamIdentity _streamIdentity;

    public GeospherePlateTrigger(TruthStreamIdentity streamIdentity)
    {
        _streamIdentity = streamIdentity;
    }

    public TriggerId Id => new("GeospherePlateTrigger");

    public IReadOnlyList<ITruthEventDraft> EmitDrafts(DriverOutput output, CanonicalTick tick)
    {
        if (output.Signal is string s && s == "Genesis")
        {
            // Emit PlateCreatedEvent for Plate "P:0" (using deterministic UUID)
            var plateId = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));

            // NOTE: Non-deterministic GUID for MVP. In production, use deterministic generator from context/seed.
            var eventId = Guid.NewGuid();

            var draft = new GenericTruthEventDraft(
                tick,
                _streamIdentity,
                (seq) => new PlateCreatedEvent(
                    eventId,
                    plateId,
                    tick,
                    seq,
                    _streamIdentity,
                    ReadOnlyMemory<byte>.Empty, // Filled by store
                    ReadOnlyMemory<byte>.Empty  // Filled by store
                )
            );

            return new[] { draft };
        }

        return Array.Empty<ITruthEventDraft>();
    }
}
