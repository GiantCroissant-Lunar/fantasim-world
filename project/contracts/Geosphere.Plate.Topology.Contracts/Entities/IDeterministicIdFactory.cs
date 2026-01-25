using Plate.TimeDete.Determinism.Abstractions;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

/// <summary>
/// Factory interface for deterministic ID generation per RFC-V2-0001 and RFC-099 guidance.
///
/// Solver implementations SHOULD use this interface instead of calling NewId() directly
/// to ensure replay determinism. IDs are derived from the provided RNG stream,
/// producing identical IDs on identical replays given the same seed.
///
/// Implementations should be obtained via dependency injection, not static singletons,
/// to maintain plugin boundary hygiene.
/// </summary>
public interface IDeterministicIdFactory
{
    /// <summary>
    /// Creates a new PlateId deterministically using the provided RNG.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    PlateId NewPlateId(ISeededRng rng);

    /// <summary>
    /// Creates a new BoundaryId deterministically using the provided RNG.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    BoundaryId NewBoundaryId(ISeededRng rng);

    /// <summary>
    /// Creates a new JunctionId deterministically using the provided RNG.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    JunctionId NewJunctionId(ISeededRng rng);

    /// <summary>
    /// Creates a new EventId deterministically using the provided RNG.
    /// Essential for RFC-099 solver determinism: event IDs must be reproducible.
    /// </summary>
    /// <param name="rng">The seeded RNG instance for deterministic generation.</param>
    EventId NewEventId(ISeededRng rng);
}
