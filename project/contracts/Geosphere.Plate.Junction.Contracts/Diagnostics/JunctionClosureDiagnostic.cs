using System.Collections.Immutable;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Diagnostics;

/// <summary>
/// Kinematic closure diagnostic for a junction (RFC-V2-0042 §7.1).
/// </summary>
/// <remarks>
/// <para>
/// At a triple junction where plates A, B, C meet, the relative velocities must close:
/// <code>v_AB + v_BC + v_CA = 0</code>
/// </para>
/// <para>
/// The <see cref="Residual"/> is the magnitude of the closure error.
/// Non-zero residual indicates inconsistent rotation poles or topology/kinematics mismatch.
/// </para>
/// </remarks>
[MessagePackObject]
public readonly record struct JunctionClosureDiagnostic(
    /// <summary>The junction being diagnosed.</summary>
    [property: Key(0)] JunctionId JunctionId,

    /// <summary>Junction position in body frame coordinates.</summary>
    [property: Key(1)] Point3 Position,

    /// <summary>The closure residual vector (should be near zero for consistent kinematics).</summary>
    [property: Key(2)] Velocity3d Residual,

    /// <summary>Magnitude of the residual vector.</summary>
    [property: Key(3)] double ResidualMagnitude,

    /// <summary>Relative velocities between incident plates (for debugging).</summary>
    [property: Key(4)] ImmutableArray<PlateRelativeVelocity> RelativeVelocities,

    /// <summary>True if residual is below the closure tolerance.</summary>
    [property: Key(5)] bool IsClosed
);
