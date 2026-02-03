using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// RFC-V2-0046 Section 5.1: Tests for IFrameAwareKinematicsView interface and implementation.
/// </summary>
public class FrameAwareKinematicsViewTests
{
    private readonly FrameService _frameService;
    private readonly TestKinematicsView _kinematics;
    private readonly TestTopologyView _topology;
    private readonly ModelId _modelId;
    private readonly FrameAwareKinematicsView _sut;

    public FrameAwareKinematicsViewTests()
    {
        _frameService = new FrameService();
        _kinematics = new TestKinematicsView();
        _topology = new TestTopologyView();
        _modelId = new ModelId(Guid.NewGuid());
        _sut = new FrameAwareKinematicsView(_kinematics, _topology, _frameService, _modelId);
    }

    #region GetRotationInFrame - MantleFrame

    [Fact]
    public void GetRotationInFrame_MantleFrame_ReturnsBaseRotation()
    {
        // Arrange - When asking for rotation in MantleFrame, we get the base rotation
        // because MantleFrame is the canonical frame where rotations are stored
        var plateId = new PlateId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        _kinematics.Rotations[plateId] = rotation.Orientation;
        _topology.AddPlate(plateId, 1.0);

        // Act
        var result = _sut.GetRotationInFrame(plateId, CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert - rotation in mantle frame equals base rotation
        // (mantle frame transform to itself is identity)
        result.Should().NotBeNull();
        AssertRotationsApproximatelyEqual(result!.Value, rotation);
    }

    [Fact]
    public void GetRotationInFrame_MantleFrame_TwoPlates_ReturnsAdjustedRotation()
    {
        // Arrange - two plates with different rotations
        // Mantle frame net rotation = area-weighted average
        // With equal areas, net = average of rotations
        var plateA = new PlateId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var plateB = new PlateId(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        // Plate A: +0.2 rad, Plate B: -0.2 rad around Z
        // Net rotation = 0, so mantle transform = identity
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.2);
        _kinematics.Rotations[plateA] = rotationA.Orientation;
        _kinematics.Rotations[plateB] = rotationB.Orientation;

        _topology.AddPlate(plateA, 1.0);
        _topology.AddPlate(plateB, 1.0);

        // Act
        var resultA = _sut.GetRotationInFrame(plateA, CanonicalTick.Genesis, MantleFrame.Instance);
        var resultB = _sut.GetRotationInFrame(plateB, CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert - with symmetric rotations, mantle frame has zero net, so rotations unchanged
        resultA.Should().NotBeNull();
        resultB.Should().NotBeNull();
        AssertRotationsApproximatelyEqual(resultA!.Value, rotationA);
        AssertRotationsApproximatelyEqual(resultB!.Value, rotationB);
    }

    [Fact]
    public void GetRotationInFrame_MantleFrame_WithIdentityRotation_ReturnsIdentity()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        _kinematics.Rotations[plateId] = Quaterniond.Identity;
        _topology.AddPlate(plateId, 1.0);

        // Act
        var result = _sut.GetRotationInFrame(plateId, CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().NotBeNull();
        result!.Value.IsIdentity.Should().BeTrue();
    }

    #endregion

    #region GetRotationInFrame - PlateAnchor

    [Fact]
    public void GetRotationInFrame_PlateAnchor_ReturnsRelativeRotation()
    {
        // Arrange - Use symmetric rotations so mantle frame net = 0 (identity transform)
        // This isolates the plate anchor logic from mantle frame calculation
        var plateA = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateB = new PlateId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        // Plate A: +0.15 rad, Plate B: -0.15 rad around Z (symmetric, net = 0)
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.15);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.15);
        _kinematics.Rotations[plateA] = rotationA.Orientation;
        _kinematics.Rotations[plateB] = rotationB.Orientation;

        _topology.AddPlate(plateA, 1.0);
        _topology.AddPlate(plateB, 1.0);

        // Act - Get plate A rotation relative to plate B
        var result = _sut.GetRotationInFrame(plateA, CanonicalTick.Genesis, new PlateAnchor { PlateId = plateB });

        // Assert - Relative rotation should be 0.30 rad (0.15 - (-0.15) = 0.30)
        result.Should().NotBeNull();
        result!.Value.Angle.Should().BeApproximately(0.30, 1e-6);
    }

    [Fact]
    public void GetRotationInFrame_PlateAnchor_SamePlate_ReturnsIdentity()
    {
        // Arrange - Use symmetric rotations to neutralize mantle frame calculation
        var plateId = new PlateId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var otherPlate = new PlateId(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.5);
        var oppositeRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.5);
        _kinematics.Rotations[plateId] = rotation.Orientation;
        _kinematics.Rotations[otherPlate] = oppositeRotation.Orientation;

        _topology.AddPlate(plateId, 1.0);
        _topology.AddPlate(otherPlate, 1.0);

        // Act - Get plate rotation relative to itself
        var result = _sut.GetRotationInFrame(plateId, CanonicalTick.Genesis, new PlateAnchor { PlateId = plateId });

        // Assert - Should be identity (plate relative to itself)
        result.Should().NotBeNull();
        result!.Value.IsIdentity.Should().BeTrue();
    }

    #endregion

    #region GetRotationInFrame - AbsoluteFrame

    [Fact]
    public void GetRotationInFrame_AbsoluteFrame_ReturnsRotation()
    {
        // Arrange - Use symmetric rotations to neutralize mantle frame
        // Without TPW model, AbsoluteFrame is identical to MantleFrame
        var plateId = new PlateId(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var otherPlate = new PlateId(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.3);
        var oppositeRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitX, -0.3);
        _kinematics.Rotations[plateId] = rotation.Orientation;
        _kinematics.Rotations[otherPlate] = oppositeRotation.Orientation;

        _topology.AddPlate(plateId, 1.0);
        _topology.AddPlate(otherPlate, 1.0);

        // Act
        var result = _sut.GetRotationInFrame(plateId, CanonicalTick.Genesis, AbsoluteFrame.Instance);

        // Assert - With symmetric plates, mantle/absolute frame = identity, rotation preserved
        result.Should().NotBeNull();
        AssertRotationsApproximatelyEqual(result!.Value, rotation);
    }

    #endregion

    #region GetRotationInFrame - Unknown Plate

    [Fact]
    public void GetRotationInFrame_UnknownPlate_ReturnsNull()
    {
        // Arrange
        var unknownPlate = new PlateId(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        // Don't add this plate to kinematics

        // Act
        var result = _sut.GetRotationInFrame(unknownPlate, CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllRotationsInFrame

    [Fact]
    public void GetAllRotationsInFrame_ReturnsAllPlatesInFrame()
    {
        // Arrange
        var plate1 = new PlateId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var plate2 = new PlateId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var plate3 = new PlateId(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var rotation1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotation2 = FiniteRotation.FromAxisAngle(Vector3d.UnitY, 0.2);
        var rotation3 = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.3);

        _kinematics.Rotations[plate1] = rotation1.Orientation;
        _kinematics.Rotations[plate2] = rotation2.Orientation;
        _kinematics.Rotations[plate3] = rotation3.Orientation;

        _topology.AddPlate(plate1, 1.0);
        _topology.AddPlate(plate2, 2.0);
        _topology.AddPlate(plate3, 3.0);

        // Act
        var result = _sut.GetAllRotationsInFrame(CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainKey(plate1);
        result.Should().ContainKey(plate2);
        result.Should().ContainKey(plate3);
    }

    [Fact]
    public void GetAllRotationsInFrame_ExcludesRetiredPlates()
    {
        // Arrange
        var activePlate = new PlateId(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var retiredPlate = new PlateId(Guid.Parse("55555555-5555-5555-5555-555555555555"));

        _kinematics.Rotations[activePlate] = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1).Orientation;
        _kinematics.Rotations[retiredPlate] = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2).Orientation;

        _topology.AddPlate(activePlate, 1.0, isRetired: false);
        _topology.AddPlate(retiredPlate, 1.0, isRetired: true);

        // Act
        var result = _sut.GetAllRotationsInFrame(CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(activePlate);
        result.Should().NotContainKey(retiredPlate);
    }

    [Fact]
    public void GetAllRotationsInFrame_ExcludesPlatesWithoutRotations()
    {
        // Arrange
        var plateWithRotation = new PlateId(Guid.Parse("66666666-6666-6666-6666-666666666666"));
        var plateWithoutRotation = new PlateId(Guid.Parse("77777777-7777-7777-7777-777777777777"));

        _kinematics.Rotations[plateWithRotation] = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1).Orientation;
        // Don't add rotation for plateWithoutRotation

        _topology.AddPlate(plateWithRotation, 1.0);
        _topology.AddPlate(plateWithoutRotation, 1.0);

        // Act
        var result = _sut.GetAllRotationsInFrame(CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(plateWithRotation);
        result.Should().NotContainKey(plateWithoutRotation);
    }

    [Fact]
    public void GetAllRotationsInFrame_EmptyTopology_ReturnsEmptyDictionary()
    {
        // Arrange - Empty topology, no plates

        // Act
        var result = _sut.GetAllRotationsInFrame(CanonicalTick.Genesis, MantleFrame.Instance);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllRotationsInFrame_PlateAnchor_ReturnsRelativeRotations()
    {
        // Arrange - Set up plates with rotations where mantle correction is effectively identity
        // Three plates with equal areas - rotations sum to zero so net = identity
        var anchorPlate = new PlateId(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        var otherPlate = new PlateId(Guid.Parse("99999999-9999-9999-9999-999999999999"));
        var thirdPlate = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Rotations: anchor=+0.1, other=+0.3, third=-0.4 (sum = 0, so net rotation = 0)
        var anchorRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var otherRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.3);
        var thirdRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.4);

        _kinematics.Rotations[anchorPlate] = anchorRotation.Orientation;
        _kinematics.Rotations[otherPlate] = otherRotation.Orientation;
        _kinematics.Rotations[thirdPlate] = thirdRotation.Orientation;

        _topology.AddPlate(anchorPlate, 1.0);
        _topology.AddPlate(otherPlate, 1.0);
        _topology.AddPlate(thirdPlate, 1.0);

        // Act
        var result = _sut.GetAllRotationsInFrame(CanonicalTick.Genesis, new PlateAnchor { PlateId = anchorPlate });

        // Assert
        result.Should().HaveCount(3);

        // With zero net rotation (mantle correction = identity):
        // Anchor plate relative to itself should be approximately identity
        // Note: Small numerical errors (~0.001 rad) occur from MantleFrame's area-weighted
        // quaternion averaging even when rotations theoretically sum to zero
        result[anchorPlate].Angle.Should().BeLessThan(0.01); // ~0.5 degrees tolerance

        // Other plate relative to anchor should be ~0.2 rad (0.3 - 0.1)
        // Tolerance accounts for mantle frame correction numerical artifacts
        result[otherPlate].Angle.Should().BeApproximately(0.2, 0.01);

        // Third plate relative to anchor should be ~0.5 rad (-0.4 - 0.1 = -0.5, angle is absolute)
        result[thirdPlate].Angle.Should().BeApproximately(0.5, 0.01);
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullKinematics_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FrameAwareKinematicsView(null!, _topology, _frameService, _modelId));
    }

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FrameAwareKinematicsView(_kinematics, null!, _frameService, _modelId));
    }

    [Fact]
    public void Constructor_NullFrameService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FrameAwareKinematicsView(_kinematics, _topology, null!, _modelId));
    }

    #endregion

    #region CustomFrame Tests

    [Fact]
    public void GetRotationInFrame_CustomFrame_ReturnsTransformedRotation()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        _kinematics.Rotations[plateId] = rotation.Orientation;
        _topology.AddPlate(plateId, 1.0);

        var customFrameTransform = FiniteRotation.FromAxisAngle(Vector3d.UnitY, 0.1);
        var customFrame = new CustomFrame
        {
            Definition = new FrameDefinition
            {
                Name = "TestFrame",
                Chain = new[]
                {
                    new FrameChainLink
                    {
                        BaseFrame = MantleFrame.Instance,
                        Transform = customFrameTransform
                    }
                }
            }
        };

        // Act
        var result = _sut.GetRotationInFrame(plateId, CanonicalTick.Genesis, customFrame);

        // Assert
        result.Should().NotBeNull();
        // The result should be different from the base rotation due to the custom frame transform
        result!.Value.Should().NotBe(rotation);
    }

    #endregion

    #region Helpers

    private static void AssertRotationsApproximatelyEqual(FiniteRotation actual, FiniteRotation expected, double tolerance = 1e-9)
    {
        actual.Angle.Should().BeApproximately(expected.Angle, tolerance);
        if (actual.Angle > 1e-9)
        {
            // Compare axes only if there's a meaningful rotation
            var dotProduct = Math.Abs(
                actual.Axis.X * expected.Axis.X +
                actual.Axis.Y * expected.Axis.Y +
                actual.Axis.Z * expected.Axis.Z);
            dotProduct.Should().BeApproximately(1.0, tolerance);
        }
    }

    #endregion

    #region Test Doubles

    private class TestKinematicsView : IPlateKinematicsStateView
    {
        public Dictionary<PlateId, Quaterniond> Rotations { get; } = new();

        public TruthStreamIdentity Identity { get; } = new(
            VariantId: "test",
            BranchId: "test",
            LLevel: 0,
            Domain: Domain.GeoPlatesKinematics,
            Model: "M0");

        public long LastEventSequence => 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            return Rotations.TryGetValue(plateId, out rotation);
        }
    }

    private class TestTopologyView : IPlateTopologyStateView
    {
        public TruthStreamIdentity Identity { get; } = new(
            VariantId: "test",
            BranchId: "test",
            LLevel: 0,
            Domain: Domain.GeoPlatesTopology,
            Model: "M0");

        private readonly Dictionary<PlateId, Topology.Contracts.Entities.Plate> _plates = new();
        private readonly Dictionary<PlateId, double> _plateAreas = new();

        public IReadOnlyDictionary<PlateId, Topology.Contracts.Entities.Plate> Plates => _plates;

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }
            = new Dictionary<BoundaryId, Boundary>();

        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; }
            = new Dictionary<JunctionId, Junction>();

        public long LastEventSequence => 0;

        public void AddPlate(PlateId plateId, double area, bool isRetired = false)
        {
            _plates[plateId] = new Topology.Contracts.Entities.Plate(
                PlateId: plateId,
                IsRetired: isRetired,
                RetirementReason: null
            );
            _plateAreas[plateId] = area;
        }

        public bool TryGetPlateArea(PlateId plateId, out double area)
        {
            return _plateAreas.TryGetValue(plateId, out area);
        }
    }

    #endregion
}
