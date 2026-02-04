using FantaSim.Spatial.Region.Contracts;

namespace FantaSim.Spatial.Region.Contracts.Tests.Serialization;

/// <summary>
/// Tests for MessagePack serialization round-trips.
/// </summary>
public class RegionSpecSerializationTests
{
    [Fact]
    public void RegionSpec_RoundTrip_SurfaceShell()
    {
        var original = RegionSpec.Surface(thicknessM: 100.0);

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<RegionSpec>(bytes);

        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.Space, restored.Space);
        Assert.Equal(original.Shape.Kind, restored.Shape.Kind);
        Assert.Equal(original.Shape.SurfaceShell!.ThicknessM, restored.Shape.SurfaceShell!.ThicknessM);
    }

    [Fact]
    public void RegionSpec_RoundTrip_SphericalShell()
    {
        var original = RegionSpec.SphericalShell(
            rMinM: 6371000.0,
            rMaxM: 6451000.0,
            angularClip: AngularClip.CapClip(new Point3(0, 0, 1), Math.PI / 4));

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<RegionSpec>(bytes);

        Assert.Equal(original.Shape.SphericalShell!.RMinM, restored.Shape.SphericalShell!.RMinM);
        Assert.Equal(original.Shape.SphericalShell!.RMaxM, restored.Shape.SphericalShell!.RMaxM);
        Assert.NotNull(restored.Shape.SphericalShell!.AngularClip);
        Assert.Equal("cap", restored.Shape.SphericalShell!.AngularClip!.Kind);
    }

    [Fact]
    public void RegionFrame_RoundTrip_PlanetCenter()
    {
        var original = RegionFrame.PlanetCenter();

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<RegionFrame>(bytes);

        Assert.Equal("planet_center", restored.Anchor.Type);
        Assert.Equal("planet_fixed", restored.Basis.Type);
    }

    [Fact]
    public void RegionSpec_RoundTrip_WithSampling()
    {
        var original = new RegionSpec
        {
            Version = 1,
            Space = "canonical_sphere",
            Shape = RegionShape.SurfaceShellShape(0.0),
            Frame = RegionFrame.PlanetCenter(),
            Sampling = RegionSampling.S2(level: 12, zLayers: 5, toleranceM: 100.0)
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<RegionSpec>(bytes);

        Assert.NotNull(restored.Sampling);
        Assert.Equal("s2", restored.Sampling!.IndexKind);
        Assert.Equal(12, restored.Sampling!.Level);
        Assert.Equal(5, restored.Sampling!.ZLayers);
        Assert.Equal(100.0, restored.Sampling!.ToleranceM);
    }

    [Fact]
    public void SliceSpec_RoundTrip()
    {
        var original = new SliceSpec
        {
            Version = 1,
            RegionSpecHash = "abc123",
            Mode = "plane_section",
            Frame = SliceFrame.HorizontalAt(10000.0),
            Mapping = ChartMapping.Orthographic(scale: 0.001),
            Clip2D = null
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<SliceSpec>(bytes);

        Assert.Equal(original.Mode, restored.Mode);
        Assert.Equal(original.RegionSpecHash, restored.RegionSpecHash);
        Assert.Equal(original.Mapping.Type, restored.Mapping.Type);
    }
}
