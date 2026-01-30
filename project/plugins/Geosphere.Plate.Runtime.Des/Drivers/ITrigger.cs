using System.Collections.Generic;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using Plate.TimeDete.Determinism.Abstractions;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Drivers;

public readonly record struct TriggerId(string Value);

public interface ITrigger
{
    TriggerId Id { get; }

    /// <summary>
    /// Returns 0..n truth event drafts for this tick.
    /// </summary>
    /// <param name="output">The driver output containing any signal data.</param>
    /// <param name="tick">The current canonical tick.</param>
    /// <param name="rng">Seeded RNG for deterministic ID generation. Callers MUST provide
    /// a tick-scoped RNG instance to ensure reproducibility.</param>
    /// <returns>A collection of truth event drafts to be committed.</returns>
    IReadOnlyList<ITruthEventDraft> EmitDrafts(DriverOutput output, CanonicalTick tick, ISeededRng rng);
}
