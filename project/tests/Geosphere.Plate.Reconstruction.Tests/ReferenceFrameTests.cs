using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.World.Plates;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public class ReferenceFrameTests
{
    private readonly FrameService _sut;
    private readonly StubKinematicsView _kinematics;
    private readonly StubTopologyView _topology;

    public ReferenceFrameTests()
    {
        _sut = new FrameService();
        _kinematics = new StubKinematicsView();
        _topology = new StubTopologyView();
    }

    [Fact]
    public void GetFrameTransform_MantleToMantle_ReturnsIdentity()
    {
        var result = _sut.GetFrameTransform(
            MantleFrame.Instance,
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology);

        result.Transform.Should().Be(FiniteRotation.Identity);
    }

    [Fact]
    public void GetFrameTransform_AnchorToMantle_ReturnsAnchorRotation()
    {
        // Setup
        var plateId = new PlateId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        _kinematics.Rotations[plateId] = rotation.Orientation;

        // T_anchor_mantle = Rotation(Anchor)
        var result = _sut.GetFrameTransform(
            new PlateAnchor { PlateId = plateId },
            MantleFrame.Instance,
            CanonicalTick.Genesis,
            new ModelId(Guid.NewGuid()),
            _kinematics,
            _topology);

        // We compare Quaternions roughly or check axis/angle
        result.Transform.Orientation.Should().Be(rotation.Orientation);
    }

    [Fact]
    public void ValidateFrameDefinition_EmptyChain_Throws()
    {
        var def = new FrameDefinition
        {
            Name = "Empty",
            Chain = Array.Empty<FrameChainLink>()
        };

        Assert.Throws<ArgumentException>(() => _sut.ValidateFrameDefinition(def));
    }

    // Stub classes
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

        public IReadOnlyDictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate> Plates { get; set; }
            = new Dictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate>();

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }
            = new Dictionary<BoundaryId, Boundary>();

        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; }
            = new Dictionary<JunctionId, Junction>();

        public long LastEventSequence => 0;
    }
}
