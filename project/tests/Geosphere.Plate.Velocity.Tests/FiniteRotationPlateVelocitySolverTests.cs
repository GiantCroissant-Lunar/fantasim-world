using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;

namespace FantaSim.Geosphere.Plate.Velocity.Tests;

public sealed class FiniteRotationPlateVelocitySolverTests
{
    private const double Epsilon = 1e-9;

    [Fact]
    public void GetAngularVelocity_ReturnsZero_WhenKinematicsReturnsFalse()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new FakeKinematicsState(returnsRotation: false);

        var omega = solver.GetAngularVelocity(kinematics, plateId, new CanonicalTick(10));

        omega.Should().Be(AngularVelocity3d.Zero);
    }

    [Fact]
    public void GetAbsoluteVelocity_ReturnsZero_WhenKinematicsReturnsFalse()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new FakeKinematicsState(returnsRotation: false);
        var point = new Vector3d(1, 0, 0);

        var velocity = solver.GetAbsoluteVelocity(kinematics, plateId, point, new CanonicalTick(10));

        velocity.Should().Be(Velocity3d.Zero);
    }

    [Fact]
    public void GetAngularVelocity_ReturnsZero_WhenRotationIsIdentity()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new FakeKinematicsState(Quaterniond.Identity);

        var omega = solver.GetAngularVelocity(kinematics, plateId, new CanonicalTick(10));

        omega.Rate().Should().BeLessThan(Epsilon);
    }

    [Fact]
    public void GetAbsoluteVelocity_ReturnsZero_WhenRotationIsIdentity()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new FakeKinematicsState(Quaterniond.Identity);
        var point = new Vector3d(1, 0, 0);

        var velocity = solver.GetAbsoluteVelocity(kinematics, plateId, point, new CanonicalTick(10));

        velocity.Magnitude().Should().BeLessThan(Epsilon);
    }

    [Fact]
    public void GetAngularVelocity_ExtractsCorrectAxisAndRate_ForKnownRotation()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Rotation about Z axis: 0.1 radians per tick
        var angularRate = 0.1;
        var kinematics = new RotatingKinematicsState(Vector3d.UnitZ, angularRate);

        var omega = solver.GetAngularVelocity(kinematics, plateId, new CanonicalTick(10));

        omega.Rate().Should().BeApproximately(angularRate, Epsilon);
        var (axisX, axisY, axisZ) = omega.GetAxis();
        axisX.Should().BeApproximately(0, Epsilon);
        axisY.Should().BeApproximately(0, Epsilon);
        Math.Abs(axisZ).Should().BeApproximately(1, Epsilon);
    }

    [Fact]
    public void GetAbsoluteVelocity_ComputesCorrectCrossProduct()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Rotation about Z axis: 0.1 radians per tick
        var angularRate = 0.1;
        var kinematics = new RotatingKinematicsState(Vector3d.UnitZ, angularRate);

        // Point on X axis at distance 1
        var point = new Vector3d(1, 0, 0);
        var tick = new CanonicalTick(10);

        var velocity = solver.GetAbsoluteVelocity(kinematics, plateId, point, tick);

        // v = ω × p = (0, 0, angularRate) × (1, 0, 0) = (0, angularRate, 0)
        velocity.X.Should().BeApproximately(0, Epsilon);
        velocity.Y.Should().BeApproximately(angularRate, Epsilon);
        velocity.Z.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void GetRelativeVelocity_ReturnsVelocityDifference()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateA = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateB = new PlateId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var kinematics = new TwoPlateKinematicsState(
            plateA, Vector3d.UnitZ, 0.1,
            plateB, Vector3d.UnitZ, 0.05);

        var point = new Vector3d(1, 0, 0);
        var tick = new CanonicalTick(10);

        var vA = solver.GetAbsoluteVelocity(kinematics, plateA, point, tick);
        var vB = solver.GetAbsoluteVelocity(kinematics, plateB, point, tick);
        var relative = solver.GetRelativeVelocity(kinematics, plateA, plateB, point, tick);

        relative.X.Should().BeApproximately(vA.X - vB.X, Epsilon);
        relative.Y.Should().BeApproximately(vA.Y - vB.Y, Epsilon);
        relative.Z.Should().BeApproximately(vA.Z - vB.Z, Epsilon);
    }

    [Fact]
    public void GetRelativeVelocity_ReturnsZero_WhenBothPlatesMissingKinematics()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateA = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateB = new PlateId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var kinematics = new FakeKinematicsState(returnsRotation: false);
        var point = new Vector3d(1, 0, 0);

        var relative = solver.GetRelativeVelocity(kinematics, plateA, plateB, point, new CanonicalTick(10));

        relative.Should().Be(Velocity3d.Zero);
    }

    [Fact]
    public void GetAngularVelocity_IsDeterministic()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new RotatingKinematicsState(Vector3d.UnitZ, 0.1);
        var tick = new CanonicalTick(10);

        var omega1 = solver.GetAngularVelocity(kinematics, plateId, tick);
        var omega2 = solver.GetAngularVelocity(kinematics, plateId, tick);

        omega1.Should().Be(omega2);
    }

    [Fact]
    public void GetAbsoluteVelocity_IsDeterministic()
    {
        var solver = new FiniteRotationPlateVelocitySolver();
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var kinematics = new RotatingKinematicsState(Vector3d.UnitZ, 0.1);
        var point = new Vector3d(1, 0, 0);
        var tick = new CanonicalTick(10);

        var v1 = solver.GetAbsoluteVelocity(kinematics, plateId, point, tick);
        var v2 = solver.GetAbsoluteVelocity(kinematics, plateId, point, tick);

        v1.Should().Be(v2);
    }

    [Fact]
    public void AngularVelocity3d_GetLinearVelocityAt_ComputesCrossProduct()
    {
        var omega = new AngularVelocity3d(0, 0, 1);

        var v = omega.GetLinearVelocityAt(1, 0, 0);

        v.X.Should().BeApproximately(0, Epsilon);
        v.Y.Should().BeApproximately(1, Epsilon);
        v.Z.Should().BeApproximately(0, Epsilon);
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

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = _rotation;
            return _returnsRotation;
        }
    }

    private sealed class RotatingKinematicsState : IPlateKinematicsStateView
    {
        private readonly Vector3d _axis;
        private readonly double _ratePerTick;

        public RotatingKinematicsState(Vector3d axis, double ratePerTick)
        {
            _axis = axis;
            _ratePerTick = ratePerTick;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            var angle = tick.Value * _ratePerTick;
            rotation = Quaterniond.FromAxisAngle(_axis, angle);
            return true;
        }
    }

    private sealed class TwoPlateKinematicsState : IPlateKinematicsStateView
    {
        private readonly PlateId _plateA;
        private readonly Vector3d _axisA;
        private readonly double _rateA;
        private readonly PlateId _plateB;
        private readonly Vector3d _axisB;
        private readonly double _rateB;

        public TwoPlateKinematicsState(
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

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
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
}
