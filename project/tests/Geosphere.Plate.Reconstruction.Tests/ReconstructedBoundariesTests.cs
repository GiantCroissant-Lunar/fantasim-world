using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifyGeometry;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public sealed class ReconstructedBoundariesTests
{
    [Fact]
    public void ReconstructBoundaries_IsDeterministic_AndCarriesProvenance()
    {
        var solver = new NaivePlateReconstructionSolver();

        var plateA = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateB = new PlateId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var boundary1 = new Boundary(
            new BoundaryId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            plateA,
            plateB,
            BoundaryType.Divergent,
            new Point2(1, 2),
            false,
            null);

        var boundary2 = new Boundary(
            new BoundaryId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            plateB,
            plateA,
            BoundaryType.Convergent,
            new Point2(3, 4),
            false,
            null);

        var topo = new FakeTopologyState(
            Domain.Parse("geo.plates"),
            new[] { boundary1, boundary2 });

        var kin = new FakeKinematicsState();

        var r1 = solver.ReconstructBoundaries(topo, kin, new CanonicalTick(10));
        var r2 = solver.ReconstructBoundaries(topo, kin, new CanonicalTick(10));

        Assert.Equal(r1, r2);
        Assert.Equal(2, r1.Count);

        // Sorted deterministically by BoundaryId.Value
        Assert.Equal(boundary2.BoundaryId, r1[0].BoundaryId);
        Assert.Equal(boundary1.BoundaryId, r1[1].BoundaryId);

        // Provenance policy: left plate
        Assert.Equal(boundary2.PlateIdLeft, r1[0].PlateIdProvenance);
        Assert.Equal(boundary1.PlateIdLeft, r1[1].PlateIdProvenance);
    }

    [Fact]
    public void ReconstructBoundaries_RotatesPoint3Geometry_WhenKinematicsProvidesRotation()
    {
        var solver = new NaivePlateReconstructionSolver();

        var plateA = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateB = new PlateId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var boundary = new Boundary(
            new BoundaryId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            plateA,
            plateB,
            BoundaryType.Divergent,
            new Point3(1d, 0d, 0d),
            false,
            null);

        var topo = new FakeTopologyState(Domain.Parse("geo.plates"), new[] { boundary });

        var rotation = Quaterniond.FromAxisAngle(Vector3d.UnitZ, Math.PI / 2d);
        var kin = new FakeKinematicsState(rotation);

        var r = solver.ReconstructBoundaries(topo, kin, new CanonicalTick(10));
        Assert.Single(r);

        var rotated = Assert.IsType<Point3>(r[0].Geometry);
        Assert.True(Math.Abs(rotated.X) < 1e-9);
        Assert.True(Math.Abs(rotated.Y - 1d) < 1e-9);
        Assert.True(Math.Abs(rotated.Z) < 1e-9);
    }

    private sealed class FakeTopologyState : IPlateTopologyStateView
    {
        private static readonly IReadOnlyDictionary<PlateId, PlateEntity> EmptyPlates =
            new Dictionary<PlateId, PlateEntity>();
        private static readonly IReadOnlyDictionary<JunctionId, JunctionEntity> EmptyJunctions =
            new Dictionary<JunctionId, JunctionEntity>();

        public FakeTopologyState(Domain domain, IEnumerable<Boundary> boundaries)
        {
            Identity = new TruthStreamIdentity("science", "trunk", 2, domain, "0");
            Boundaries = boundaries.ToDictionary(b => b.BoundaryId, b => b);
            Plates = EmptyPlates;
            Junctions = EmptyJunctions;
            LastEventSequence = 0;
        }

        public TruthStreamIdentity Identity { get; }

        public IReadOnlyDictionary<PlateId, PlateEntity> Plates { get; }

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; }

        public IReadOnlyDictionary<JunctionId, JunctionEntity> Junctions { get; }

        public long LastEventSequence { get; }
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

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out FantaSim.Geosphere.Plate.Topology.Contracts.Numerics.Quaterniond rotation)
        {
            rotation = _rotation;
            return true;
        }
    }
}
