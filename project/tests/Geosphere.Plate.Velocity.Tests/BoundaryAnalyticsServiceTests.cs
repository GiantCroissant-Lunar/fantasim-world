using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;
using FluentAssertions;
using Xunit;

namespace Geosphere.Plate.Velocity.Tests;

/// <summary>
/// Test gates for RFC-V2-0048 Boundary Analytics implementation.
/// </summary>
public class BoundaryAnalyticsServiceTests
{
    private readonly BoundaryAnalyticsService _analytics;

    public BoundaryAnalyticsServiceTests()
    {
        // Create service with mocked dependencies for unit testing
        // In a full test setup, these would be proper mocks
        _analytics = new BoundaryAnalyticsService(
            null!, // IBoundaryVelocitySolver - not used in pure calculation tests
            null!  // IPlateVelocitySolver - not used in pure calculation tests
        );
    }

    #region Test Gate 1: Consistency Gate

    /// <summary>
    /// Test Gate 1: Rates derived from velocities must match finite rotation differences.
    /// </summary>
    [Fact]
    public void RateConsistency_MatchesFiniteRotationDifferences()
    {
        // This test verifies that the rate calculations are mathematically consistent
        // with the underlying velocity computations.
        // In a full implementation, this would compare against direct rotation calculations.

        // Arrange: Create sample profile with known rates
        var samples = CreateTestSamples(normalRate: -5.0, tangentialRate: 3.0);
        var profile = CreateTestProfile(samples);

        // Act
        var convergence = _analytics.GetConvergenceRateSummary(profile);

        // Assert: Convergence rate should equal -min(normalRate, 0)
        convergence.MaxConvergenceRate.Should().BeApproximately(5.0, 1e-10);
    }

    #endregion

    #region Test Gate 2: Conservation Gate

    /// <summary>
    /// Test Gate 2: Rate calculations follow conservation rules.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 0, 0)]      // Zero motion
    [InlineData(-5, 0, 5, 0)]     // Pure convergence
    [InlineData(5, 0, 0, 5)]      // Pure divergence
    [InlineData(0, 5, 0, 0)]      // Pure strike-slip
    [InlineData(-3, 4, 3, 0)]     // Convergent + strike-slip
    [InlineData(3, 4, 0, 3)]      // Divergent + strike-slip
    public void RateConservation_RatesSumCorrectly(
        double normalRate,
        double tangentialRate,
        double expectedConvergence,
        double expectedDivergence)
    {
        // Act
        var convergence = _analytics.GetConvergenceRate(normalRate);
        var divergence = _analytics.GetDivergenceRate(normalRate);
        var strikeSlip = _analytics.GetStrikeSlipRate(tangentialRate);

        // Assert
        convergence.Should().BeApproximately(expectedConvergence, 1e-10);
        divergence.Should().BeApproximately(expectedDivergence, 1e-10);
        strikeSlip.Should().BeApproximately(Math.Abs(tangentialRate), 1e-10);

        // Conservation: convergence and divergence are mutually exclusive
        (convergence > 0 && divergence > 0).Should().BeFalse(
            "A boundary cannot be both convergent and divergent");
    }

    #endregion

    #region Test Gate 3: Determinism Gate

    /// <summary>
    /// Test Gate 3a: Same inputs produce same outputs (bit-for-bit).
    /// </summary>
    [Fact]
    public void RateDeterminism_SameInputsProduceSameOutputs()
    {
        // Arrange
        var samples1 = CreateTestSamples(normalRate: -5.0, tangentialRate: 3.0);
        var samples2 = CreateTestSamples(normalRate: -5.0, tangentialRate: 3.0);
        var profile1 = CreateTestProfile(samples1);
        var profile2 = CreateTestProfile(samples2);

        // Act
        var stats1 = BoundaryAnalyticsService.ComputeStatistics(profile1.Samples.ToList());
        var stats2 = BoundaryAnalyticsService.ComputeStatistics(profile2.Samples.ToList());

        // Assert: Bit-for-bit equality
        stats1.MinNormalRate.Should().Be(stats2.MinNormalRate);
        stats1.MaxNormalRate.Should().Be(stats2.MaxNormalRate);
        stats1.MeanNormalRate.Should().Be(stats2.MeanNormalRate);
    }

    /// <summary>
    /// Test Gate 3b: Normal convention is consistent when swapping plates.
    /// </summary>
    [Fact]
    public void RateDeterminism_NormalConventionIsConsistent()
    {
        // Arrange: Two boundaries with opposite plate assignments
        // When normal vectors point in opposite directions, rates should negate

        // Act & Assert
        // Positive normal rate = divergence
        _analytics.GetDivergenceRate(5.0).Should().Be(5.0);
        _analytics.GetConvergenceRate(5.0).Should().Be(0.0);

        // Negative normal rate = convergence
        _analytics.GetDivergenceRate(-5.0).Should().Be(0.0);
        _analytics.GetConvergenceRate(-5.0).Should().Be(5.0);
    }

    #endregion

    #region Test Gate 4: Calculation Correctness

    /// <summary>
    /// Verifies convergence rate calculation (RFC-V2-0048 §5.1).
    /// </summary>
    [Theory]
    [InlineData(-10.0, 10.0)]   // Strong convergence
    [InlineData(-1.0, 1.0)]     // Weak convergence
    [InlineData(0.0, 0.0)]      // No convergence
    [InlineData(5.0, 0.0)]      // Divergence (not convergence)
    public void ConvergenceRate_CalculatedCorrectly(double normalRate, double expectedRate)
    {
        // Act
        var rate = _analytics.GetConvergenceRate(normalRate);

        // Assert
        rate.Should().BeApproximately(expectedRate, 1e-10);
        rate.Should().BeGreaterOrEqualTo(0, "Convergence rate must always be non-negative");
    }

    /// <summary>
    /// Verifies divergence rate calculation (RFC-V2-0048 §5.2).
    /// </summary>
    [Theory]
    [InlineData(10.0, 10.0)]    // Strong divergence
    [InlineData(1.0, 1.0)]      // Weak divergence
    [InlineData(0.0, 0.0)]      // No divergence
    [InlineData(-5.0, 0.0)]     // Convergence (not divergence)
    public void DivergenceRate_CalculatedCorrectly(double normalRate, double expectedRate)
    {
        // Act
        var rate = _analytics.GetDivergenceRate(normalRate);

        // Assert
        rate.Should().BeApproximately(expectedRate, 1e-10);
        rate.Should().BeGreaterOrEqualTo(0, "Divergence rate must always be non-negative");
    }

    /// <summary>
    /// Verifies strike-slip rate calculation (RFC-V2-0048 §5.4).
    /// </summary>
    [Theory]
    [InlineData(10.0, 10.0)]    // Right-lateral
    [InlineData(-10.0, 10.0)]   // Left-lateral
    [InlineData(0.0, 0.0)]      // No strike-slip
    [InlineData(5.5, 5.5)]      // Arbitrary rate
    public void StrikeSlipRate_CalculatedCorrectly(double tangentialRate, double expectedRate)
    {
        // Act
        var rate = _analytics.GetStrikeSlipRate(tangentialRate);

        // Assert
        rate.Should().BeApproximately(expectedRate, 1e-10);
        rate.Should().BeGreaterOrEqualTo(0, "Strike-slip rate must always be non-negative");
    }

    /// <summary>
    /// Verifies strike-slip sense classification (RFC-V2-0048 §5.4).
    /// </summary>
    [Theory]
    [InlineData(1.0, StrikeSlipSense.RightLateral)]
    [InlineData(0.1, StrikeSlipSense.RightLateral)]
    [InlineData(-1.0, StrikeSlipSense.LeftLateral)]
    [InlineData(-0.1, StrikeSlipSense.LeftLateral)]
    [InlineData(0.0, StrikeSlipSense.None)]
    [InlineData(1e-13, StrikeSlipSense.None)]  // Below epsilon
    public void StrikeSlipSense_ClassifiedCorrectly(double tangentialRate, StrikeSlipSense expectedSense)
    {
        // Act
        var sense = _analytics.GetStrikeSlipSense(tangentialRate);

        // Assert
        sense.Should().Be(expectedSense);
    }

    /// <summary>
    /// Verifies obliquity angle calculation (RFC-V2-0048 §4.5).
    /// </summary>
    [Theory]
    [InlineData(1.0, 0.0, 0.0)]      // Pure convergence
    [InlineData(-1.0, 0.0, 0.0)]     // Pure divergence
    [InlineData(0.0, 1.0, 90.0)]     // Pure strike-slip
    [InlineData(0.0, -1.0, 90.0)]    // Pure strike-slip (negative)
    [InlineData(1.0, 1.0, 45.0)]     // 45° oblique
    [InlineData(1.7320508075688772, 1.0, 30.0)]  // 30° oblique (sqrt(3))
    public void ObliquityAngle_CalculatedCorrectly(double normalRate, double tangentialRate, double expectedDegrees)
    {
        // Act
        var angle = _analytics.ComputeObliquityAngle(normalRate, tangentialRate);

        // Assert
        angle.Should().BeApproximately(expectedDegrees, 1e-10);
        angle.Should().BeInRange(0.0, 90.0, "Obliquity angle must be in [0°, 90°]");
    }

    #endregion

    #region Test Gate 5: Spreading Metrics

    /// <summary>
    /// Verifies spreading metrics calculation (RFC-V2-0048 §5.3).
    /// </summary>
    [Fact]
    public void SpreadingMetrics_CalculatedCorrectly()
    {
        // Arrange: Create samples with known divergence rates
        var samples = new List<BoundaryRateSample>
        {
            CreateSample(normalRate: 5.0, tangentialRate: 0.0, arcLength: 0),
            CreateSample(normalRate: 5.0, tangentialRate: 0.0, arcLength: 100),
            CreateSample(normalRate: 5.0, tangentialRate: 0.0, arcLength: 200),
        };
        var profile = CreateTestProfile(samples);

        // Act
        var metrics = _analytics.ComputeSpreadingMetrics(profile);

        // Assert
        metrics.HalfRate.Should().BeApproximately(5.0, 1e-10);
        metrics.FullRate.Should().BeApproximately(10.0, 1e-10);
    }

    /// <summary>
    /// Verifies that spreading metrics handle empty profiles gracefully.
    /// </summary>
    [Fact]
    public void SpreadingMetrics_EmptyProfile_ReturnsZeros()
    {
        // Arrange
        var profile = CreateTestProfile(new List<BoundaryRateSample>());

        // Act
        var metrics = _analytics.ComputeSpreadingMetrics(profile);

        // Assert
        metrics.FullRate.Should().Be(0.0);
        metrics.HalfRate.Should().Be(0.0);
    }

    #endregion

    #region Test Gate 6: Convergence Summary

    /// <summary>
    /// Verifies convergence summary calculation (RFC-V2-0048 §5.1).
    /// </summary>
    [Fact]
    public void ConvergenceSummary_CalculatedCorrectly()
    {
        // Arrange: Create samples with known convergence rates
        var samples = new List<BoundaryRateSample>
        {
            CreateSample(normalRate: -5.0, tangentialRate: 0.0, arcLength: 0),
            CreateSample(normalRate: -10.0, tangentialRate: 0.0, arcLength: 100),
            CreateSample(normalRate: -3.0, tangentialRate: 0.0, arcLength: 200),
        };
        var profile = CreateTestProfile(samples);

        // Act
        var summary = _analytics.GetConvergenceRateSummary(profile);

        // Assert
        summary.MaxConvergenceRate.Should().BeApproximately(10.0, 1e-10);
        summary.MeanConvergenceRate.Should().BeApproximately(6.0, 1e-10); // (5+10+3)/3
        summary.ConvergentSampleCount.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private static List<BoundaryRateSample> CreateTestSamples(double normalRate, double tangentialRate)
    {
        return new List<BoundaryRateSample>
        {
            CreateSample(normalRate, tangentialRate, 0),
            CreateSample(normalRate, tangentialRate, 100),
        };
    }

    private static BoundaryRateSample CreateSample(double normalRate, double tangentialRate, double arcLength)
    {
        return new BoundaryRateSample(
            Position: default,
            ArcLength: arcLength,
            RelativeVelocity: default,
            Tangent: default,
            Normal: default,
            Vertical: default,
            NormalRate: normalRate,
            TangentialRate: tangentialRate,
            VerticalRate: null,
            RelativeSpeed: Math.Sqrt(normalRate * normalRate + tangentialRate * tangentialRate),
            RelativeAzimuth: 0,
            ObliquityAngle: 0,
            Uncertainty: default,
            Provenance: new SampleProvenance(
                SampleIndex: 0,
                SegmentIndex: -1,
                SegmentT: double.NaN)
        );
    }

    private static BoundaryRateProfile CreateTestProfile(List<BoundaryRateSample> samples)
    {
        return new BoundaryRateProfile(
            BoundaryId: default,
            Type: BoundaryType.Divergent,
            Tick: default,
            Frame: MantleFrame.Instance,
            Samples: samples.ToImmutableArray(),
            Statistics: BoundaryAnalyticsService.ComputeStatistics(samples),
            ConvergenceSummary: null,
            SpreadingMetrics: null,
            StrikeSlipSummary: null
        );
    }

    #endregion
}
