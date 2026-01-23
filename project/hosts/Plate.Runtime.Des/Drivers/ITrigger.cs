using System.Collections.Generic;
using Plate.Runtime.Des.Events;

using Plate.TimeDete.Time.Primitives;

namespace Plate.Runtime.Des.Drivers;

public readonly record struct TriggerId(string Value);

public interface ITrigger
{
    TriggerId Id { get; }

    // Returns 0..n drafts for this tick.
    IReadOnlyList<ITruthEventDraft> EmitDrafts(DriverOutput output, CanonicalTick tick);
}
