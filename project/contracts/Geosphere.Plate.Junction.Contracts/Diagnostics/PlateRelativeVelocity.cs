using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Diagnostics;

/// <summary>
/// Relative velocity between two plates at a junction point (RFC-V2-0042 §7.1).
/// </summary>
[MessagePackObject]
public readonly record struct PlateRelativeVelocity(
    /// <summary>The "from" plate in the relative velocity calculation.</summary>
    [property: Key(0)] PlateId FromPlate,

    /// <summary>The "to" plate in the relative velocity calculation.</summary>
    [property: Key(1)] PlateId ToPlate,

    /// <summary>Velocity of ToPlate relative to FromPlate at the junction point.</summary>
    [property: Key(2)] Velocity3d Velocity
);
