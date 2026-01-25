using Plate.TimeDete.Determinism.Abstractions;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

/// <summary>
/// Default implementation of IDeterministicIdFactory using UUIDv7-style format.
///
/// IDs are generated using the RNG stream for the random portion, while maintaining
/// UUIDv7 structure for sortability.
///
/// NOTE: This class should be registered with your DI container and injected where needed.
/// Avoid static singletons at plugin/service boundaries.
/// </summary>
public sealed class DeterministicIdFactory : IDeterministicIdFactory
{
    /// <inheritdoc />
    public PlateId NewPlateId(ISeededRng rng) => PlateId.NewId(rng);

    /// <inheritdoc />
    public BoundaryId NewBoundaryId(ISeededRng rng) => BoundaryId.NewId(rng);

    /// <inheritdoc />
    public JunctionId NewJunctionId(ISeededRng rng) => JunctionId.NewId(rng);

    /// <inheritdoc />
    public EventId NewEventId(ISeededRng rng) => EventId.NewId(rng);
}
