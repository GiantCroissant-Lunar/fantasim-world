using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Boundary-local relative velocity decomposition for measuring convergence/divergence rates,
/// spreading rates, and other plate boundary analytics (RFC-V2-0048).
/// </summary>
/// <remarks>
/// <para>
/// This service extends the capabilities of <see cref="IBoundaryVelocitySolver"/> by providing
/// comprehensive rate analytics including uncertainty bounds, type-specific metrics,
/// and deterministic sampling policies.
/// </para>
/// <para>
/// <b>RFC-V2-0048 Compliance:</b>
/// - Rates are derived from kinematics + topology + sampling policy
/// - Normal vector convention: points from left plate → right plate
/// - Deterministic outputs for identical inputs
/// - Error bounds are first-class output fields
/// </para>
/// </remarks>
public interface IBoundaryAnalytics
{
    /// <summary>
    /// Samples boundary velocities at specified points along a boundary segment (RFC-V2-0048 §3.2).
    /// </summary>
    /// <param name="boundaryId">The boundary to sample.</param>
    /// <param name="tick">Target simulation time.</param>
    /// <param name="sampling">Sampling specification.</param>
    /// <param name="frame">Optional reference frame for velocity computation.</param>
    /// <returns>Rate profile with per-sample decomposition and aggregates.</returns>
    ValueTask<BoundaryRateProfile> SampleBoundaryVelocitiesAsync(
        BoundaryId boundaryId,
        CanonicalTick tick,
        BoundarySamplingSpec sampling,
        string? frame = null);

    /// <summary>
    /// Samples multiple boundaries at the specified tick.
    /// </summary>
    /// <param name="boundaryIds">The boundaries to sample.</param>
    /// <param name="tick">Target simulation time.</param>
    /// <param name="sampling">Sampling specification.</param>
    /// <param name="frame">Optional reference frame for velocity computation.</param>
    /// <returns>Collection of rate profiles sorted by BoundaryId.</returns>
    ValueTask<IReadOnlyList<BoundaryRateProfile>> SampleBoundariesAsync(
        IEnumerable<BoundaryId> boundaryIds,
        CanonicalTick tick,
        BoundarySamplingSpec sampling,
        string? frame = null);

    /// <summary>
    /// Computes spreading metrics for a ridge boundary (RFC-V2-0048 §5.3).
    /// </summary>
    /// <param name="profile">The rate profile from a divergent boundary.</param>
    /// <returns>Spreading rate metrics including asymmetry and obliquity.</returns>
    SpreadingRateMetrics ComputeSpreadingMetrics(BoundaryRateProfile profile);

    /// <summary>
    /// Gets convergence rate summary for a convergent boundary (RFC-V2-0048 §5.1).
    /// </summary>
    /// <param name="profile">The rate profile from a convergent boundary.</param>
    /// <returns>Convergence rate summary statistics.</returns>
    ConvergenceRateSummary GetConvergenceRateSummary(BoundaryRateProfile profile);

    /// <summary>
    /// Gets strike-slip summary for a transform boundary (RFC-V2-0048 §5.4).
    /// </summary>
    /// <param name="profile">The rate profile from a transform boundary.</param>
    /// <returns>Strike-slip summary including sense classification.</returns>
    StrikeSlipSummary GetStrikeSlipSummary(BoundaryRateProfile profile);

    /// <summary>
    /// Determines the strike-slip sense from the tangential rate.
    /// </summary>
    /// <param name="tangentialRate">The tangential rate component.</param>
    /// <returns>The sense of strike-slip motion.</returns>
    StrikeSlipSense GetStrikeSlipSense(double tangentialRate);

    /// <summary>
    /// Computes the convergence rate from the normal rate (RFC-V2-0048 §5.1).
    /// </summary>
    /// <param name="normalRate">The normal rate component.</param>
    /// <returns>Convergence rate (always non-negative).</returns>
    double GetConvergenceRate(double normalRate);

    /// <summary>
    /// Computes the divergence rate from the normal rate (RFC-V2-0048 §5.2).
    /// </summary>
    /// <param name="normalRate">The normal rate component.</param>
    /// <returns>Divergence rate (always non-negative).</returns>
    double GetDivergenceRate(double normalRate);

    /// <summary>
    /// Computes the strike-slip rate from the tangential rate (RFC-V2-0048 §5.4).
    /// </summary>
    /// <param name="tangentialRate">The tangential rate component.</param>
    /// <returns>Strike-slip rate magnitude (always non-negative).</returns>
    double GetStrikeSlipRate(double tangentialRate);

    /// <summary>
    /// Computes the obliquity angle between relative velocity and boundary normal (RFC-V2-0048 §4.5).
    /// </summary>
    /// <param name="normalRate">The normal rate component.</param>
    /// <param name="tangentialRate">The tangential rate component.</param>
    /// <returns>Obliquity angle in degrees (0° = pure convergence/divergence, 90° = pure strike-slip).</returns>
    double ComputeObliquityAngle(double normalRate, double tangentialRate);
}
