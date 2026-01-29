using System.Collections.Immutable;
using MessagePack;
using FantaSim.Geosphere.Plate.Junction.Contracts.Products;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Diagnostics;

/// <summary>
/// Complete diagnostic result for junction analysis (RFC-V2-0042 §7.2).
/// </summary>
[MessagePackObject]
public readonly record struct JunctionDiagnostics(
    /// <summary>The tick at which diagnostics were computed.</summary>
    [property: Key(0)] CanonicalTick Tick,

    /// <summary>Closure diagnostics for all junctions with available kinematics.</summary>
    [property: Key(1)] ImmutableArray<JunctionClosureDiagnostic> ClosureDiagnostics,

    /// <summary>Junctions that could not be processed (e.g., missing kinematics, invalid topology).</summary>
    [property: Key(2)] ImmutableArray<JunctionInfo> InvalidJunctions,

    /// <summary>Total number of junctions analyzed.</summary>
    [property: Key(3)] int TotalJunctions,

    /// <summary>Number of junctions with closed kinematics (residual below tolerance).</summary>
    [property: Key(4)] int ClosedJunctions,

    /// <summary>Number of junctions with unclosed kinematics (residual above tolerance).</summary>
    [property: Key(5)] int UnclosedJunctions
)
{
    /// <summary>
    /// Closure ratio: proportion of analyzed junctions that are kinematically closed.
    /// </summary>
    public double ClosureRatio => TotalJunctions > 0
        ? (double)ClosedJunctions / TotalJunctions
        : 1.0;

    /// <summary>
    /// True if all analyzed junctions are kinematically closed.
    /// </summary>
    public bool AllClosed => UnclosedJunctions == 0;
}
