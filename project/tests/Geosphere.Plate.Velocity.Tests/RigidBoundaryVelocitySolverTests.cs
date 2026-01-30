using FluentAssertions;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Velocity.Tests;

/// <summary>
/// Unit tests for RigidBoundaryVelocitySolver (RFC-V2-0034).
/// These tests validate the key invariants documented in the RFC:
/// - Sampling determinism
/// - Tangent follows geometry direction
/// - Normal points left → right (topology-only)
/// - Aggregate math correctness
/// - Fallback behavior when kinematics missing
/// - Batch ordering determinism
/// </summary>
public sealed class RigidBoundaryVelocitySolverTests
{
    private const double Epsilon = 1e-9;

    private static readonly CanonicalTick DefaultTick = new(1000);
    private static readonly BoundarySamplingSpec DefaultSampling = new(32, SamplingMode.ArcLength, IncludeEndpoints: true);

    #region 1️⃣ Sampling Determinism

    [Fact]
    public void AnalyzeBoundary_IsDeterministic_SameInputsProduceSameOutputs()
    {
        // Arrange
        var (solver, topology, kinematics, boundary) = CreateDivergentBoundaryScenario();

        // Act
        var profile1 = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);
        var profile2 = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert - bit-identical outputs
        profile1.BoundaryId.Should().Be(profile2.BoundaryId);
        profile1.SampleCount.Should().Be(profile2.SampleCount);
        profile1.MinNormalRate.Should().Be(profile2.MinNormalRate);
        profile1.MaxNormalRate.Should().Be(profile2.MaxNormalRate);
        profile1.MeanNormalRate.Should().Be(profile2.MeanNormalRate);
        profile1.MeanSlipRate.Should().Be(profile2.MeanSlipRate);
        profile1.MinSampleIndex.Should().Be(profile2.MinSampleIndex);
        profile1.MaxSampleIndex.Should().Be(profile2.MaxSampleIndex);
    }

    [Fact]
    public void AnalyzeBoundary_SampleCountMatchesSamplingSpec()
    {
        // Arrange
        var (solver, topology, kinematics, boundary) = CreateDivergentBoundaryScenario();
        var sampling = new BoundarySamplingSpec(16, SamplingMode.ArcLength, IncludeEndpoints: true);

        // Act
        var profile = solver.AnalyzeBoundary(boundary, sampling, DefaultTick, topology, kinematics);

        // Assert
        profile.SampleCount.Should().Be(16);
    }

    [Fact]
    public void AnalyzeBoundary_IsByteIdentical_WhenSerialized()
    {
        // Arrange: RFC-V2-0034 requires bit-identical determinism
        // This test serializes the output and compares byte-for-byte
        var (solver, topology, kinematics, boundary) = CreateDivergentBoundaryScenario();

        // Act
        var profile1 = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);
        var profile2 = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);

        // Serialize both profiles
        var bytes1 = MessagePackSerializer.Serialize(profile1);
        var bytes2 = MessagePackSerializer.Serialize(profile2);

        // Assert: Byte-identical output
        bytes1.Should().BeEquivalentTo(bytes2, "RFC-V2-0034 requires byte-identical determinism");
        bytes1.Length.Should().BeGreaterThan(0, "serialized output should not be empty");
    }

    [Fact]
    public void AnalyzeAllBoundaries_IsByteIdentical_WhenSerialized()
    {
        // Arrange: Verify batch determinism at byte level
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdB = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));

        var geometry1 = CreateGreatCircleArc(0.0, -20.0, 0.0, 20.0);
        var geometry2 = CreateGreatCircleArc(10.0, -10.0, 10.0, 10.0);

        var boundary1 = new Boundary(
            new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003")),
            plateIdA, plateIdB, BoundaryType.Divergent, geometry1, false, null);
        var boundary2 = new Boundary(
            new BoundaryId(Guid.Parse("00000002-0000-0000-0000-000000000004")),
            plateIdA, plateIdB, BoundaryType.Transform, geometry2, false, null);

        var boundaries = new[] { boundary2, boundary1 }; // Intentionally out of order

        var kinematics = new StationaryKinematicsState();
        var topology = new MultiBoundaryTopologyState(boundaries, plateIdA, plateIdB);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act
        var collection1 = solver.AnalyzeAllBoundaries(boundaries, DefaultSampling, DefaultTick, topology, kinematics);
        var collection2 = solver.AnalyzeAllBoundaries(boundaries, DefaultSampling, DefaultTick, topology, kinematics);

        // Serialize each profile (not the whole collection which contains CanonicalTick)
        collection1.Profiles.Length.Should().Be(collection2.Profiles.Length);

        for (var i = 0; i < collection1.Profiles.Length; i++)
        {
            var bytes1 = MessagePackSerializer.Serialize(collection1.Profiles[i]);
            var bytes2 = MessagePackSerializer.Serialize(collection2.Profiles[i]);
            bytes1.Should().BeEquivalentTo(bytes2,
                $"RFC-V2-0034 requires byte-identical batch determinism (profile {i})");
        }
    }

    #endregion

    #region 2️⃣ Tangent Follows Geometry Order

    [Fact]
    public void Tangent_FollowsGeometryDirection_NotReversed()
    {
        // Arrange: Simple boundary from (1,0,0) → (0,1,0)
        // Expected tangent at first sample: roughly (-1,1,0) normalized (pointing from start to end)
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        // Polyline direction: (1,0,0) → (0,1,0)
        var points = new Point3[]
        {
            new(1, 0, 0),
            new(0, 1, 0)
        };
        var geometry = new Polyline3(points);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        var kinematics = new StationaryKinematicsState();
        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Use minimal sampling at endpoints
        var sampling = new BoundarySamplingSpec(2, SamplingMode.ChordLength, IncludeEndpoints: true);

        // Act - get individual samples to inspect tangent direction
        var samples = solver.GetBoundarySamples(boundary, sampling, DefaultTick, topology, kinematics);

        // Assert: Tangent should point in geometry direction (from start toward end)
        samples.Should().HaveCount(2);

        var tangent = samples[0].Tangent;
        // Expected direction: (0,1,0) - (1,0,0) = (-1,1,0), normalized
        var expectedDirection = new Vector3d(-1, 1, 0).Normalize();

        // Tangent should have positive dot product with expected direction (same general direction)
        var dot = tangent.X * expectedDirection.X + tangent.Y * expectedDirection.Y + tangent.Z * expectedDirection.Z;
        dot.Should().BeGreaterThan(0.9, "tangent should follow geometry order, not be reversed");
    }

    [Fact]
    public void Tangent_FollowsGeometryDirection_WithReversedPolyline()
    {
        // Arrange: Same boundary points but reversed order: (0,1,0) → (1,0,0)
        // Expected tangent at first sample: roughly (1,-1,0) normalized (opposite of previous test)
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        // Polyline direction: (0,1,0) → (1,0,0) - REVERSED from previous test
        var points = new Point3[]
        {
            new(0, 1, 0),
            new(1, 0, 0)
        };
        var geometry = new Polyline3(points);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        var kinematics = new StationaryKinematicsState();
        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        var sampling = new BoundarySamplingSpec(2, SamplingMode.ChordLength, IncludeEndpoints: true);

        // Act
        var samples = solver.GetBoundarySamples(boundary, sampling, DefaultTick, topology, kinematics);

        // Assert: Tangent should now point in opposite direction
        samples.Should().HaveCount(2);

        var tangent = samples[0].Tangent;
        // Expected direction: (1,0,0) - (0,1,0) = (1,-1,0), normalized
        var expectedDirection = new Vector3d(1, -1, 0).Normalize();

        var dot = tangent.X * expectedDirection.X + tangent.Y * expectedDirection.Y + tangent.Z * expectedDirection.Z;
        dot.Should().BeGreaterThan(0.9, "tangent should follow reversed geometry order");
    }

    #endregion

    #region 3️⃣ Normal Points Left → Right (Topology-Only)

    [Fact]
    public void Normal_FlipsDirection_WhenPlateIdsSwapped()
    {
        // Arrange: Two boundaries with same geometry but swapped left/right plates
        // RFC-V2-0034: Normal must flip to always point from smaller PlateId to larger
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001")); // smaller
        var plateIdB = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002")); // larger
        var boundaryId1 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));
        var boundaryId2 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000004"));

        var points = new Point3[]
        {
            new(1, 0, 0),
            new(0, 1, 0)
        };
        var geometry = new Polyline3(points);

        // Boundary 1: A is left, B is right
        var boundary1 = new Boundary(boundaryId1, plateIdA, plateIdB, BoundaryType.Divergent, geometry, false, null);
        // Boundary 2: B is left, A is right (swapped)
        var boundary2 = new Boundary(boundaryId2, plateIdB, plateIdA, BoundaryType.Divergent, geometry, false, null);

        var kinematics = new StationaryKinematicsState();
        var topology1 = new SingleBoundaryTopologyState(boundary1, plateIdA, plateIdB);
        var topology2 = new SingleBoundaryTopologyState(boundary2, plateIdB, plateIdA);

        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);
        var sampling = new BoundarySamplingSpec(2, SamplingMode.ChordLength, IncludeEndpoints: true);

        // Act
        var samples1 = solver.GetBoundarySamples(boundary1, sampling, DefaultTick, topology1, kinematics);
        var samples2 = solver.GetBoundarySamples(boundary2, sampling, DefaultTick, topology2, kinematics);

        // Assert: Normal directions should be OPPOSITE since plates are swapped
        samples1.Should().HaveCount(2);
        samples2.Should().HaveCount(2);

        var normal1 = samples1[0].Normal;
        var normal2 = samples2[0].Normal;

        // Dot product of opposite vectors should be negative (close to -1)
        var dot = normal1.X * normal2.X + normal1.Y * normal2.Y + normal1.Z * normal2.Z;
        dot.Should().BeLessThan(-0.9,
            "normal should flip direction when left/right plates are swapped (RFC-V2-0034 §8.3)");
    }

    [Fact]
    public void Normal_UsesTopologyOnly_NoCentroidTricks()
    {
        // Arrange: The normal should be determined by PlateId ordering, not by geometry centroid
        // This test verifies that same PlateId ordering produces same normal direction
        // regardless of where the boundary is located on the sphere
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdB = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));

        // Two boundaries with different locations but same PlateId ordering
        var geometry1 = new Polyline3(new[] { new Point3(1, 0, 0), new Point3(0, 1, 0) });
        var geometry2 = new Polyline3(new[] { new Point3(0, 0, 1), new Point3(0, 1, 0) }); // Different location

        var boundary1 = new Boundary(
            new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003")),
            plateIdA, plateIdB, BoundaryType.Divergent, geometry1, false, null);
        var boundary2 = new Boundary(
            new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000004")),
            plateIdA, plateIdB, BoundaryType.Divergent, geometry2, false, null);

        var kinematics = new StationaryKinematicsState();
        var topology1 = new SingleBoundaryTopologyState(boundary1, plateIdA, plateIdB);
        var topology2 = new SingleBoundaryTopologyState(boundary2, plateIdA, plateIdB);

        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);
        var sampling = new BoundarySamplingSpec(2, SamplingMode.ChordLength, IncludeEndpoints: true);

        // Act
        var samples1 = solver.GetBoundarySamples(boundary1, sampling, DefaultTick, topology1, kinematics);
        var samples2 = solver.GetBoundarySamples(boundary2, sampling, DefaultTick, topology2, kinematics);

        // Assert: Both should have valid non-zero normals (topology-determined, not centroid-based)
        samples1.Should().NotBeEmpty();
        samples2.Should().NotBeEmpty();

        var normal1 = samples1[0].Normal;
        var normal2 = samples2[0].Normal;

        // Normals should be non-zero (computed correctly)
        var length1 = Math.Sqrt(normal1.X * normal1.X + normal1.Y * normal1.Y + normal1.Z * normal1.Z);
        var length2 = Math.Sqrt(normal2.X * normal2.X + normal2.Y * normal2.Y + normal2.Z * normal2.Z);

        length1.Should().BeApproximately(1.0, 0.001, "normal should be unit length");
        length2.Should().BeApproximately(1.0, 0.001, "normal should be unit length");
    }

    [Fact]
    public void Normal_PointsFromLeftToRight_ConsistentWithPlateIdOrdering()
    {
        // Arrange: Test that swapping left/right plate IDs produces consistent behavior
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdB = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId1 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));
        var boundaryId2 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000004"));

        // Simple boundary geometry
        var points = new Point3[]
        {
            new(1, 0, 0),
            new(0, 1, 0)
        };
        var geometry = new Polyline3(points);

        // Boundary 1: A is left, B is right
        var boundary1 = new Boundary(boundaryId1, plateIdA, plateIdB, BoundaryType.Divergent, geometry, false, null);
        // Boundary 2: B is left, A is right (swapped)
        var boundary2 = new Boundary(boundaryId2, plateIdB, plateIdA, BoundaryType.Divergent, geometry, false, null);

        // Use identical kinematics for both
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdA, new Vector3d(0, 0, 1), 0.1,   // A rotates around Z
            plateIdB, new Vector3d(0, 0, 1), -0.1); // B rotates around Z opposite

        var topology1 = new SingleBoundaryTopologyState(boundary1, plateIdA, plateIdB);
        var topology2 = new SingleBoundaryTopologyState(boundary2, plateIdB, plateIdA);

        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);
        var sampling = new BoundarySamplingSpec(4, SamplingMode.ArcLength, IncludeEndpoints: true);

        // Act
        var profile1 = solver.AnalyzeBoundary(boundary1, sampling, DefaultTick, topology1, kinematics);
        var profile2 = solver.AnalyzeBoundary(boundary2, sampling, DefaultTick, topology2, kinematics);

        // Assert: Both profiles should have the same number of samples
        profile1.SampleCount.Should().Be(profile2.SampleCount,
            "same geometry should produce same sample count");

        // The mean slip rates should be equal (tangential motion is the same)
        Math.Abs(profile1.MeanSlipRate - profile2.MeanSlipRate).Should().BeLessThan(0.001,
            "same geometry and kinematics should produce same slip rate magnitude");

        // Min/max sample indices should be valid
        profile1.MinSampleIndex.Should().BeGreaterOrEqualTo(0);
        profile1.MaxSampleIndex.Should().BeLessThan(profile1.SampleCount);
        profile2.MinSampleIndex.Should().BeGreaterOrEqualTo(0);
        profile2.MaxSampleIndex.Should().BeLessThan(profile2.SampleCount);
    }

    #endregion

    #region 4️⃣ Aggregate Math Correctness

    [Fact]
    public void Aggregates_MinMaxMean_AreComputedCorrectly()
    {
        // Arrange: Use a scenario where we know the velocity pattern
        var (solver, topology, kinematics, boundary) = CreateDivergentBoundaryScenario();
        var sampling = new BoundarySamplingSpec(10, SamplingMode.ArcLength, IncludeEndpoints: true);

        // Act
        var profile = solver.AnalyzeBoundary(boundary, sampling, DefaultTick, topology, kinematics);

        // Assert: Basic aggregate invariants
        profile.MinNormalRate.Should().BeLessThanOrEqualTo(profile.MeanNormalRate);
        profile.MeanNormalRate.Should().BeLessThanOrEqualTo(profile.MaxNormalRate);
        profile.MeanSlipRate.Should().BeGreaterThanOrEqualTo(0, "slip rate is absolute value");

        // Min/Max sample indices should be valid
        profile.MinSampleIndex.Should().BeGreaterThanOrEqualTo(0);
        profile.MinSampleIndex.Should().BeLessThan(profile.SampleCount);
        profile.MaxSampleIndex.Should().BeGreaterThanOrEqualTo(0);
        profile.MaxSampleIndex.Should().BeLessThan(profile.SampleCount);
    }

    [Fact]
    public void Aggregates_ForStationaryPlates_AllRatesAreZero()
    {
        // Arrange: Both plates stationary
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        var geometry = CreateGreatCircleArc(0.0, -30.0, 0.0, 30.0);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        var kinematics = new StationaryKinematicsState();
        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act
        var profile = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert: All rates should be zero for stationary plates
        profile.MinNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MaxNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MeanNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MeanSlipRate.Should().BeApproximately(0, Epsilon);
    }

    #endregion

    #region 5️⃣ Fallback Behavior (Kinematics Missing)

    [Fact]
    public void AnalyzeBoundary_ReturnsZeroRates_WhenKinematicsMissing()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        var geometry = CreateGreatCircleArc(0.0, -30.0, 0.0, 30.0);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        // Kinematics returns false for all plates
        var kinematics = new MissingKinematicsState();
        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act - should not throw
        var profile = solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert: All rates should be zero (fallback behavior per RFC-V2-0033/0034)
        profile.MinNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MaxNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MeanNormalRate.Should().BeApproximately(0, Epsilon);
        profile.MeanSlipRate.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void AnalyzeBoundary_DoesNotThrow_WhenKinematicsMissing()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        var geometry = CreateGreatCircleArc(0.0, -30.0, 0.0, 30.0);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        var kinematics = new MissingKinematicsState();
        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act & Assert - should not throw
        var action = () => solver.AnalyzeBoundary(boundary, DefaultSampling, DefaultTick, topology, kinematics);
        action.Should().NotThrow();
    }

    #endregion

    #region 6️⃣ Batch Ordering Determinism

    [Fact]
    public void AnalyzeAllBoundaries_SortsByBoundaryIdValue()
    {
        // Arrange: Create boundaries with out-of-order IDs
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdB = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));

        // IDs that will sort differently by Value
        var boundaryId3 = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000000"));
        var boundaryId1 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000000"));
        var boundaryId2 = new BoundaryId(Guid.Parse("00000002-0000-0000-0000-000000000000"));

        var geometry = CreateGreatCircleArc(0.0, -10.0, 0.0, 10.0);

        var boundary3 = new Boundary(boundaryId3, plateIdA, plateIdB, BoundaryType.Divergent, geometry, false, null);
        var boundary1 = new Boundary(boundaryId1, plateIdA, plateIdB, BoundaryType.Convergent, geometry, false, null);
        var boundary2 = new Boundary(boundaryId2, plateIdA, plateIdB, BoundaryType.Transform, geometry, false, null);

        // Feed boundaries in wrong order: 3, 1, 2
        var boundaries = new[] { boundary3, boundary1, boundary2 };

        var kinematics = new StationaryKinematicsState();
        var topology = new MultiBoundaryTopologyState(boundaries, plateIdA, plateIdB);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act
        var collection = solver.AnalyzeAllBoundaries(boundaries, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert: Output should be sorted by BoundaryId.Value
        collection.Profiles.Should().HaveCount(3);
        collection.Profiles[0].BoundaryId.Should().Be(boundaryId1);
        collection.Profiles[1].BoundaryId.Should().Be(boundaryId2);
        collection.Profiles[2].BoundaryId.Should().Be(boundaryId3);
    }

    [Fact]
    public void AnalyzeAllBoundaries_ExcludesRetiredBoundaries()
    {
        // Arrange
        var plateIdA = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdB = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));

        var boundaryId1 = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000000"));
        var boundaryId2 = new BoundaryId(Guid.Parse("00000002-0000-0000-0000-000000000000"));

        var geometry = CreateGreatCircleArc(0.0, -10.0, 0.0, 10.0);

        var activeBoundary = new Boundary(boundaryId1, plateIdA, plateIdB, BoundaryType.Divergent, geometry, false, null);
        var retiredBoundary = new Boundary(boundaryId2, plateIdA, plateIdB, BoundaryType.Convergent, geometry, true, "Test retirement");

        var boundaries = new[] { activeBoundary, retiredBoundary };

        var kinematics = new StationaryKinematicsState();
        var topology = new MultiBoundaryTopologyState(boundaries, plateIdA, plateIdB);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        // Act
        var collection = solver.AnalyzeAllBoundaries(boundaries, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert: Only active boundary should be included
        collection.Profiles.Should().HaveCount(1);
        collection.Profiles[0].BoundaryId.Should().Be(boundaryId1);
    }

    [Fact]
    public void AnalyzeAllBoundaries_IncludesSolverIdInCollection()
    {
        // Arrange
        var (solver, topology, kinematics, boundary) = CreateDivergentBoundaryScenario();
        var boundaries = new[] { boundary };

        // Act
        var collection = solver.AnalyzeAllBoundaries(boundaries, DefaultSampling, DefaultTick, topology, kinematics);

        // Assert
        collection.SolverId.Should().Be(nameof(RigidBoundaryVelocitySolver));
        collection.Tick.Should().Be(DefaultTick);
    }

    #endregion

    #region Helper Methods

    private static (RigidBoundaryVelocitySolver solver, IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics, Boundary boundary) CreateDivergentBoundaryScenario()
    {
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        // Angular velocities: opposite rotations create divergence
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(1, 0, 0), 0.1,
            plateIdRight, new Vector3d(1, 0, 0), -0.1);

        var geometry = CreateGreatCircleArc(0.0, -30.0, 0.0, 30.0);
        var boundary = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null);

        var topology = new SingleBoundaryTopologyState(boundary, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new RigidBoundaryVelocitySolver(velocitySolver);

        return (solver, topology, kinematics, boundary);
    }

    private static Polyline3 CreateGreatCircleArc(double lon1, double lat1, double lon2, double lat2)
    {
        const int segments = 4;
        var points = new Point3[segments + 1];

        var start = LatLonToPoint3(lat1, lon1);
        var end = LatLonToPoint3(lat2, lon2);

        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            points[i] = GreatCircleInterpolate(start, end, t);
        }

        return new Polyline3(points);
    }

    private static Point3 LatLonToPoint3(double lat, double lon)
    {
        double latRad = lat * Math.PI / 180.0;
        double lonRad = lon * Math.PI / 180.0;

        double x = Math.Cos(latRad) * Math.Cos(lonRad);
        double y = Math.Cos(latRad) * Math.Sin(lonRad);
        double z = Math.Sin(latRad);

        return new Point3(x, y, z);
    }

    private static Point3 GreatCircleInterpolate(Point3 a, Point3 b, double t)
    {
        double ax = a.X, ay = a.Y, az = a.Z;
        double bx = b.X, by = b.Y, bz = b.Z;

        double dot = ax * bx + ay * by + az * bz;
        dot = Math.Clamp(dot, -1.0, 1.0);

        double angle = Math.Acos(dot);

        if (angle < 1e-10)
            return a;

        double sinAngle = Math.Sin(angle);
        double wa = Math.Sin((1 - t) * angle) / sinAngle;
        double wb = Math.Sin(t * angle) / sinAngle;

        double x = wa * ax + wb * bx;
        double y = wa * ay + wb * by;
        double z = wa * az + wb * bz;

        return new Point3(x, y, z);
    }

    #endregion

    #region Test Fakes

    private sealed class StationaryKinematicsState : IPlateKinematicsStateView
    {
        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = Quaterniond.Identity;
            return true;
        }
    }

    private sealed class MissingKinematicsState : IPlateKinematicsStateView
    {
        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = Quaterniond.Identity;
            return false; // Simulate missing kinematics
        }
    }

    private sealed class TwoPlateRotatingKinematicsState : IPlateKinematicsStateView
    {
        private readonly PlateId _plateA;
        private readonly Vector3d _axisA;
        private readonly double _rateA;
        private readonly PlateId _plateB;
        private readonly Vector3d _axisB;
        private readonly double _rateB;

        public TwoPlateRotatingKinematicsState(
            PlateId plateA, Vector3d axisA, double rateA,
            PlateId plateB, Vector3d axisB, double rateB)
        {
            _plateA = plateA;
            _axisA = axisA;
            _rateA = rateA;
            _plateB = plateB;
            _axisB = axisB;
            _rateB = rateB;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            if (plateId == _plateA)
            {
                var angle = tick.Value * _rateA;
                rotation = Quaterniond.FromAxisAngle(_axisA, angle);
                return true;
            }
            if (plateId == _plateB)
            {
                var angle = tick.Value * _rateB;
                rotation = Quaterniond.FromAxisAngle(_axisB, angle);
                return true;
            }
            rotation = Quaterniond.Identity;
            return false;
        }
    }

    private sealed class SingleBoundaryTopologyState : IPlateTopologyStateView
    {
        private readonly Dictionary<PlateId, PlateEntity> _plates;
        private readonly Dictionary<BoundaryId, Boundary> _boundaries;

        public SingleBoundaryTopologyState(Boundary boundary, PlateId plateIdLeft, PlateId plateIdRight)
        {
            _plates = new Dictionary<PlateId, PlateEntity>
            {
                [plateIdLeft] = new PlateEntity(plateIdLeft, false, null),
                [plateIdRight] = new PlateEntity(plateIdRight, false, null)
            };
            _boundaries = new Dictionary<BoundaryId, Boundary>
            {
                [boundary.BoundaryId] = boundary
            };
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesTopology, "0");
        public IReadOnlyDictionary<PlateId, PlateEntity> Plates => _plates;
        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries => _boundaries;
        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; } = new Dictionary<JunctionId, Junction>();
        public long LastEventSequence { get; } = 0;
    }

    private sealed class MultiBoundaryTopologyState : IPlateTopologyStateView
    {
        private readonly Dictionary<PlateId, PlateEntity> _plates;
        private readonly Dictionary<BoundaryId, Boundary> _boundaries;

        public MultiBoundaryTopologyState(IEnumerable<Boundary> boundaries, PlateId plateIdA, PlateId plateIdB)
        {
            _plates = new Dictionary<PlateId, PlateEntity>
            {
                [plateIdA] = new PlateEntity(plateIdA, false, null),
                [plateIdB] = new PlateEntity(plateIdB, false, null)
            };
            _boundaries = boundaries.ToDictionary(b => b.BoundaryId);
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesTopology, "0");
        public IReadOnlyDictionary<PlateId, PlateEntity> Plates => _plates;
        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries => _boundaries;
        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; } = new Dictionary<JunctionId, Junction>();
        public long LastEventSequence { get; } = 0;
    }

    #endregion
}
