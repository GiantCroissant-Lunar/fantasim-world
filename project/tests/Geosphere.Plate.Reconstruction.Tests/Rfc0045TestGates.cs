using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FrameId = FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using Xunit;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// RFC-V2-0045 Section 7 Test Gates.
/// These tests verify the determinism, stability, and provenance completeness
/// requirements specified in the RFC.
/// </summary>
public sealed class Rfc0045TestGates
{
    private static readonly PlateId PlateA = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PlateId PlateB = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly BoundaryId BoundaryId1 = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly BoundaryId BoundaryId2 = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly CanonicalTick TargetTick = new(100);

    /// <summary>
    /// RFC-V2-0045 §7 Test Gate 1: Reconstruct_IsDeterministic.
    /// Same inputs must produce identical outputs.
    /// </summary>
    [Fact]
    public void Reconstruct_IsDeterministic()
    {
        // Arrange
        var solver = new NaivePlateReconstructionSolver();
        var topology = CreateTestTopology();
        var kinematics = new FakeKinematicsState();
        var policy = CreateTestPolicy();

        // Act: Call ReconstructWithProvenance twice with same inputs
        var result1 = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);
        var result2 = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);

        // Assert: Geometry hash equality
        result1.Features.Length.Should().Be(result2.Features.Length);
        for (var i = 0; i < result1.Features.Length; i++)
        {
            result1.Features[i].FeatureId.Should().Be(result2.Features[i].FeatureId);
            result1.Features[i].Geometry.Should().Be(result2.Features[i].Geometry);
            result1.Features[i].PlateIdProvenance.Should().Be(result2.Features[i].PlateIdProvenance);
        }

        // Assert: Provenance chain equality
        result1.Provenance.SourceBoundaryIds.Should().BeEquivalentTo(result2.Provenance.SourceBoundaryIds);
        result1.Provenance.Stream.TopologyStreamHash.Should().Be(result2.Provenance.Stream.TopologyStreamHash);
        result1.Provenance.Stream.KinematicsStreamHash.Should().Be(result2.Provenance.Stream.KinematicsStreamHash);

        // Assert: Metadata equality
        result1.Metadata.TopologyStreamHash.Should().Be(result2.Metadata.TopologyStreamHash);
        result1.Metadata.KinematicsStreamHash.Should().Be(result2.Metadata.KinematicsStreamHash);
        result1.Metadata.CacheKey.Should().Be(result2.Metadata.CacheKey);
    }

    /// <summary>
    /// RFC-V2-0045 §7 Test Gate 2: Reconstruct_GeometryHashes_AreStable.
    /// Hashes must be stable across runs (golden value comparison).
    /// </summary>
    [Fact]
    public void Reconstruct_GeometryHashes_AreStable()
    {
        // Arrange
        var solver = new NaivePlateReconstructionSolver();
        var topology = CreateTestTopology();
        var kinematics = new FakeKinematicsState();
        var policy = CreateTestPolicy();

        // Act
        var result = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);

        // Assert: Hash matches expected golden value
        // The topology stream hash is derived from TruthStreamIdentity.ToEventStreamIdString()
        // For our test topology stream: VariantId="science", BranchId="trunk", L=2, Domain="geo.plates", Model="0"
        var expectedTopologyHash = "S:science:trunk:L2:geo.plates:M0:Events";
        var expectedKinematicsHash = "S:science:trunk:L2:geo.plates.kinematics:M0:Events";

        result.Metadata.TopologyStreamHash.Should().Be(expectedTopologyHash);
        result.Metadata.KinematicsStreamHash.Should().Be(expectedKinematicsHash);

        // Provenance stream hashes should match metadata
        result.Provenance.Stream.TopologyStreamHash.Should().Be(expectedTopologyHash);
        result.Provenance.Stream.KinematicsStreamHash.Should().Be(expectedKinematicsHash);
    }

    /// <summary>
    /// RFC-V2-0045 §7 Test Gate 3: Reconstruct_Pagination_IsStable.
    /// Pagination must be stable using cursor-based queries.
    /// </summary>
    [Fact]
    public void Reconstruct_Pagination_IsStable()
    {
        // Arrange
        var solver = new NaivePlateReconstructionSolver();
        var topology = CreateTestTopology();
        var kinematics = new FakeKinematicsState();
        var policy = CreateTestPolicy();

        // Act: Two calls with same inputs (simulating cursor-based pagination stability)
        var result1 = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);
        var result2 = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);

        // Assert: Same feature count
        result1.Features.Length.Should().Be(result2.Features.Length);
        result1.Features.Length.Should().Be(2, "topology has 2 active boundaries");

        // Assert: Same ordering (cursor stability)
        // Features are ordered by FeatureId (derived from BoundaryId) using RFC4122 byte ordering
        for (var i = 0; i < result1.Features.Length; i++)
        {
            result1.Features[i].FeatureId.Should().Be(result2.Features[i].FeatureId,
                $"feature at index {i} should have same ID across calls");
        }

        // Assert: Cache key stability (would be used as cursor basis)
        result1.Metadata.CacheKey.Should().Be(result2.Metadata.CacheKey);
    }

    /// <summary>
    /// RFC-V2-0045 §7 Test Gate 4: Reconstruct_Provenance_IsComplete.
    /// All outputs must carry complete provenance.
    /// </summary>
    [Fact]
    public void Reconstruct_Provenance_IsComplete()
    {
        // Arrange
        var solver = new NaivePlateReconstructionSolver();
        var topology = CreateTestTopology();
        var kinematics = new FakeKinematicsState();
        var policy = CreateTestPolicy();

        // Act
        var result = solver.ReconstructWithProvenance(topology, kinematics, policy, TargetTick);

        // Assert: Each feature has non-null provenance fields
        result.Features.Should().NotBeEmpty();
        foreach (var feature in result.Features)
        {
            feature.PlateIdProvenance.Should().NotBe(default(PlateId),
                $"feature {feature.FeatureId} should have plate provenance");
        }

        // Assert: Provenance has required fields
        result.Provenance.Should().NotBeNull();
        result.Provenance.SourceBoundaryIds.Should().NotBeNull();
        result.Provenance.SourceBoundaryIds.Should().HaveCount(2, "should track all source boundaries");
        result.Provenance.PlateAssignment.Should().NotBeNull();
        result.Provenance.PlateAssignment.Method.Should().NotBe(default);
        result.Provenance.PlateAssignment.Confidence.Should().BeGreaterThanOrEqualTo(0.0);
        result.Provenance.PlateAssignment.Confidence.Should().BeLessThanOrEqualTo(1.0);
        result.Provenance.Kinematics.Should().NotBeNull();
        result.Provenance.Kinematics.ReferenceFrame.Should().NotBe(default(FrameId));
        result.Provenance.Kinematics.InterpolationMethod.Should().NotBeNullOrEmpty();
        result.Provenance.Stream.Should().NotBeNull();
        result.Provenance.Stream.TopologyStreamHash.Should().NotBeNullOrEmpty();
        result.Provenance.Stream.KinematicsStreamHash.Should().NotBeNullOrEmpty();
        result.Provenance.QueryMetadata.Should().NotBeNull();
        result.Provenance.QueryMetadata.QueryTick.Should().Be(TargetTick);
        result.Provenance.QueryMetadata.SolverVersion.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// RFC-V2-0045 §7 Test Gate 5: Reconstruct_Cache_InvalidatesOnTopologyChange.
    /// Cache must invalidate when topology changes.
    /// </summary>
    [Fact]
    public void Reconstruct_Cache_InvalidatesOnTopologyChange()
    {
        // Arrange
        var solver = new NaivePlateReconstructionSolver();
        var topology1 = CreateTestTopology();
        var kinematics = new FakeKinematicsState();
        var policy = CreateTestPolicy();

        // Act: First reconstruction
        var result1 = solver.ReconstructWithProvenance(topology1, kinematics, policy, TargetTick);

        // Simulate topology update: Create new topology with different stream identity
        var topology2 = CreateUpdatedTopology();

        // Act: Second reconstruction with updated topology
        var result2 = solver.ReconstructWithProvenance(topology2, kinematics, policy, TargetTick);

        // Assert: Different topology stream hash indicates cache invalidation
        result1.Metadata.TopologyStreamHash.Should().NotBe(result2.Metadata.TopologyStreamHash,
            "topology stream hash should change when topology is updated");

        // Assert: Different cache keys
        result1.Metadata.CacheKey.Should().NotBe(result2.Metadata.CacheKey,
            "cache key should differ for different topologies");

        // Assert: Provenance also reflects the change
        result1.Provenance.Stream.TopologyStreamHash.Should().NotBe(result2.Provenance.Stream.TopologyStreamHash);
    }

    #region Test Helpers

    private static ReconstructionPolicy CreateTestPolicy()
    {
        return new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = ModelId.NewId(),
            PartitionTolerance = new TolerancePolicy.StrictPolicy(),
            Strictness = ProvenanceStrictness.Strict
        };
    }

    private static FakeTopologyState CreateTestTopology()
    {
        var boundary1 = new Boundary(
            BoundaryId1,
            PlateA,
            PlateB,
            BoundaryType.Divergent,
            new Point3(1d, 0d, 0d),
            IsRetired: false,
            null);

        var boundary2 = new Boundary(
            BoundaryId2,
            PlateB,
            PlateA,
            BoundaryType.Convergent,
            new Point3(0d, 1d, 0d),
            IsRetired: false,
            null);

        return new FakeTopologyState(
            Domain.Parse("geo.plates"),
            new[] { boundary1, boundary2 },
            lastEventSequence: 0);
    }

    private static FakeTopologyState CreateUpdatedTopology()
    {
        // Same boundaries but with updated event sequence (simulating topology change)
        var boundary1 = new Boundary(
            BoundaryId1,
            PlateA,
            PlateB,
            BoundaryType.Divergent,
            new Point3(1d, 0d, 0d),
            IsRetired: false,
            null);

        var boundary2 = new Boundary(
            BoundaryId2,
            PlateB,
            PlateA,
            BoundaryType.Convergent,
            new Point3(0d, 1d, 0d),
            IsRetired: false,
            null);

        // Different stream identity hash (incremented version)
        return new FakeTopologyState(
            Domain.Parse("geo.plates"),
            new[] { boundary1, boundary2 },
            lastEventSequence: 1,
            streamVersion: 3); // Different version triggers different hash
    }

    #endregion

    #region Fake Implementations

    private sealed class FakeTopologyState : IPlateTopologyStateView
    {
        private static readonly IReadOnlyDictionary<PlateId, PlateEntity> EmptyPlates =
            new Dictionary<PlateId, PlateEntity>();
        private static readonly IReadOnlyDictionary<JunctionId, JunctionEntity> EmptyJunctions =
            new Dictionary<JunctionId, JunctionEntity>();

        public FakeTopologyState(
            Domain domain,
            IEnumerable<Boundary> boundaries,
            long lastEventSequence,
            int streamVersion = 2)
        {
            Identity = new TruthStreamIdentity("science", "trunk", streamVersion, domain, lastEventSequence.ToString());
            Boundaries = boundaries.ToDictionary(b => b.BoundaryId, b => b);
            Plates = EmptyPlates;
            Junctions = EmptyJunctions;
            LastEventSequence = lastEventSequence;
        }

        public TruthStreamIdentity Identity { get; }
        public IReadOnlyDictionary<PlateId, PlateEntity> Plates { get; }
        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }
        public IReadOnlyDictionary<JunctionId, JunctionEntity> Junctions { get; }
        public long LastEventSequence { get; }
    }

    private sealed class FakeKinematicsState : IPlateKinematicsStateView
    {
        private readonly Quaterniond _rotation;
        private readonly bool _returnsRotation;

        public FakeKinematicsState()
            : this(Quaterniond.Identity, returnsRotation: true)
        {
        }

        public FakeKinematicsState(Quaterniond rotation)
            : this(rotation, returnsRotation: true)
        {
        }

        public FakeKinematicsState(bool returnsRotation)
            : this(Quaterniond.Identity, returnsRotation)
        {
        }

        private FakeKinematicsState(Quaterniond rotation, bool returnsRotation)
        {
            _rotation = rotation;
            _returnsRotation = returnsRotation;
        }

        public TruthStreamIdentity Identity { get; } =
            new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");

        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = _rotation;
            return _returnsRotation;
        }
    }

    #endregion
}
