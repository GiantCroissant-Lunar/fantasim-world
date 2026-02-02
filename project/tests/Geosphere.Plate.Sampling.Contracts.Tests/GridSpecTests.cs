using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts.Tests;

public class GridSpecTests
{
    [Fact]
    public void GlobalOneDegree_PixelRegistered_HasCorrectNodeCount()
    {
        // 1.0 degree pixel registered: 180 lat zones, 360 lon zones
        // Lat: -89.5 ... 89.5 (180 nodes)
        // Lon: -179.5 ... 179.5 (360 nodes)
        var grid = GridSpec.Global(1.0, GridRegistration.Pixel);

        grid.NLat.Should().Be(180);
        grid.NLon.Should().Be(360);
        grid.NodeCount.Should().Be(64800);
        grid.CellCentered.Should().BeTrue();
    }

    [Fact]
    public void GlobalOneDegree_GridlineRegistered_HasCorrectNodeCount()
    {
        // 1.0 degree gridline registered:
        // Lat: -90 ... 90 (181 nodes)
        // Lon: -180 ... 180 (361 nodes) - Usually wraps, but gridline definition implies endpoints
        // RFC Rule: NLat = 181, NLon = 361
        var grid = GridSpec.Global(1.0, GridRegistration.Gridline);

        grid.NLat.Should().Be(181);
        grid.NLon.Should().Be(361);
        grid.NodeCount.Should().Be(65341); // 181 * 361
        grid.CellCentered.Should().BeFalse();
    }

    [Fact]
    public void GlobalHalfDegree_PixelRegistered_HasCorrectNodeCount()
    {
        // 0.5 degree pixel
        // 180/0.5 = 360 lat
        // 360/0.5 = 720 lon
        var grid = GridSpec.Global(0.5, GridRegistration.Pixel);

        grid.NLat.Should().Be(360);
        grid.NLon.Should().Be(720);
        grid.NodeCount.Should().Be(259200);
    }
}
