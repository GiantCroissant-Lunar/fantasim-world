using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FrameId = FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Cache;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public class PolicyTests
{
    private ReconstructionPolicy CreateTestPolicy()
    {
        return new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = ModelId.NewId(),
            PartitionTolerance = new TolerancePolicy.StrictPolicy(),
            Strictness = ProvenanceStrictness.Strict
        };
    }

    [Fact]
    public void PolicyHash_IsStable()
    {
        var policy = CreateTestPolicy();
        var hash1 = PolicyCacheKey.ComputeHash(policy);
        var hash2 = PolicyCacheKey.ComputeHash(policy);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void PolicyEquality_SameValuesAreEqual()
    {
        var modelId = ModelId.NewId();
        var p1 = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = modelId,
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };
        var p2 = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = modelId,
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        };

        p1.Should().Be(p2);
        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }

    [Fact]
    public void PolicyEquality_DifferentValuesAreNotEqual()
    {
        var p1 = CreateTestPolicy();
        var p2 = p1 with { Strictness = ProvenanceStrictness.Lenient };

        p1.Should().NotBe(p2);
    }

    [Fact]
    public void PolicyValidation_RejectsMissingRequiredFields()
    {
        var invalidPolicy = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = ModelId.Default, // Empty/Default might be rejected if we enforce it?
            PartitionTolerance = new TolerancePolicy.StrictPolicy()
        } with { KinematicsModel = ModelId.Default }; // Explicitly default

        // Validator checks IsEmpty for KinematicsModel
        // ModelId.Default is empty.

        var result = ReconstructionPolicyValidator.ValidateForQuery(
            invalidPolicy, QueryType.Reconstruct);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("KinematicsModel is required.");
    }

    [Fact]
    public void PolicyValidation_AcceptsValidPolicy()
    {
        var policy = CreateTestPolicy();
        var result = ReconstructionPolicyValidator.ValidateForQuery(policy, QueryType.Reconstruct);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PolicyValidation_BoundaryAnalytics_RequiresBoundarySampling()
    {
        var policy = CreateTestPolicy(); // Has no boundary sampling by default

        var result = ReconstructionPolicyValidator.ValidateForQuery(policy, QueryType.BoundaryAnalytics);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("BoundarySampling is required for BoundaryAnalytics queries.");
    }
}
