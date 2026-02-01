using FluentAssertions;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public sealed class FeaturePlateAssignerTests
{
    [Fact]
    public void AssignPlateProvenance_Point2_AssignsPlateId()
    {
        var assigner = new FeaturePlateAssigner();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var regionA = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(10, 0),
                new Point2(10, 10),
                new Point2(0, 10)
            }),
            Array.Empty<Polygon2>());

        var features = new[]
        {
            new ReconstructableFeature(new FeatureId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), new Point2(5, 5), null)
        };

        var partition = new[]
        {
            new PlatePartitionRegion(plateA, regionA)
        };

        var assigned = assigner.AssignPlateProvenance(features, partition);

        assigned.Should().HaveCount(1);
        assigned[0].PlateIdProvenance.Should().Be(plateA);
    }

    [Fact]
    public void AssignPlateProvenance_Polyline2_UsesFirstVertex()
    {
        var assigner = new FeaturePlateAssigner();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var regionA = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(10, 0),
                new Point2(10, 10),
                new Point2(0, 10)
            }),
            Array.Empty<Polygon2>());

        var polyline = new Polyline2(new[] { new Point2(5, 5), new Point2(50, 50) });

        var features = new[]
        {
            new ReconstructableFeature(new FeatureId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")), polyline, null)
        };

        var partition = new[]
        {
            new PlatePartitionRegion(plateA, regionA)
        };

        var assigned = assigner.AssignPlateProvenance(features, partition);

        assigned.Should().HaveCount(1);
        assigned[0].PlateIdProvenance.Should().Be(plateA);
    }

    [Fact]
    public void AssignPlateProvenance_OverlappingRegions_PicksLowestPlateIdDeterministically()
    {
        var assigner = new FeaturePlateAssigner();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var plateB = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var region = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(10, 0),
                new Point2(10, 10),
                new Point2(0, 10)
            }),
            Array.Empty<Polygon2>());

        var features = new[]
        {
            new ReconstructableFeature(new FeatureId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")), new Point2(5, 5), null)
        };

        var partition = new[]
        {
            new PlatePartitionRegion(plateB, region),
            new PlatePartitionRegion(plateA, region)
        };

        var assigned = assigner.AssignPlateProvenance(features, partition);

        assigned.Should().HaveCount(1);
        assigned[0].PlateIdProvenance.Should().Be(plateA);
    }

    [Fact]
    public void AssignPlateProvenance_DoesNotOverrideExistingProvenance()
    {
        var assigner = new FeaturePlateAssigner();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var plateB = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var regionA = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(10, 0),
                new Point2(10, 10),
                new Point2(0, 10)
            }),
            Array.Empty<Polygon2>());

        var features = new[]
        {
            new ReconstructableFeature(new FeatureId(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")), new Point2(5, 5), plateB)
        };

        var partition = new[]
        {
            new PlatePartitionRegion(plateA, regionA)
        };

        var assigned = assigner.AssignPlateProvenance(features, partition);

        assigned.Should().HaveCount(1);
        assigned[0].PlateIdProvenance.Should().Be(plateB);
    }

    [Fact]
    public void AssignPlateProvenance_UnsupportedGeometry_LeavesUnassigned()
    {
        var assigner = new FeaturePlateAssigner();

        var plateA = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var regionA = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(10, 0),
                new Point2(10, 10),
                new Point2(0, 10)
            }),
            Array.Empty<Polygon2>());

        var features = new[]
        {
            new ReconstructableFeature(
                new FeatureId(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
                new Segment2(1, 1, 2, 2),
                null)
        };

        var partition = new[]
        {
            new PlatePartitionRegion(plateA, regionA)
        };

        var assigned = assigner.AssignPlateProvenance(features, partition);

        assigned.Should().HaveCount(1);
        assigned[0].PlateIdProvenance.Should().BeNull();
    }
}
