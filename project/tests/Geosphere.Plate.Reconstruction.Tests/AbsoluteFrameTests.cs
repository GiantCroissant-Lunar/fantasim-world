using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.TruePolarWander;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Service.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// Tests for RFC-V2-0046 Section 3.3: AbsoluteFrame with True Polar Wander (TPW) integration.
/// </summary>
public class AbsoluteFrameTests
{
    private readonly FrameService _sut;
    private readonly StubKinematicsView _kinematics;
    private readonly StubTopologyView _topology;

    public AbsoluteFrameTests()
    {
        _sut = new FrameService();
        _kinematics = new StubKinematicsView();
        _topology = new StubTopologyView();
    }

    #region Test Case 1: No TPW model returns Identity

    [Fact]
    public void GetFrameTransform_AbsoluteToMantle_NoTpwModel_ReturnsIdentity()
    {
        // Per RFC: Without TPW data, AbsoluteFrame MUST behave as identity transform
        var result = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel: null);

        result.Transform.Should().Be(FiniteRotation.Identity);
        result.Validity.Should().Be(TransformValidity.Valid);
    }

    [Fact]
    public void GetFrameTransform_MantleToAbsolute_NoTpwModel_ReturnsIdentity()
    {
        var result = _sut.GetFrameTransform(
            MantleFrame.Instance,
            AbsoluteFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel: null);

        result.Transform.Should().Be(FiniteRotation.Identity);
        result.Validity.Should().Be(TransformValidity.Valid);
    }

    #endregion

    #region Test Case 2: With TPW model returns TPW rotation

    [Fact]
    public void GetFrameTransform_AbsoluteToMantle_WithTpwModel_ReturnsTpwRotation()
    {
        // Per RFC: If TPW data is available, it MUST be applied consistently
        var expectedRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.15);
        var tpwModel = new StubTpwModel(expectedRotation);

        var result = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        result.Transform.Orientation.Should().Be(expectedRotation.Orientation);
        result.Validity.Should().Be(TransformValidity.Valid);
    }

    [Fact]
    public void GetFrameTransform_MantleToAbsolute_WithTpwModel_ReturnsInverseTpwRotation()
    {
        var tpwRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.15);
        var tpwModel = new StubTpwModel(tpwRotation);

        var result = _sut.GetFrameTransform(
            MantleFrame.Instance,
            AbsoluteFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        // Mantle->Absolute = (Mantle->Mantle) ∘ (Absolute->Mantle)^-1 = Identity ∘ TPW^-1 = TPW^-1
        var expectedInverse = tpwRotation.Inverted();
        result.Transform.Orientation.X.Should().BeApproximately(expectedInverse.Orientation.X, 1e-10);
        result.Transform.Orientation.Y.Should().BeApproximately(expectedInverse.Orientation.Y, 1e-10);
        result.Transform.Orientation.Z.Should().BeApproximately(expectedInverse.Orientation.Z, 1e-10);
        result.Transform.Orientation.W.Should().BeApproximately(expectedInverse.Orientation.W, 1e-10);
    }

    #endregion

    #region Test Case 3: TPW model returns Identity

    [Fact]
    public void GetFrameTransform_AbsoluteToMantle_TpwModelReturnsIdentity_ReturnsIdentity()
    {
        var tpwModel = new StubTpwModel(FiniteRotation.Identity);

        var result = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        result.Transform.Should().Be(FiniteRotation.Identity);
        result.Validity.Should().Be(TransformValidity.Valid);
    }

    #endregion

    #region Test Case 4: Multiple ticks get different rotations

    [Fact]
    public void GetFrameTransform_AbsoluteToMantle_DifferentTicks_ReturnsDifferentRotations()
    {
        // Per RFC: TPW rotation should be time-dependent
        var tick1 = new CanonicalTick(0);
        var tick2 = new CanonicalTick(100);
        var tick3 = new CanonicalTick(200);

        var rotation1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotation2 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var rotation3 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.3);

        var tpwModel = new TickDependentTpwModel(new Dictionary<long, FiniteRotation>
        {
            { tick1.Value, rotation1 },
            { tick2.Value, rotation2 },
            { tick3.Value, rotation3 }
        });

        var result1 = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            tick1,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        var result2 = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            tick2,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        var result3 = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            tick3,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology,
            tpwModel);

        result1.Transform.Orientation.Should().Be(rotation1.Orientation);
        result2.Transform.Orientation.Should().Be(rotation2.Orientation);
        result3.Transform.Orientation.Should().Be(rotation3.Orientation);

        // Verify they are actually different
        result1.Transform.Angle.Should().BeApproximately(0.1, 1e-10);
        result2.Transform.Angle.Should().BeApproximately(0.2, 1e-10);
        result3.Transform.Angle.Should().BeApproximately(0.3, 1e-10);
    }

    #endregion

    #region Provenance Tests

    [Fact]
    public void GetFrameTransform_AbsoluteToMantle_HasCorrectProvenance()
    {
        var result = _sut.GetFrameTransform(
            AbsoluteFrame.Instance,
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology);

        result.Provenance.FromFrame.Should().Be(AbsoluteFrame.Instance);
        result.Provenance.ToFrame.Should().Be(MantleFrame.Instance);
        result.Provenance.EvaluationChain.Should().BeEmpty();
    }

    #endregion

    #region Stub classes

    private class StubTpwModel : ITruePolarWanderModel
    {
        private readonly FiniteRotation _rotation;

        public StubTpwModel(FiniteRotation rotation)
        {
            _rotation = rotation;
        }

        public FiniteRotation GetRotationAt(CanonicalTick tick)
        {
            return _rotation;
        }
    }

    private class TickDependentTpwModel : ITruePolarWanderModel
    {
        private readonly Dictionary<long, FiniteRotation> _rotations;

        public TickDependentTpwModel(Dictionary<long, FiniteRotation> rotations)
        {
            _rotations = rotations;
        }

        public FiniteRotation GetRotationAt(CanonicalTick tick)
        {
            return _rotations.TryGetValue(tick.Value, out var rotation)
                ? rotation
                : FiniteRotation.Identity;
        }
    }

    private class StubKinematicsView : IPlateKinematicsStateView
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

    private class StubTopologyView : IPlateTopologyStateView
    {
        public TruthStreamIdentity Identity { get; } = new(
            VariantId: "test",
            BranchId: "test",
            LLevel: 0,
            Domain: Domain.GeoPlatesTopology,
            Model: "M0");

        public Dictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate> PlatesDict { get; }
            = new Dictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate>();

        public IReadOnlyDictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate> Plates => PlatesDict;

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }
            = new Dictionary<BoundaryId, Boundary>();

        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; }
            = new Dictionary<JunctionId, Junction>();

        public Dictionary<PlateId, double> PlateAreas { get; } = new();

        public long LastEventSequence => 0;

        public bool TryGetPlateArea(PlateId plateId, out double area)
        {
            return PlateAreas.TryGetValue(plateId, out area);
        }
    }

    #endregion
}
