using System;
using System.Collections.Generic;
using FantaSim.World.Contracts.Time;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Determinism.Abstractions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Drivers;

public sealed class GeospherePlateTrigger : ITrigger
{
    private readonly TruthStreamIdentity _streamIdentity;

    public GeospherePlateTrigger(TruthStreamIdentity streamIdentity)
    {
        _streamIdentity = streamIdentity;
    }

    public TriggerId Id => new("GeospherePlateTrigger");

    public IReadOnlyList<ITruthEventDraft> EmitDrafts(DriverOutput output, CanonicalTick tick, ISeededRng rng)
    {
        if (output.Signal is string s && s == "Genesis")
        {
            // Emit PlateCreatedEvent for Plate "P:0" (using deterministic UUID)
            var plateId = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));

            // Generate deterministic event ID from seeded RNG for reproducibility
            var eventId = EventId.NewId(rng);

            var draft = new GenericTruthEventDraft(
                tick,
                _streamIdentity,
                (seq) => new PlateCreatedEvent(
                    eventId.Value,
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
