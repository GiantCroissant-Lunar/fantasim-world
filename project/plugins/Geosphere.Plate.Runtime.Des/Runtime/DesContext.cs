using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Plate.TimeDete.Determinism.Abstractions;
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

    /// <summary>
    /// Tick-scoped seeded RNG for deterministic ID generation.
    /// Each tick MUST receive a fresh RNG instance derived from the scenario seed
    /// and stream identity to ensure reproducibility across runs.
    /// </summary>
    public required ISeededRng Rng { get; init; }
}
