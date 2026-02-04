using FantaSim.Spatial.Region.Contracts;

namespace FantaSim.Spatial.Region.Contracts.Tests.Factories;

/// <summary>
/// Tests for factory methods.
/// </summary>
public class RegionFactoryTests
{
    [Fact]
    public void RegionSpec_Surface_CreatesCorrectShape()
    {
        var region = RegionSpec.Surface(thicknessM: 50.0);

        Assert.Equal(1, region.Version);
        Assert.Equal("canonical_sphere", region.Space);
        Assert.Equal("surface_shell", region.Shape.Kind);
        Assert.Equal(50.0, region.Shape.SurfaceShell!.ThicknessM);
        Assert.Equal("planet_center", region.Frame.Anchor.Type);
    }

    [Fact]
    public void RegionSpec_SphericalShell_CreatesCorrectShape()
    {
        var region = RegionSpec.SphericalShell(
            rMinM: 6371000.0,
            rMaxM: 6451000.0);

        Assert.Equal("spherical_shell", region.Shape.Kind);
        Assert.Equal(6371000.0, region.Shape.SphericalShell!.RMinM);
        Assert.Equal(6451000.0, region.Shape.SphericalShell!.RMaxM);
        Assert.Null(region.Shape.SphericalShell!.AngularClip);
    }

    [Fact]
    public void RegionFrame_PlanetCenter_CreatesCorrectFrame()
    {
        var frame = RegionFrame.PlanetCenter();

        Assert.Equal("planet_center", frame.Anchor.Type);
        Assert.Null(frame.Anchor.PlateId);
        Assert.Null(frame.Anchor.Position);
        Assert.Equal("planet_fixed", frame.Basis.Type);
    }

    [Fact]
    public void RegionFrame_ForPlate_CreatesCorrectFrame()
    {
        var frame = RegionFrame.ForPlate("plate_701");

        Assert.Equal("plate", frame.Anchor.Type);
        Assert.Equal("plate_701", frame.Anchor.PlateId);
    }

    [Fact]
    public void RegionFrame_AtPoint_CreatesCorrectFrame()
    {
        var point = new Point3(1.0, 2.0, 3.0);
        var frame = RegionFrame.AtPoint(point);

        Assert.Equal("point", frame.Anchor.Type);
        Assert.Equal(point, frame.Anchor.Position);
        Assert.Equal("tangent", frame.Basis.Type);
        Assert.Equal(point, frame.Basis.At);
    }

    [Fact]
    public void RegionSampling_S2_CreatesCorrectPolicy()
    {
        var sampling = RegionSampling.S2(level: 12, zLayers: 5, toleranceM: 100.0);

        Assert.Equal("s2", sampling.IndexKind);
        Assert.Equal(12, sampling.Level);
        Assert.Equal(5, sampling.ZLayers);
        Assert.Equal(100.0, sampling.ToleranceM);
    }

    [Fact]
    public void RegionSampling_Octree_CreatesCorrectPolicy()
    {
        var sampling = RegionSampling.Octree(depth: 8, toleranceM: 500.0);

        Assert.Equal("octree", sampling.IndexKind);
        Assert.Equal(8, sampling.Level);
        Assert.Null(sampling.ZLayers);
        Assert.Equal(500.0, sampling.ToleranceM);
    }

    [Fact]
    public void AngularClip_CapClip_CreatesCorrectClip()
    {
        var center = new Point3(0, 0, 1);
        var clip = AngularClip.CapClip(center, Math.PI / 4);

        Assert.Equal("cap", clip.Kind);
        Assert.NotNull(clip.Cap);
        Assert.Equal(center, clip.Cap!.Center);
        Assert.Equal(Math.PI / 4, clip.Cap!.AngularRadiusRad);
    }

    [Fact]
    public void SurfaceFootprint_Plate_CreatesCorrectFootprint()
    {
        var footprint = SurfaceFootprint.Plate("plate_101");

        Assert.Equal("plate", footprint.Kind);
        Assert.Equal("plate_101", footprint.PlateId);
        Assert.Null(footprint.PolygonVertices);
    }
}
