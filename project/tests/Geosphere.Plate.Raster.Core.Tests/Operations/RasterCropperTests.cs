using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Core;
using FantaSim.Geosphere.Plate.Raster.Core.Operations;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Raster.Core.Tests.Operations;

/// <summary>
/// Tests for RasterCropper operations.
/// </summary>
public class RasterCropperTests
{
    [Fact]
    public void Crop_ValidBounds_ReturnsSubset()
    {
        // Arrange: 4x4 grid covering lon [0,4], lat [0,4]
        // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
        var data = new double[]
        {
            1, 2, 3, 4,        // row 0 (lat 3-4, top row)
            5, 6, 7, 8,        // row 1 (lat 2-3)
            9, 10, 11, 12,     // row 2 (lat 1-2)
            13, 14, 15, 16     // row 3 (lat 0-1, bottom row)
        };
        var frame = CreateFrame(4, 4, new RasterBounds(0, 4, 0, 4), data);

        // Act: Crop to center (lon [1,3], lat [1,3])
        var cropped = RasterCropper.Crop(frame, new RasterBounds(1, 3, 1, 3));

        // Assert
        cropped.Width.Should().Be(2);
        cropped.Height.Should().Be(2);
        // Center 2x2 region at rows 1-2, cols 1-2
        cropped.GetValue(0, 0).Should().Be(6.0);  // row 1, col 1
        cropped.GetValue(0, 1).Should().Be(7.0);  // row 1, col 2
        cropped.GetValue(1, 0).Should().Be(10.0); // row 2, col 1
        cropped.GetValue(1, 1).Should().Be(11.0); // row 2, col 2
    }

    [Fact]
    public void Crop_NonOverlappingBounds_ThrowsArgumentException()
    {
        // Arrange
        var data = new double[] { 1, 2, 3, 4 };
        // RasterBounds(MinLon, MaxLon, MinLat, MaxLat)
        var frame = CreateFrame(2, 2, new RasterBounds(0, 2, 0, 2), data);

        // Act & Assert
        var act = () => RasterCropper.Crop(frame, new RasterBounds(10, 20, 10, 20));
        act.Should().Throw<ArgumentException>();
    }

    private static ArrayRasterFrame CreateFrame(int width, int height, RasterBounds bounds, double[] data)
    {
        return new ArrayRasterFrame(
            CanonicalTick.Genesis,
            width, height,
            bounds,
            RasterDataType.Float64,
            data,
            null);
    }
}
