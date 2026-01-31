using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.World.Plates;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public class FrameDoctrineTests
{
    private readonly FrameService _sut;
    private readonly StubKinematicsView _kinematics;
    private readonly StubTopologyView _topology;

    public FrameDoctrineTests()
    {
        _sut = new FrameService();
        _kinematics = new StubKinematicsView();
        _topology = new StubTopologyView();
    }

    [Fact]
    public void FrameDoctrine_SameTruthDifferentFrames_ConsistentRelativeMotion()
    {
        // ARRANGE
        // Define two plates with distinct rotations relative to Mantle
        var plateA = new PlateId(Guid.NewGuid()); // e.g. North America
        var plateB = new PlateId(Guid.NewGuid()); // e.g. Pacific

        // R_a: Rotation of Plate A relative to Mantle
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        // R_b: Rotation of Plate B relative to Mantle
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.2);

        _kinematics.Rotations[plateA] = rotationA.Orientation;
        _kinematics.Rotations[plateB] = rotationB.Orientation;

        var tick = CanonicalTick.Genesis;
        var modelId = new ModelId(Guid.NewGuid());

        // ACT
        // 1. Get Transform of Plate A observed field from Mantle Frame
        // T_A_M = R_a
        var resultInMantle = _sut.GetFrameTransform(
            new PlateAnchor { PlateId = plateA }, // From A
            MantleFrame.Instance,                 // To Mantle
            tick, modelId, _kinematics, _topology);

        // 2. Get Transform of Plate A observed from Plate B Frame (Anchor B)
        // T_A_B = T_M_B * T_A_M (where T_M_B is Mantle->B, i.e., inv(R_b))
        // So T_A_B = inv(R_b) * R_a
        var resultInFrameB = _sut.GetFrameTransform(
            new PlateAnchor { PlateId = plateA }, // From A
            new PlateAnchor { PlateId = plateB }, // To B
            tick, modelId, _kinematics, _topology);

        // ASSERT
        // Verify Principle 1 & 2: Frames are consistent lenses

        // Expected T_A_B = inv(R_b) * R_a
        var expectedTransform = rotationA.Compose(rotationB.Inverted());

        // Compare orientations
        // Note: Floating point comparison requires tolerance
        var actualQ = resultInFrameB.Transform.Orientation;
        var expectedQ = expectedTransform.Orientation;

        // Simple dot product check for quaternion equality (should be ~1 or ~-1)
        var dot = actualQ.X * expectedQ.X + actualQ.Y * expectedQ.Y + actualQ.Z * expectedQ.Z + actualQ.W * expectedQ.W;
        Math.Abs(dot).Should().BeGreaterThan(0.999999, "Frames must compose consistently: Relative motion A->B should match (A->M)->(M->B)");

        // Verify Principle 3: No side effects (mock checking)
        // (Implicit as we used a stub and nothing else was called)
    }

    // Stubs
    private class StubKinematicsView : IPlateKinematicsStateView
    {
        public Dictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Numerics.Quaterniond> Rotations { get; } = new();

        public TruthStreamIdentity Identity { get; } = new(
            VariantId: "test",
            BranchId: "test",
            LLevel: 0,
            Domain: Domain.GeoPlatesKinematics,
            Model: "M0");

        public long LastEventSequence => 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out FantaSim.Geosphere.Plate.Topology.Contracts.Numerics.Quaterniond rotation)
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

        public IReadOnlyDictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate> Plates { get; }
            = new Dictionary<PlateId, FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate>();

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }
            = new Dictionary<BoundaryId, Boundary>();

        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; }
            = new Dictionary<JunctionId, Junction>();

        public long LastEventSequence => 0;
    }
}
