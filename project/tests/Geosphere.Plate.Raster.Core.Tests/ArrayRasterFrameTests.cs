using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Core;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Raster.Core.Tests;

/// <summary>
/// Tests for ArrayRasterFrame - the core raster frame implementation.
/// </summary>
public class ArrayRasterFrameTests
{
    [Fact]
    public void Constructor_ValidData_CreatesFrame()
    {
        // Arrange
        var tick = CanonicalTick.Genesis;
        // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var data = new double[] { 1.0, 2.0, 3.0, 4.0 };

        // Act
        var frame = new ArrayRasterFrame(tick, 2, 2, bounds, RasterDataType.Float64, data, null);

        // Assert
        frame.Width.Should().Be(2);
        frame.Height.Should().Be(2);
        frame.Tick.Should().Be(tick);
        frame.Bounds.Should().Be(bounds);
    }

    [Fact]
    public void GetValue_ValidIndex_ReturnsValue()
    {
        // Arrange
        var data = new double[] { 1.0, 2.0, 3.0, 4.0 };
        var frame = CreateFrame(2, 2, data);

        // Act & Assert
        frame.GetValue(0, 0).Should().Be(1.0);
        frame.GetValue(0, 1).Should().Be(2.0);
        frame.GetValue(1, 0).Should().Be(3.0);
        frame.GetValue(1, 1).Should().Be(4.0);
    }

    [Fact]
    public void GetValue_NoDataValue_ReturnsNull()
    {
        // Arrange
        var noData = -9999.0;
        var data = new double[] { 1.0, noData, 3.0, 4.0 };
        var frame = new ArrayRasterFrame(
            CanonicalTick.Genesis, 2, 2,
            // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
            new RasterBounds(-180, 180, -90, 90),
            RasterDataType.Float64, data, noData);

        // Act & Assert
        frame.GetValue(0, 0).Should().Be(1.0);
        frame.GetValue(0, 1).Should().BeNull();
    }

    [Fact]
    public void GetValueAt_CenterOfCell_ReturnsValue()
    {
        // Arrange: 2x2 grid covering full world
        var data = new double[] { 1.0, 2.0, 3.0, 4.0 };
        var frame = CreateFrame(2, 2, data);

        // Act: Query center of top-left cell
        // Top-left cell covers lon [-180, 0), lat [0, 90]
        var value = frame.GetValueAt(-90, 45);

        // Assert
        value.Should().Be(1.0);
    }

    [Fact]
    public void GetValueAt_OutOfBounds_ReturnsNull()
    {
        // Arrange
        var data = new double[] { 1.0, 2.0, 3.0, 4.0 };
        var frame = CreateFrame(2, 2, data);

        // Act
        var value = frame.GetValueAt(200, 0); // Out of bounds

        // Assert
        value.Should().BeNull();
    }

    private static ArrayRasterFrame CreateFrame(int width, int height, double[] data)
    {
        return new ArrayRasterFrame(
            CanonicalTick.Genesis,
            width, height,
            // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
            new RasterBounds(-180, 180, -90, 90),
            RasterDataType.Float64,
            data,
            null);
    }
}
