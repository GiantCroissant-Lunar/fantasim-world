using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Complete rate profile for a boundary with aggregate statistics (RFC-V2-0048 §5.6).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryRateProfile(
    [property: Key(0)] BoundaryId BoundaryId,
    [property: Key(1)] BoundaryType Type,
    [property: Key(2)] CanonicalTick Tick,
    [property: Key(3)] ReferenceFrameId Frame,
    [property: Key(4)] ImmutableArray<BoundaryRateSample> Samples,
    [property: Key(5)] RateStatistics Statistics,
    [property: Key(6)] ConvergenceRateSummary? ConvergenceSummary,
    [property: Key(7)] SpreadingRateMetrics? SpreadingMetrics,
    [property: Key(8)] StrikeSlipSummary? StrikeSlipSummary
)
{
    /// <summary>
    /// Returns true if this profile represents a convergent boundary.
    /// </summary>
    [IgnoreMember]
    public bool IsConvergent => Type == BoundaryType.Convergent;

    /// <summary>
    /// Returns true if this profile represents a divergent boundary.
    /// </summary>
    [IgnoreMember]
    public bool IsDivergent => Type == BoundaryType.Divergent;

    /// <summary>
    /// Returns true if this profile represents a transform boundary.
    /// </summary>
    [IgnoreMember]
    public bool IsTransform => Type == BoundaryType.Transform;

    /// <summary>
    /// Gets the maximum convergence rate (always non-negative).
    /// </summary>
    [IgnoreMember]
    public double MaxConvergenceRate => ConvergenceSummary?.MaxConvergenceRate ?? 0.0;

    /// <summary>
    /// Gets the mean divergence rate (always non-negative).
    /// </summary>
    [IgnoreMember]
    public double MeanDivergenceRate => SpreadingMetrics?.HalfRate ?? 0.0;

    /// <summary>
    /// Gets the full spreading rate (2x divergence rate for ridges).
    /// </summary>
    [IgnoreMember]
    public double FullSpreadingRate => SpreadingMetrics?.FullRate ?? 0.0;
}
