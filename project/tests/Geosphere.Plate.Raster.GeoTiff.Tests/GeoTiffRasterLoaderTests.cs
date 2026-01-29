using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using NSubstitute;

namespace FantaSim.Geosphere.Plate.Raster.GeoTiff.Tests;

public class GeoTiffRasterLoaderTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly IPlatesDataset _mockDataset;

    public GeoTiffRasterLoaderTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), $"geotiff_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDir);

        // Create mock dataset pointing to our test directory
        _mockDataset = Substitute.For<IPlatesDataset>();
        _mockDataset.DatasetRootPath.Returns(_testDataDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
        {
            try
            {
                Directory.Delete(_testDataDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public void CanLoad_WithGeoTiffFormat_ReturnsTrue()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();

        // Act & Assert
        loader.CanLoad("geotiff").Should().BeTrue();
        loader.CanLoad("tiff").Should().BeTrue();
        loader.CanLoad("tif").Should().BeTrue();
        loader.CanLoad("GEOTIFF").Should().BeTrue();
    }

    [Fact]
    public void CanLoad_WithUnsupportedFormat_ReturnsFalse()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();

        // Act & Assert
        loader.CanLoad("netcdf").Should().BeFalse();
        loader.CanLoad("asc").Should().BeFalse();
        loader.CanLoad("jpg").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_SingleFile_ReturnsSequence()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var testFile = "test.tif";
        CreateSimpleTiff(Path.Combine(_testDataDir, testFile), 10, 10);
        var asset = new RasterSequenceAsset("test-sequence", testFile, "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);

        // Assert
        sequence.Should().NotBeNull();
        sequence.AvailableTicks.Should().HaveCount(1);
        sequence.SequenceId.Should().Be("test-sequence");
    }

    [Fact]
    public async Task LoadAsync_SingleFile_HasCorrectDimensions()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var testFile = "dimensions_test.tif";
        CreateSimpleTiff(Path.Combine(_testDataDir, testFile), 20, 30);
        var asset = new RasterSequenceAsset("dim-sequence", testFile, "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);
        var frame = sequence.GetFrameAt(sequence.AvailableTicks[0]);

        // Assert
        frame.Should().NotBeNull();
        frame!.Width.Should().Be(20);
        frame.Height.Should().Be(30);
        frame.DataType.Should().Be(RasterDataType.Float32);
    }

    [Fact]
    public async Task LoadAsync_MultiBandFile_ReturnsMultipleFrames()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var testFile = "multiband.tif";
        CreateMultiBandTiff(Path.Combine(_testDataDir, testFile), 10, 10, bands: 3);
        var asset = new RasterSequenceAsset("multiband-sequence", testFile, "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);

        // Assert
        sequence.Should().NotBeNull();
        sequence.AvailableTicks.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadAsync_PatternMatching_ReturnsSequence()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_001.tif"), 10, 10);
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_002.tif"), 10, 10);
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_003.tif"), 10, 10);
        var asset = new RasterSequenceAsset("pattern-sequence", "elevation_*.tif", "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);

        // Assert
        sequence.Should().NotBeNull();
        sequence.AvailableTicks.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadAsync_PatternMatching_ExtractsTicksFromFilenames()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_100.tif"), 10, 10);
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_200.tif"), 10, 10);
        CreateSimpleTiff(Path.Combine(_testDataDir, "elevation_300.tif"), 10, 10);
        var asset = new RasterSequenceAsset("tick-sequence", "elevation_*.tif", "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);

        // Assert
        sequence.Should().NotBeNull();
        sequence.AvailableTicks.Should().Contain(new CanonicalTick(100));
        sequence.AvailableTicks.Should().Contain(new CanonicalTick(200));
        sequence.AvailableTicks.Should().Contain(new CanonicalTick(300));
    }

    [Fact]
    public async Task LoadAsync_Directory_ReturnsSequence()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var subDir = Path.Combine(_testDataDir, "subdir");
        Directory.CreateDirectory(subDir);
        CreateSimpleTiff(Path.Combine(subDir, "file1.tif"), 10, 10);
        CreateSimpleTiff(Path.Combine(subDir, "file2.tif"), 10, 10);
        var asset = new RasterSequenceAsset("dir-sequence", "subdir", "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);

        // Assert
        sequence.Should().NotBeNull();
        sequence.AvailableTicks.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_FrameData_IsAccessible()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var testFile = "data_test.tif";
        var expectedValue = 42.0f;
        CreateSimpleTiff(Path.Combine(_testDataDir, testFile), 2, 2,
            values: new[] { expectedValue, 1.0f, 2.0f, 3.0f });
        var asset = new RasterSequenceAsset("data-sequence", testFile, "geotiff");

        // Act
        var sequence = await loader.LoadAsync(asset, _mockDataset);
        var frame = sequence.GetFrameAt(sequence.AvailableTicks[0]);

        // Assert
        frame.Should().NotBeNull();
        frame!.GetValue(0, 0).Should().BeApproximately(expectedValue, 0.001);
    }

    [Fact]
    public async Task LoadAsync_NonExistentFile_Throws()
    {
        // Arrange
        var loader = new GeoTiffRasterLoader();
        var asset = new RasterSequenceAsset("nonexistent", "nonexistent.tif", "geotiff");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadAsync(asset, _mockDataset));
    }

    [Fact]
    public void GeoTiffRasterFrame_GetValue_OutOfRange_Throws()
    {
        // Arrange
        var data = new double[25];
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frame = new GeoTiffRasterFrame(
            CanonicalTick.Genesis,
            5, 5,
            bounds,
            RasterDataType.Float32,
            data);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetValue(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetValue(5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetValue(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetValue(0, 5));
    }

    [Fact]
    public void GeoTiffRasterFrame_GetRegion_ReturnsCorrectSubset()
    {
        // Arrange
        var data = new double[]
        {
            1,  2,  3,  4,
            5,  6,  7,  8,
            9,  10, 11, 12,
            13, 14, 15, 16
        };
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frame = new GeoTiffRasterFrame(
            CanonicalTick.Genesis,
            4, 4,
            bounds,
            RasterDataType.Float32,
            data);

        // Act
        var region = frame.GetRegion(1, 3, 1, 3);

        // Assert
        region.GetLength(0).Should().Be(2);
        region.GetLength(1).Should().Be(2);
        region[0, 0].Should().Be(6);
        region[0, 1].Should().Be(7);
        region[1, 0].Should().Be(10);
        region[1, 1].Should().Be(11);
    }

    [Fact]
    public void GeoTiffRasterFrame_GetValueAt_ReturnsCorrectValue()
    {
        // Arrange - create a 2x2 raster covering -180 to 180 lon, -90 to 90 lat
        var data = new double[] { 1.0, 2.0, 3.0, 4.0 };
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frame = new GeoTiffRasterFrame(
            CanonicalTick.Genesis,
            2, 2,
            bounds,
            RasterDataType.Float32,
            data);

        // Act & Assert - check corners and out-of-bounds
        frame.GetValueAt(-180, 90).Should().NotBeNull(); // Top-left
        frame.GetValueAt(180, -90).Should().NotBeNull(); // Bottom-right
        frame.GetValueAt(200, 0).Should().BeNull(); // Out of bounds
    }

    [Fact]
    public void GeoTiffRasterFrame_NoDataValue_ReturnsNull()
    {
        // Arrange
        var noDataValue = -9999.0;
        var data = new double[] { 1.0, noDataValue, 3.0, 4.0 };
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frame = new GeoTiffRasterFrame(
            CanonicalTick.Genesis,
            2, 2,
            bounds,
            RasterDataType.Float32,
            data,
            noDataValue);

        // Act & Assert
        frame.GetValue(0, 0).Should().Be(1.0);
        frame.GetValue(0, 1).Should().BeNull(); // NoData
        frame.GetValue(1, 0).Should().Be(3.0);
        frame.GetValue(1, 1).Should().Be(4.0);
    }

    [Fact]
    public void GeoTiffRasterSequence_QueryAt_NearestNeighbor_SelectsClosestFrame()
    {
        // Arrange
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frames = new[]
        {
            (new CanonicalTick(0), CreateTestFrame(new CanonicalTick(0), bounds)),
            (new CanonicalTick(100), CreateTestFrame(new CanonicalTick(100), bounds)),
            (new CanonicalTick(200), CreateTestFrame(new CanonicalTick(200), bounds))
        };
        var sequence = new GeoTiffRasterSequence("test", "Test Sequence", frames);

        // Act & Assert
        var result50 = sequence.QueryAt(new CanonicalTick(50), RasterQueryOptions.WithNearestNeighbor());
        result50.HasData.Should().BeTrue();

        var result75 = sequence.QueryAt(new CanonicalTick(75), RasterQueryOptions.WithNearestNeighbor());
        result75.HasData.Should().BeTrue();
    }

    [Fact]
    public void GeoTiffRasterSequence_QueryAt_Linear_ReturnsInterpolatedResult()
    {
        // Arrange
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frames = new[]
        {
            (new CanonicalTick(0), CreateTestFrame(new CanonicalTick(0), bounds, value: 0)),
            (new CanonicalTick(100), CreateTestFrame(new CanonicalTick(100), bounds, value: 100))
        };
        var sequence = new GeoTiffRasterSequence("test", "Test Sequence", frames);

        // Act
        var result = sequence.QueryAt(new CanonicalTick(50), RasterQueryOptions.WithLinearInterpolation());

        // Assert
        result.HasData.Should().BeTrue();
        result.IsInterpolated.Should().BeTrue();
        result.InterpolationWeight.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void GeoTiffRasterSequence_GetFramesInRange_ReturnsCorrectFrames()
    {
        // Arrange
        var bounds = new RasterBounds(-180, 180, -90, 90);
        var frames = new[]
        {
            (new CanonicalTick(0), CreateTestFrame(new CanonicalTick(0), bounds)),
            (new CanonicalTick(100), CreateTestFrame(new CanonicalTick(100), bounds)),
            (new CanonicalTick(200), CreateTestFrame(new CanonicalTick(200), bounds)),
            (new CanonicalTick(300), CreateTestFrame(new CanonicalTick(300), bounds))
        };
        var sequence = new GeoTiffRasterSequence("test", "Test Sequence", frames);

        // Act
        var inRange = sequence.GetFramesInRange(new CanonicalTick(50), new CanonicalTick(250)).ToList();

        // Assert
        inRange.Should().HaveCount(2);
        inRange[0].Tick.Should().Be(new CanonicalTick(100));
        inRange[1].Tick.Should().Be(new CanonicalTick(200));
    }

    [Fact]
    public void CalculateBounds_UnrotatedRaster_ReturnsCorrectBounds()
    {
        // Arrange - standard global raster
        var width = 360;
        var height = 180;
        var geoTransform = new[] { -180.0, 1.0, 0.0, 90.0, 0.0, -1.0 };

        // Act
        var bounds = GeoTiffRasterLoader.CalculateBounds(width, height, geoTransform);

        // Assert
        bounds.MinLongitude.Should().BeApproximately(-180, 0.001);
        bounds.MaxLongitude.Should().BeApproximately(180, 0.001);
        bounds.MinLatitude.Should().BeApproximately(-90, 0.001);
        bounds.MaxLatitude.Should().BeApproximately(90, 0.001);
    }

    #region Test Helpers

    private static GeoTiffRasterFrame CreateTestFrame(CanonicalTick tick, RasterBounds bounds, double value = 0)
    {
        var data = new double[4];
        Array.Fill(data, value);
        return new GeoTiffRasterFrame(
            tick,
            2, 2,
            bounds,
            RasterDataType.Float32,
            data);
    }

    private void CreateSimpleTiff(string path, int width, int height, float[]? values = null)
    {
        using var tiff = Tiff.Open(path, "w");
        if (tiff == null) throw new InvalidOperationException($"Failed to create TIFF: {path}");

        tiff.SetField(TiffTag.IMAGEWIDTH, width);
        tiff.SetField(TiffTag.IMAGELENGTH, height);
        tiff.SetField(TiffTag.BITSPERSAMPLE, 32);
        tiff.SetField(TiffTag.SAMPLEFORMAT, (int)SampleFormat.IEEEFP);
        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
        tiff.SetField(TiffTag.ROWSPERSTRIP, height);
        tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
        tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

        // Create data - ensure it's the right size
        var dataSize = width * height;
        var data = values != null && values.Length >= dataSize
            ? values
            : CreateGradientData(width, height);

        var byteData = new byte[width * sizeof(float)];

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var index = row * width + col;
                var value = index < data.Length ? data[index] : (float)index;
                var bytes = BitConverter.GetBytes(value);
                Buffer.BlockCopy(bytes, 0, byteData, col * sizeof(float), sizeof(float));
            }
            tiff.WriteScanline(byteData, row);
        }
    }

    private void CreateMultiBandTiff(string path, int width, int height, int bands)
    {
        using var tiff = Tiff.Open(path, "w");
        if (tiff == null) throw new InvalidOperationException($"Failed to create TIFF: {path}");

        tiff.SetField(TiffTag.IMAGEWIDTH, width);
        tiff.SetField(TiffTag.IMAGELENGTH, height);
        tiff.SetField(TiffTag.BITSPERSAMPLE, 32);
        tiff.SetField(TiffTag.SAMPLEFORMAT, (int)SampleFormat.IEEEFP);
        tiff.SetField(TiffTag.SAMPLESPERPIXEL, bands);
        tiff.SetField(TiffTag.ROWSPERSTRIP, height);
        tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
        tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

        var bandData = new float[bands];
        var byteData = new byte[width * bands * sizeof(float)];

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                for (int band = 0; band < bands; band++)
                {
                    bandData[band] = (float)(band * 100 + row * width + col);
                }

                for (int band = 0; band < bands; band++)
                {
                    var bytes = BitConverter.GetBytes(bandData[band]);
                    var offset = (col * bands + band) * sizeof(float);
                    Buffer.BlockCopy(bytes, 0, byteData, offset, sizeof(float));
                }
            }
            tiff.WriteScanline(byteData, row);
        }
    }

    private static float[] CreateGradientData(int width, int height)
    {
        var data = new float[width * height];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }
        return data;
    }

    #endregion
}
