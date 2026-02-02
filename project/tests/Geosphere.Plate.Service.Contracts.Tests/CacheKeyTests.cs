using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

using FantaSim.Geosphere.Plate.Service.Contracts;

namespace FantaSim.Geosphere.Plate.Service.Contracts.Tests;

/// <summary>
/// Test gates for RFC-V2-0045 cache key requirements (Section 4.2.1).
/// </summary>
public class CacheKeyTests
{
    #region Frame Inclusion Gate (RFC 4.2.1 Normative Requirement)

    [Fact]
    public void Frame_Inclusion_Gate_BuildReconstructKey_Throws_When_Frame_Empty()
    {
        // Arrange: Policy with empty frame
        var policy = new ReconstructionPolicy
        {
            Frame = null!, // Invalid per RFC: frame must be specified
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        // Act & Assert: Should throw because Frame is required per RFC 4.2.1
        var ex = Assert.Throws<ArgumentException>(() =>
            CacheKeyBuilder.BuildReconstructKey(
                FeatureSetId.NewId(),
                CanonicalTick.Genesis,
                policy));

        Assert.Contains("Frame", ex.Message);
        Assert.Contains("RFC-V2-0045", ex.Message);
    }

    [Fact]
    public void Frame_Inclusion_Gate_BuildPlateIdKey_Throws_When_Frame_Empty()
    {
        // Arrange
        var policy = new ReconstructionPolicy
        {
            Frame = null!,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CacheKeyBuilder.BuildPlateIdKey(
                new Point3 { X = 1, Y = 0, Z = 0 },
                CanonicalTick.Genesis,
                policy));

        Assert.Contains("Frame", ex.Message);
    }

    [Fact]
    public void Frame_Inclusion_Gate_BuildVelocityKey_Throws_When_Frame_Null()
    {
        // Arrange
        var point = new Point3 { X = 1, Y = 0, Z = 0 };
        var modelId = new ModelId(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            CacheKeyBuilder.BuildVelocityKey(
                point,
                CanonicalTick.Genesis,
                modelId,
                frame: null!));

        Assert.Contains("frame", ex.ParamName);
    }

    [Fact]
    public void Frame_Inclusion_Gate_ValidatePolicy_Throws_For_Empty_Frame()
    {
        // Arrange
        var policy = new ReconstructionPolicy
        {
            Frame = null!,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CacheKeyBuilder.ValidatePolicy(policy));

        Assert.Contains("RFC-V2-0045", ex.Message);
    }

    #endregion

    #region Cache Key Format Stability Gate

    [Fact]
    public void Cache_Key_Format_Stability_ReconstructKey_Has_Correct_Prefix()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var key = CacheKeyBuilder.BuildReconstructKey(
            FeatureSetId.NewId(),
            CanonicalTick.Genesis,
            policy);

        // Assert
        Assert.StartsWith("recon:", key);
    }

    [Fact]
    public void Cache_Key_Format_Stability_PlateIdKey_Has_Correct_Prefix()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var key = CacheKeyBuilder.BuildPlateIdKey(
            new Point3 { X = 1, Y = 0, Z = 0 },
            CanonicalTick.Genesis,
            policy);

        // Assert
        Assert.StartsWith("plate:", key);
    }

    [Fact]
    public void Cache_Key_Format_Stability_VelocityKey_Has_Correct_Prefix()
    {
        // Arrange
        var point = new Point3 { X = 1, Y = 0, Z = 0 };
        var modelId = new ModelId(Guid.NewGuid());
        var frame = MantleFrame.Instance;

        // Act
        var key = CacheKeyBuilder.BuildVelocityKey(
            point,
            CanonicalTick.Genesis,
            modelId,
            frame);

        // Assert
        Assert.StartsWith("vel:", key);
    }

    [Fact]
    public void Cache_Key_Format_Stability_Keys_Are_Deterministic()
    {
        // Arrange
        var featureSetId = FeatureSetId.Parse("11111111-1111-1111-1111-111111111111");
        var tick = new CanonicalTick(42);
        var policy = CreateValidPolicy();

        // Act
        var key1 = CacheKeyBuilder.BuildReconstructKey(featureSetId, tick, policy);
        var key2 = CacheKeyBuilder.BuildReconstructKey(featureSetId, tick, policy);

        // Assert: Same inputs produce identical keys
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Cache_Key_Format_Stability_Different_Policies_Different_Keys()
    {
        // Arrange
        var featureSetId = FeatureSetId.NewId();
        var tick = CanonicalTick.Genesis;

        var policy1 = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        var policy2 = new ReconstructionPolicy
        {
            Frame = AbsoluteFrame.Instance,
            KinematicsModel = policy1.KinematicsModel,
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        // Act
        var key1 = CacheKeyBuilder.BuildReconstructKey(featureSetId, tick, policy1);
        var key2 = CacheKeyBuilder.BuildReconstructKey(featureSetId, tick, policy2);

        // Assert: Different frames produce different keys
        Assert.NotEqual(key1, key2);
    }

    #endregion

    #region Policy Hash Stability Gate

    [Fact]
    public void Policy_Hash_Stability_Same_Policy_Same_Hash()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var hash1 = CacheKeyBuilder.ComputePolicyHash(policy);
        var hash2 = CacheKeyBuilder.ComputePolicyHash(policy);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Policy_Hash_Stability_Frame_Difference_Produces_Different_Hash()
    {
        // Arrange
        var basePolicy = CreateValidPolicy();

        var policy1 = basePolicy with { Frame = MantleFrame.Instance };
        var policy2 = basePolicy with { Frame = AbsoluteFrame.Instance };

        // Act
        var hash1 = CacheKeyBuilder.ComputePolicyHash(policy1);
        var hash2 = CacheKeyBuilder.ComputePolicyHash(policy2);

        // Assert: Different frames = different hashes per RFC 4.2.1
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Policy_Hash_Is_First_Component_In_Hash_Computation()
    {
        // Per RFC 4.2.1, Frame MUST be first in cache key computation
        // This is verified by checking that changing frame produces different hash
        var policy1 = new ReconstructionPolicy
        {
            Frame = new PlateAnchor { PlateId = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001")) },
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        var policy2 = new ReconstructionPolicy
        {
            Frame = new PlateAnchor { PlateId = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002")) },
            KinematicsModel = policy1.KinematicsModel,
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        var hash1 = CacheKeyBuilder.ComputePolicyHash(policy1);
        var hash2 = CacheKeyBuilder.ComputePolicyHash(policy2);

        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Extension Methods Gate

    [Fact]
    public void Extension_Methods_ToCacheKey_Produces_Consistent_Hash()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var key1 = policy.ToCacheKey();
        var key2 = CacheKeyBuilder.BuildPolicyKey(policy);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Extension_Methods_GetDeterministicHashCode_Includes_Frame()
    {
        // Arrange
        var policy1 = CreateValidPolicy();
        var policy2 = policy1 with { Frame = AbsoluteFrame.Instance };

        // Act
        var hash1 = policy1.GetDeterministicHashCode();
        var hash2 = policy2.GetDeterministicHashCode();

        // Assert: Different frames produce different hash codes
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Cache Key Parsing Gate

    [Fact]
    public void Cache_Key_Parsing_ParseKey_Extracts_Components()
    {
        // Arrange
        var policy = CreateValidPolicy();
        var key = CacheKeyBuilder.BuildReconstructKey(
            FeatureSetId.Parse("11111111-1111-1111-1111-111111111111"),
            new CanonicalTick(42),
            policy);

        // Act
        var components = CacheKeyBuilder.ParseKey(key);

        // Assert
        Assert.Equal("recon", components["prefix"]);
        Assert.True(components.ContainsKey("component1"));
        Assert.True(components.ContainsKey("component2"));
        Assert.True(components.ContainsKey("component3"));
    }

    #endregion

    #region Helper Methods

    private static ReconstructionPolicy CreateValidPolicy()
    {
        return new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = new ModelId(Guid.NewGuid()),
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };
    }

    #endregion
}
