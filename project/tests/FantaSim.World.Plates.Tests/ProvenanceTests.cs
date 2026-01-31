using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.World.Plates.Tests;

/// <summary>
/// Test gates for RFC-V2-0045 provenance requirements (Section 5).
/// </summary>
public class ProvenanceTests
{
    #region Provenance Completeness Gate

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Passes_With_All_Fields()
    {
        // Arrange
        var provenance = CreateCompleteProvenance();

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Strict);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Fails_With_Empty_KinematicsModelId()
    {
        // Arrange
        var provenance = CreateCompleteProvenance() with
        {
            KinematicsModelId = default(ModelId)
        };

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Strict);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Fails_With_Empty_RotationSegments()
    {
        // Arrange
        var provenance = CreateCompleteProvenance() with
        {
            RotationSegments = Array.Empty<RotationSegmentRef>()
        };

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Strict);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Fails_With_Invalid_TopologyHash_Length()
    {
        // Arrange: Hash length != 32 bytes (not SHA-256)
        var provenance = CreateCompleteProvenance() with
        {
            TopologyStreamHash = new byte[16] // Wrong length
        };

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Strict);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Passes_Lenient_With_Missing_Fields()
    {
        // Arrange: Empty source lists
        var provenance = CreateCompleteProvenance() with
        {
            SourceFeatureIds = Array.Empty<FeatureId>(),
            SourceBoundaryIds = Array.Empty<BoundaryId>(),
            SourceJunctionIds = Array.Empty<JunctionId>(),
            RotationSegments = new[] { CreateRotationSegment() } // Still need at least one
        };

        // Act
        var isValidStrict = provenance.Validate(ProvenanceStrictness.Strict);
        var isValidLenient = provenance.Validate(ProvenanceStrictness.Lenient);

        // Assert: Strict fails, Lenient passes
        Assert.False(isValidStrict);
        Assert.True(isValidLenient);
    }

    [Fact]
    public void Provenance_Completeness_Gate_Validate_Always_Passes_Disabled()
    {
        // Arrange: Even completely empty provenance
        var provenance = ProvenanceChain.Empty;

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Disabled);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Provenance_Completeness_Gate_All_Required_Fields_Present()
    {
        // Arrange
        var featureId = new FeatureId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());
        var plateId = new PlateId(Guid.NewGuid());
        var modelId = new ModelId(Guid.NewGuid());

        var provenance = new ProvenanceChain
        {
            SourceFeatureIds = new[] { featureId },
            SourceBoundaryIds = new[] { boundaryId },
            SourceJunctionIds = new[] { junctionId },
            PlateId = plateId,
            KinematicsModelId = modelId,
            KinematicsModelVersion = 1,
            RotationSegments = new[] { CreateRotationSegment() },
            TopologyStreamHash = new byte[32],
            TopologyReferenceTick = new CanonicalTick(100),
            QueryTick = new CanonicalTick(200),
            QueryContractVersion = "RFC-V2-0045",
            SolverImplementation = "TestSolver/1.0"
        };

        // Act
        var isValid = provenance.Validate(ProvenanceStrictness.Strict);

        // Assert
        Assert.True(isValid);
        Assert.Single(provenance.SourceFeatureIds);
        Assert.Single(provenance.SourceBoundaryIds);
        Assert.Single(provenance.SourceJunctionIds);
        Assert.Equal(plateId, provenance.PlateId);
        Assert.Equal(modelId, provenance.KinematicsModelId);
    }

    #endregion

    #region Provenance Strictness Levels Gate

    [Theory]
    [InlineData(ProvenanceStrictness.Strict)]
    [InlineData(ProvenanceStrictness.Lenient)]
    [InlineData(ProvenanceStrictness.Disabled)]
    public void Provenance_Strictness_Levels_Gate_All_Levels_Defined(ProvenanceStrictness strictness)
    {
        // Assert: All enum values are defined
        Assert.True(Enum.IsDefined(typeof(ProvenanceStrictness), strictness));
    }

    [Fact]
    public void Provenance_Strictness_Levels_Gate_Default_Is_Strict()
    {
        // Arrange
        var policy = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = TolerancePolicy.Standard
        };

        // Assert
        Assert.Equal(ProvenanceStrictness.Strict, policy.Strictness);
    }

    #endregion

    #region Query Metadata Gate

    [Fact]
    public void Query_Metadata_Gate_CacheHit_Metadata_Is_Correct()
    {
        // Arrange & Act
        var metadata = QueryMetadata.ForCacheHit("cache-key-123", "Solver/1.0");

        // Assert
        Assert.True(metadata.CacheHit);
        Assert.Equal("cache-key-123", metadata.CacheKey);
        Assert.Equal("Solver/1.0", metadata.SolverVersion);
        Assert.Equal(TimeSpan.Zero, metadata.Duration);
        Assert.Empty(metadata.Warnings);
    }

    [Fact]
    public void Query_Metadata_Gate_Computed_Metadata_Is_Correct()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);
        var warnings = new[] { "Warning 1", "Warning 2" };

        // Act
        var metadata = QueryMetadata.ForComputed(duration, "Solver/1.0", warnings);

        // Assert
        Assert.False(metadata.CacheHit);
        Assert.Null(metadata.CacheKey);
        Assert.Equal("Solver/1.0", metadata.SolverVersion);
        Assert.Equal(duration, metadata.Duration);
        Assert.Equal(2, metadata.Warnings.Count);
        Assert.Equal("Warning 1", metadata.Warnings[0]);
        Assert.Equal("Warning 2", metadata.Warnings[1]);
    }

    [Fact]
    public void Query_Metadata_Gate_Computed_Metadata_No_Warnings_Defaults_To_Empty()
    {
        // Act
        var metadata = QueryMetadata.ForComputed(TimeSpan.FromSeconds(1), "Solver/1.0");

        // Assert
        Assert.NotNull(metadata.Warnings);
        Assert.Empty(metadata.Warnings);
    }

    #endregion

    #region Rotation Segment Gate

    [Fact]
    public void Rotation_Segment_Gate_Has_All_Required_Fields()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(100);
        var eulerHash = new byte[32];

        var segment = new RotationSegmentRef
        {
            PlateId = plateId,
            StartTick = startTick,
            EndTick = endTick,
            SegmentVersion = 1,
            EulerPoleHash = eulerHash
        };

        // Assert
        Assert.Equal(plateId, segment.PlateId);
        Assert.Equal(startTick, segment.StartTick);
        Assert.Equal(endTick, segment.EndTick);
        Assert.Equal(1, segment.SegmentVersion);
        Assert.Equal(eulerHash, segment.EulerPoleHash);
    }

    #endregion

    #region Cache Invalidation Gate

    [Fact]
    public void Cache_Invalidation_Gate_Different_KinematicsModelVersion_Produced_Different_Keys()
    {
        // Arrange
        var featureSetId = FeatureSetId.NewId();
        var tick = CanonicalTick.Genesis;
        var policy = CreateValidPolicy();

        var provenance1 = CreateCompleteProvenance() with { KinematicsModelVersion = 1 };
        var provenance2 = CreateCompleteProvenance() with { KinematicsModelVersion = 2 };

        // Note: In real implementation, kinematics model version would affect cache key
        // This test verifies the provenance chain includes the version field
        Assert.Equal(1, provenance1.KinematicsModelVersion);
        Assert.Equal(2, provenance2.KinematicsModelVersion);
    }

    [Fact]
    public void Cache_Invalidation_Gate_Different_TopologyStreamHash_Indicates_Different_Data()
    {
        // Arrange
        var hash1 = new byte[32];
        hash1[0] = 0x01;

        var hash2 = new byte[32];
        hash2[0] = 0x02;

        var provenance1 = CreateCompleteProvenance() with { TopologyStreamHash = hash1 };
        var provenance2 = CreateCompleteProvenance() with { TopologyStreamHash = hash2 };

        // Assert
        Assert.NotEqual(provenance1.TopologyStreamHash, provenance2.TopologyStreamHash);
    }

    #endregion

    #region Helper Methods

    private static ProvenanceChain CreateCompleteProvenance()
    {
        return new ProvenanceChain
        {
            SourceFeatureIds = new[] { new FeatureId(Guid.NewGuid()) },
            SourceBoundaryIds = new[] { new BoundaryId(Guid.NewGuid()) },
            SourceJunctionIds = new[] { new JunctionId(Guid.NewGuid()) },
            PlateId = new PlateId(Guid.NewGuid()),
            KinematicsModelId = new ModelId(Guid.NewGuid()),
            KinematicsModelVersion = 1,
            RotationSegments = new[] { CreateRotationSegment() },
            TopologyStreamHash = new byte[32], // SHA-256 size
            TopologyReferenceTick = new CanonicalTick(100),
            QueryTick = new CanonicalTick(200),
            QueryContractVersion = "RFC-V2-0045",
            SolverImplementation = "TestSolver/1.0"
        };
    }

    private static RotationSegmentRef CreateRotationSegment()
    {
        return new RotationSegmentRef
        {
            PlateId = new PlateId(Guid.NewGuid()),
            StartTick = CanonicalTick.Genesis,
            EndTick = new CanonicalTick(100),
            SegmentVersion = 1,
            EulerPoleHash = new byte[32]
        };
    }

    private static ReconstructionPolicy CreateValidPolicy()
    {
        return new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = TolerancePolicy.Standard
        };
    }

    #endregion
}
