using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// Tests for RFC-V2-0046 Section 6.1: Frame chain canonicalization.
/// </summary>
public class FrameChainCanonicalizerTests
{
    #region Test Helpers

    private static FrameDefinition CreateDefinition(string name, IReadOnlyList<FrameChainLink> chain)
        => new() { Name = name, Chain = chain };

    private static FrameChainLink CreateLink(
        ReferenceFrameId baseFrame,
        FiniteRotation transform,
        CanonicalTickRange? validityRange = null,
        int? sequenceHint = null)
        => new()
        {
            BaseFrame = baseFrame,
            Transform = transform,
            ValidityRange = validityRange,
            SequenceHint = sequenceHint
        };

    private static CanonicalTickRange CreateRange(long start, long end)
        => new() { StartTick = new CanonicalTick(start), EndTick = new CanonicalTick(end) };

    #endregion

    #region CanonicalizeFrameChain Tests

    [Fact]
    public void Canonicalize_IdentityTransformsRemoved_ReturnsNonIdentityOnly()
    {
        // Arrange
        var nonIdentityRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.Identity),
            CreateLink(MantleFrame.Instance, nonIdentityRotation),
            CreateLink(MantleFrame.Instance, FiniteRotation.Identity)
        };
        var definition = CreateDefinition("test", links);

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().HaveCount(1);
        result.Chain[0].Transform.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void Canonicalize_AllIdentity_ReturnsEmptyChain()
    {
        // Arrange
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.Identity),
            CreateLink(MantleFrame.Instance, FiniteRotation.Identity),
            CreateLink(MantleFrame.Instance, FiniteRotation.Identity)
        };
        var definition = CreateDefinition("test", links);

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().BeEmpty();
    }

    [Fact]
    public void Canonicalize_EmptyChain_ReturnsEmptyChain()
    {
        // Arrange
        var definition = CreateDefinition("test", Array.Empty<FrameChainLink>());

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().BeEmpty();
    }

    [Fact]
    public void Canonicalize_NullChain_ReturnsEmptyChain()
    {
        // Arrange
        var definition = new FrameDefinition { Name = "test", Chain = null! };

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().BeEmpty();
    }

    [Fact]
    public void Canonicalize_SingleNonIdentityLink_ReturnsUnchanged()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var links = new[] { CreateLink(MantleFrame.Instance, rotation) };
        var definition = CreateDefinition("test", links);

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().HaveCount(1);
        result.Chain[0].Transform.Orientation.Should().Be(rotation.Orientation);
    }

    [Fact]
    public void Canonicalize_PreservesMetadata()
    {
        // Arrange
        var metadata = new FrameDefinitionMetadata { Description = "Test description", Author = "Test" };
        var links = new[] { CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1)) };
        var definition = new FrameDefinition { Name = "test", Chain = links, Metadata = metadata };

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Metadata.Should().Be(metadata);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Canonicalize_NullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => FrameChainCanonicalizer.CanonicalizeFrameChain(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region MergeConsecutiveTransforms Tests

    [Fact]
    public void MergeConsecutiveTransforms_NoConstants_ReturnsOriginal()
    {
        // Arrange: Links with different base frames cannot be merged
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1)),
            CreateLink(AbsoluteFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2))
        };

        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(links);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void MergeConsecutiveTransforms_EmptyList_ReturnsEmpty()
    {
        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(Array.Empty<FrameChainLink>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void MergeConsecutiveTransforms_SingleLink_ReturnsSingleLink()
    {
        // Arrange
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1))
        };

        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(links);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void Canonicalize_MergesConsecutiveConstants_ReturnsSingleMerged()
    {
        // Arrange: Two constants with same base frame and no validity ranges should merge
        var rotation1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotation2 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, rotation1),
            CreateLink(MantleFrame.Instance, rotation2)
        };
        var definition = CreateDefinition("test", links);

        // Act
        var result = FrameChainCanonicalizer.CanonicalizeFrameChain(definition);

        // Assert
        result.Chain.Should().HaveCount(1);
        // The merged rotation should be approximately rotation1 composed with rotation2
        var expectedComposed = rotation1.Compose(rotation2);
        result.Chain[0].Transform.Angle.Should().BeApproximately(expectedComposed.Angle, 1e-10);
    }

    [Fact]
    public void MergeConsecutiveTransforms_AdjacentRanges_MergesLinks()
    {
        // Arrange: Two links with adjacent validity ranges (end + 1 = next start)
        var rotation1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotation2 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, rotation1, CreateRange(0, 99)),
            CreateLink(MantleFrame.Instance, rotation2, CreateRange(100, 199))
        };

        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(links);

        // Assert
        result.Should().HaveCount(1);
        result[0].ValidityRange!.Value.StartTick.Value.Should().Be(0);
        result[0].ValidityRange!.Value.EndTick.Value.Should().Be(199);
    }

    [Fact]
    public void MergeConsecutiveTransforms_NonAdjacentRanges_DoesNotMerge()
    {
        // Arrange: Two links with non-adjacent ranges (gap between them)
        var rotation1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotation2 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, rotation1, CreateRange(0, 50)),
            CreateLink(MantleFrame.Instance, rotation2, CreateRange(100, 150))
        };

        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(links);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void MergeConsecutiveTransforms_DifferentBaseFrames_DoesNotMerge()
    {
        // Arrange: Same validity but different base frames
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, rotation),
            CreateLink(AbsoluteFrame.Instance, rotation)
        };

        // Act
        var result = FrameChainCanonicalizer.MergeConsecutiveTransforms(links);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region ValidateTemporalConsistency Tests

    [Fact]
    public void ValidateTemporalConsistency_ValidChain_NoException()
    {
        // Arrange: Non-overlapping ranges
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1), CreateRange(0, 50)),
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2), CreateRange(100, 150))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_OverlappingRanges_Throws()
    {
        // Arrange: Overlapping ranges for same base frame
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1), CreateRange(0, 100)),
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2), CreateRange(50, 150))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().Throw<TemporalInconsistencyException>()
            .WithMessage("*overlapping*");
    }

    [Fact]
    public void ValidateTemporalConsistency_AdjacentRanges_NoException()
    {
        // Arrange: Adjacent but non-overlapping ranges
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1), CreateRange(0, 99)),
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2), CreateRange(100, 199))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_EmptyList_NoException()
    {
        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(Array.Empty<FrameChainLink>());
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_SingleLink_NoException()
    {
        // Arrange
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1), CreateRange(0, 100))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_DifferentBaseFrames_OverlappingRanges_NoException()
    {
        // Arrange: Overlapping ranges but different base frames - should NOT throw
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1), CreateRange(0, 100)),
            CreateLink(AbsoluteFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2), CreateRange(50, 150))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_LinksWithoutRanges_NoException()
    {
        // Arrange: Links without validity ranges (always valid)
        var links = new[]
        {
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1)),
            CreateLink(MantleFrame.Instance, FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2))
        };

        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(links);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTemporalConsistency_NullList_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => FrameChainCanonicalizer.ValidateTemporalConsistency(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region IsIdentity Tests

    [Fact]
    public void IsIdentity_IdentityRotation_ReturnsTrue()
    {
        // Act & Assert
        FrameChainCanonicalizer.IsIdentity(FiniteRotation.Identity).Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_NonIdentityRotation_ReturnsFalse()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);

        // Act & Assert
        FrameChainCanonicalizer.IsIdentity(rotation).Should().BeFalse();
    }

    [Fact]
    public void IsIdentity_VerySmallRotation_ReturnsTrue()
    {
        // Arrange: Rotation below the tolerance threshold (1e-12 per FiniteRotation.IsIdentity)
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 1e-14);

        // Act & Assert
        FrameChainCanonicalizer.IsIdentity(rotation).Should().BeTrue();
    }

    #endregion
}
