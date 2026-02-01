using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Velocity.Solver;

/// <summary>
/// Implementation of boundary analytics service (RFC-V2-0048).
/// </summary>
public sealed class BoundaryAnalyticsService : IBoundaryAnalytics
{
    private const double Epsilon = 1e-12;

    private readonly IBoundaryVelocitySolver _velocitySolver;
    private readonly IPlateVelocitySolver _plateVelocitySolver;

    /// <summary>
    /// Creates a new instance of the boundary analytics service.
    /// </summary>
    /// <param name="velocitySolver">The boundary velocity solver.</param>
    /// <param name="plateVelocitySolver">The plate velocity solver.</param>
    public BoundaryAnalyticsService(
        IBoundaryVelocitySolver? velocitySolver = null,
        IPlateVelocitySolver? plateVelocitySolver = null)
    {
        _velocitySolver = velocitySolver!;
        _plateVelocitySolver = plateVelocitySolver!;
    }

    /// <inheritdoc />
    public ValueTask<BoundaryRateProfile> SampleBoundaryVelocitiesAsync(
        BoundaryId boundaryId,
        CanonicalTick tick,
        BoundarySampleSpec sampling,
        ReferenceFrameId? frame = null)
    {
        if (boundaryId.IsEmpty)
            throw new ArgumentException("BoundaryId cannot be empty.", nameof(boundaryId));

        _ = tick;
        _ = sampling;
        _ = frame;

        throw new NotSupportedException(
            "SampleBoundaryVelocitiesAsync is not yet integrated with topology/kinematics read models. " +
            "For now, use IBoundaryVelocitySolver with IPlateTopologyStateView + IPlateKinematicsStateView and compute rate analytics from those samples.");
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<BoundaryRateProfile>> SampleBoundariesAsync(
        IEnumerable<BoundaryId> boundaryIds,
        CanonicalTick tick,
        BoundarySampleSpec sampling,
        ReferenceFrameId? frame = null)
    {
        ArgumentNullException.ThrowIfNull(boundaryIds);
        var profiles = new List<BoundaryRateProfile>();
        foreach (var boundaryId in boundaryIds)
        {
            profiles.Add(await SampleBoundaryVelocitiesAsync(boundaryId, tick, sampling, frame));
        }
        return profiles.OrderBy(p => p.BoundaryId.Value).ToList();
    }

    /// <inheritdoc />
    public SpreadingRateMetrics ComputeSpreadingMetrics(BoundaryRateProfile profile)
    {
        if (profile.Samples.IsEmpty)
        {
            return new SpreadingRateMetrics(
                FullRate: 0,
                HalfRate: 0,
                Asymmetry: 0,
                Obliquity: 0,
                AlongStrikeVariation: 0
            );
        }

        var divergentSamples = profile.Samples.Where(s => s.NormalRate > 0).ToList();
        if (!divergentSamples.Any())
        {
            return new SpreadingRateMetrics(
                FullRate: 0,
                HalfRate: 0,
                Asymmetry: 0,
                Obliquity: 0,
                AlongStrikeVariation: 0
            );
        }

        var normalRates = divergentSamples.Select(s => s.NormalRate).ToList();
        var tangentialRates = divergentSamples.Select(s => s.TangentialRate).ToList();

        var meanDivergence = normalRates.Average();
        var meanTangential = tangentialRates.Average();
        var stdDevNormal = ComputeStdDev(normalRates);

        // Asymmetry: ratio of tangential to normal (simplified)
        var asymmetry = Math.Abs(meanTangential) / meanDivergence;

        // Obliquity: angle in degrees
        var obliquity = Math.Atan2(Math.Abs(meanTangential), meanDivergence) * 180.0 / Math.PI;

        return new SpreadingRateMetrics(
            FullRate: meanDivergence * 2,
            HalfRate: meanDivergence,
            Asymmetry: asymmetry,
            Obliquity: obliquity,
            AlongStrikeVariation: stdDevNormal
        );
    }

    /// <inheritdoc />
    public ConvergenceRateSummary GetConvergenceRateSummary(BoundaryRateProfile profile)
    {
        if (profile.Samples.IsEmpty)
        {
            return new ConvergenceRateSummary(
                MaxConvergenceRate: 0,
                MeanConvergenceRate: 0,
                TotalConvergentLength: 0,
                ConvergentSampleCount: 0,
                MaxConvergenceUncertainty: null
            );
        }

        var convergentSamples = profile.Samples.Where(s => s.NormalRate < 0).ToList();
        if (!convergentSamples.Any())
        {
            return new ConvergenceRateSummary(
                MaxConvergenceRate: 0,
                MeanConvergenceRate: 0,
                TotalConvergentLength: 0,
                ConvergentSampleCount: 0,
                MaxConvergenceUncertainty: null
            );
        }

        var convergenceRates = convergentSamples.Select(s => -s.NormalRate).ToList();
        var maxRate = convergenceRates.Max();
        var meanRate = convergenceRates.Average();

        // Estimate length from sample count and arc length spacing (simplified)
        var totalLength = convergentSamples.Count > 1
            ? convergentSamples.Last().ArcLength - convergentSamples.First().ArcLength
            : 0;

        return new ConvergenceRateSummary(
            MaxConvergenceRate: maxRate,
            MeanConvergenceRate: meanRate,
            TotalConvergentLength: totalLength,
            ConvergentSampleCount: convergentSamples.Count,
            MaxConvergenceUncertainty: convergentSamples.Max(s => s.Uncertainty.NormalRateSigma)
        );
    }

    /// <inheritdoc />
    public StrikeSlipSummary GetStrikeSlipSummary(BoundaryRateProfile profile)
    {
        if (profile.Samples.IsEmpty)
        {
            return new StrikeSlipSummary(
                MaxStrikeSlipRate: 0,
                MeanStrikeSlipRate: 0,
                TotalStrikeSlipLength: 0,
                StrikeSlipSampleCount: 0,
                RightLateralCount: 0,
                LeftLateralCount: 0
            );
        }

        var strikeSlipSamples = profile.Samples.Where(s => Math.Abs(s.TangentialRate) > Epsilon).ToList();
        if (!strikeSlipSamples.Any())
        {
            return new StrikeSlipSummary(
                MaxStrikeSlipRate: 0,
                MeanStrikeSlipRate: 0,
                TotalStrikeSlipLength: 0,
                StrikeSlipSampleCount: 0,
                RightLateralCount: 0,
                LeftLateralCount: 0
            );
        }

        var strikeSlipRates = strikeSlipSamples.Select(s => Math.Abs(s.TangentialRate)).ToList();
        var rightLateralCount = strikeSlipSamples.Count(s => s.TangentialRate > 0);
        var leftLateralCount = strikeSlipSamples.Count(s => s.TangentialRate < 0);

        // Estimate length
        var totalLength = strikeSlipSamples.Count > 1
            ? strikeSlipSamples.Last().ArcLength - strikeSlipSamples.First().ArcLength
            : 0;

        return new StrikeSlipSummary(
            MaxStrikeSlipRate: strikeSlipRates.Max(),
            MeanStrikeSlipRate: strikeSlipRates.Average(),
            TotalStrikeSlipLength: totalLength,
            StrikeSlipSampleCount: strikeSlipSamples.Count,
            RightLateralCount: rightLateralCount,
            LeftLateralCount: leftLateralCount
        );
    }

    /// <inheritdoc />
    public StrikeSlipSense GetStrikeSlipSense(double tangentialRate)
    {
        if (Math.Abs(tangentialRate) < Epsilon)
            return StrikeSlipSense.None;
        return tangentialRate > 0 ? StrikeSlipSense.RightLateral : StrikeSlipSense.LeftLateral;
    }

    /// <inheritdoc />
    public double GetConvergenceRate(double normalRate)
    {
        return Math.Max(0, -normalRate);
    }

    /// <inheritdoc />
    public double GetDivergenceRate(double normalRate)
    {
        return Math.Max(0, normalRate);
    }

    /// <inheritdoc />
    public double GetStrikeSlipRate(double tangentialRate)
    {
        return Math.Abs(tangentialRate);
    }

    /// <inheritdoc />
    public double ComputeObliquityAngle(double normalRate, double tangentialRate)
    {
        return Math.Atan2(Math.Abs(tangentialRate), Math.Abs(normalRate)) * 180.0 / Math.PI;
    }

    /// <summary>
    /// Computes sample statistics from boundary velocity samples (RFC-V2-0048 §5.6).
    /// </summary>
    internal static RateStatistics ComputeStatistics(IReadOnlyList<BoundaryRateSample> samples)
    {
        if (samples.Count == 0)
        {
            return new RateStatistics(
                MinNormalRate: 0,
                MaxNormalRate: 0,
                MeanNormalRate: 0,
                MinTangentialRate: 0,
                MaxTangentialRate: 0,
                MeanTangentialRate: 0,
                MaxRelativeSpeed: 0,
                MeanRelativeSpeed: 0
            );
        }

        var normalRates = samples.Select(s => s.NormalRate).ToList();
        var tangentialRates = samples.Select(s => s.TangentialRate).ToList();
        var speeds = samples.Select(s => s.RelativeSpeed).ToList();

        return new RateStatistics(
            MinNormalRate: normalRates.Min(),
            MaxNormalRate: normalRates.Max(),
            MeanNormalRate: normalRates.Average(),
            MinTangentialRate: tangentialRates.Min(),
            MaxTangentialRate: tangentialRates.Max(),
            MeanTangentialRate: tangentialRates.Average(),
            MaxRelativeSpeed: speeds.Max(),
            MeanRelativeSpeed: speeds.Average()
        );
    }

    private static double ComputeStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(v => (v - mean) * (v - mean));
        return Math.Sqrt(variance);
    }
}
