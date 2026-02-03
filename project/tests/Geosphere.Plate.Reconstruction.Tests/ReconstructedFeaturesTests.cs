using Plate.TimeDete.Time.Primitives;
using FluentAssertions;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifyGeometry;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public sealed class ReconstructedFeaturesTests
{
    [Fact]
    public void ReconstructFeatures_SkipsFeaturesWithoutProvenance()
    {
        var solver = new NaivePlateReconstructionSolver();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var plateB = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var features = new[]
        {
            new ReconstructableFeature(new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f2")), new Point2(1, 2), plateA),
            new ReconstructableFeature(new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f1")), new Point2(3, 4), null),
            new ReconstructableFeature(new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f3")), new Point2(5, 6), plateB)
        };

        var kin = new FakeKinematicsState();

        var reconstructed = solver.ReconstructFeatures(features, kin, CreatePolicy(), new CanonicalTick(10));

        reconstructed.Should().HaveCount(2);
        reconstructed.Select(x => x.FeatureId).Should().BeEquivalentTo(new[] { features[0].FeatureId, features[2].FeatureId });
        reconstructed.Select(x => x.PlateIdProvenance).Should().BeEquivalentTo(new[] { plateA, plateB });
    }

    [Fact]
    public void ReconstructFeatures_IsDeterministic_AndSortedByFeatureId()
    {
        var solver = new NaivePlateReconstructionSolver();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var f1 = new ReconstructableFeature(new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f1")), new Point2(1, 2), plateA);
        var f2 = new ReconstructableFeature(new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f2")), new Point2(3, 4), plateA);

        var kin = new FakeKinematicsState();

        var r1 = solver.ReconstructFeatures(new[] { f2, f1 }, kin, CreatePolicy(), new CanonicalTick(10));
        var r2 = solver.ReconstructFeatures(new[] { f2, f1 }, kin, CreatePolicy(), new CanonicalTick(10));

        r1.Should().Equal(r2);
        r1.Should().HaveCount(2);
        r1[0].FeatureId.Should().Be(f1.FeatureId);
        r1[1].FeatureId.Should().Be(f2.FeatureId);
    }

    [Fact]
    public void ReconstructFeatures_RotatesPoint3Geometry_WhenKinematicsProvidesRotation()
    {
        var solver = new NaivePlateReconstructionSolver();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var feature = new ReconstructableFeature(
            new FeatureId(Guid.Parse("00000000-0000-0000-0000-0000000000f1")),
            new Point3(1d, 0d, 0d),
            plateA);

        var rotation = Quaterniond.FromAxisAngle(Vector3d.UnitZ, Math.PI / 2d);
        var kin = new FakeKinematicsState(rotation);

        var r = solver.ReconstructFeatures(new[] { feature }, kin, CreatePolicy(), new CanonicalTick(10));
        r.Should().HaveCount(1);

        var rotated = r[0].Geometry.Should().BeOfType<Point3>().Subject;
        Math.Abs(rotated.X).Should().BeLessThan(1e-9);
        Math.Abs(rotated.Y - 1d).Should().BeLessThan(1e-9);
        Math.Abs(rotated.Z).Should().BeLessThan(1e-9);
    }

    private sealed class FakeKinematicsState : IPlateKinematicsStateView
    {
        private readonly Quaterniond _rotation;

        public FakeKinematicsState()
            : this(Quaterniond.Identity)
        {
        }

        public FakeKinematicsState(Quaterniond rotation)
        {
            _rotation = rotation;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");

        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = _rotation;
            return true;
        }
    }

    private static ReconstructionPolicy CreatePolicy()
    {
        return new ReconstructionPolicy
        {
            Frame = FantaSim.Geosphere.Plate.Kinematics.Contracts.MantleFrame.Instance,
            KinematicsModel = new ModelId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            PartitionTolerance = new FantaSim.Geosphere.Plate.Partition.Contracts.TolerancePolicy.StrictPolicy()
        };
    }
}
