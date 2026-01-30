// =========================================================================
// RFC-V2-0028 §8.2.1: Planet-Agnostic CRS Policy Tests
// =========================================================================
// These tests verify that the GeoTiff exporter handles coordinate reference
// systems correctly:
// - GeoKeyDirectory tag (34735) is ONLY written when an EPSG code is declared
// - Geotransform tags (PixelScale 33550, TiePoints 33922) are ALWAYS written
// =========================================================================

using System.Collections.Immutable;
using BitMiracle.LibTiff.Classic;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Export;
using Xunit;

namespace FantaSim.Raster.GeoTiff.Tests;

/// <summary>
/// Tests for GeoTiff CRS handling per RFC-V2-0028 §8.2.1.
/// </summary>
public sealed class GeoTiffRasterExporterCrsTests : IDisposable
{
    #region Constants

    // GeoTiff tags
    private const TiffTag TIFFTAG_GEOPIXELSCALE = (TiffTag)33550;
    private const TiffTag TIFFTAG_GEOTIEPOINTS = (TiffTag)33922;
    private const TiffTag TIFFTAG_GEOKEYDIRECTORY = (TiffTag)34735;

    #endregion

    #region Test Setup

    private readonly string _tempDir;
    private readonly GeoTiffRasterExporter _exporter;

    public GeoTiffRasterExporterCrsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"geotiff_crs_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _exporter = new GeoTiffRasterExporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); }
            catch { /* Ignore cleanup errors */ }
        }
    }

    #endregion

    #region 1️⃣ GeoKeyDirectory Omission Tests

    /// <summary>
    /// RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted when CoordinateSystem is null.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemIsNull()
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: null);
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull("should be able to open exported file");

        var hasGeoKeyDirectory = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY) != null;
        hasGeoKeyDirectory.Should().BeFalse(
            "RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted when CoordinateSystem is null");

        // Geotransform tags should ALWAYS be present
        var hasPixelScale = tiff.GetField(TIFFTAG_GEOPIXELSCALE) != null;
        var hasTiePoints = tiff.GetField(TIFFTAG_GEOTIEPOINTS) != null;
        hasPixelScale.Should().BeTrue("PixelScale tag must always be written");
        hasTiePoints.Should().BeTrue("TiePoints tag must always be written");
    }

    /// <summary>
    /// RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted when CoordinateSystem is empty string.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemIsEmpty()
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: "");
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull();

        var hasGeoKeyDirectory = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY) != null;
        hasGeoKeyDirectory.Should().BeFalse(
            "RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted when CoordinateSystem is empty");
    }

    /// <summary>
    /// RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted for custom/unknown CRS.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemIsCustom()
    {
        // Arrange - custom CRS that doesn't match EPSG pattern
        var sequence = CreateTestSequence(coordinateSystem: "Custom_Lambert_Conformal");
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull();

        var hasGeoKeyDirectory = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY) != null;
        hasGeoKeyDirectory.Should().BeFalse(
            "RFC-V2-0028 §8.2.1: GeoKeyDirectory must be omitted for custom CRS without EPSG code");
    }

    #endregion

    #region 2️⃣ GeoKeyDirectory Inclusion Tests

    /// <summary>
    /// RFC-V2-0028 §8.2.1: GeoKeyDirectory must be written when CoordinateSystem has EPSG code.
    /// </summary>
    [Fact]
    public async Task Export_WritesGeoKeyDirectory_WhenCoordinateSystemHasEpsg()
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: "EPSG:4326");
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull();

        var hasGeoKeyDirectory = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY) != null;
        hasGeoKeyDirectory.Should().BeTrue(
            "RFC-V2-0028 §8.2.1: GeoKeyDirectory must be written when EPSG code is declared");
    }

    /// <summary>
    /// RFC-V2-0028 §8.2.1: GeoKeyDirectory must be written for raw EPSG code format.
    /// </summary>
    [Fact]
    public async Task Export_WritesGeoKeyDirectory_WhenCoordinateSystemIsRawEpsgCode()
    {
        // Arrange - raw numeric EPSG code
        var sequence = CreateTestSequence(coordinateSystem: "4326");
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull();

        var hasGeoKeyDirectory = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY) != null;
        hasGeoKeyDirectory.Should().BeTrue(
            "RFC-V2-0028 §8.2.1: GeoKeyDirectory must be written for raw EPSG code");
    }

    #endregion

    #region 3️⃣ EPSG Code Parsing Tests

    /// <summary>
    /// RFC-V2-0028 §8.2.1: Various EPSG code formats should be correctly parsed.
    /// </summary>
    [Theory]
    [InlineData("EPSG:4326", (ushort)4326)]
    [InlineData("epsg:4326", (ushort)4326)]  // case-insensitive
    [InlineData("4326", (ushort)4326)]       // raw code
    public async Task Export_WritesCorrectEpsgCode_ForVariousFormats(string coordinateSystem, ushort expectedCode)
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: coordinateSystem);
        var spec = CreateExportSpec();

        // Act
        var result = await _exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue("export should succeed");
        result.OutputFiles.Should().NotBeEmpty();

        var tiffPath = result.OutputFiles[0];
        using var tiff = Tiff.Open(tiffPath, "r");
        tiff.Should().NotBeNull();

        var geoKeyField = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyField.Should().NotBeNull("GeoKeyDirectory should exist for EPSG CRS");

        // GeoKeyDirectory format: array of ushorts
        // [0] = KeyDirectoryVersion (1)
        // [1] = KeyRevision (1)
        // [2] = MinorRevision (0)
        // [3] = NumberOfKeys
        // Then key entries: KeyID, TIFFTagLocation, Count, Value/Offset
        // GeographicTypeGeoKey (2048) = EPSG code
        var geoKeys = geoKeyField![1].ToShortArray();
        geoKeys.Should().NotBeNull();

        // Find GeographicTypeGeoKey (2048)
        var foundEpsgCode = FindGeoKeyValue(geoKeys!, 2048);
        foundEpsgCode.Should().Be(expectedCode,
            $"EPSG code should be correctly encoded for input '{coordinateSystem}'");
    }

    #endregion

    #region Test Helpers

    private RasterExportSpec CreateExportSpec()
    {
        return new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: new CanonicalTick(0),
            EndTick: new CanonicalTick(0),
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: RasterQueryOptions.Default,
            OutputDirectory: _tempDir,
            FileNameTemplate: "test_{tick}.tif"
        );
    }

    private static TestRasterSequence CreateTestSequence(string? coordinateSystem)
    {
        var metadata = new RasterMetadata(
            Width: 10,
            Height: 10,
            Bounds: new RasterBounds(-180, 180, -90, 90),
            DataType: RasterDataType.Float64,
            NoDataValue: -9999.0,
            CoordinateSystem: coordinateSystem,
            Units: "meters"
        );

        return new TestRasterSequence("test-sequence", "Test Raster", metadata);
    }

    /// <summary>
    /// Finds a GeoKey value in the GeoKeyDirectory array.
    /// </summary>
    private static ushort? FindGeoKeyValue(short[] geoKeys, ushort keyId)
    {
        if (geoKeys.Length < 4)
            return null;

        var numberOfKeys = geoKeys[3];

        // Each key entry is 4 shorts starting at index 4
        for (int i = 0; i < numberOfKeys; i++)
        {
            var offset = 4 + (i * 4);
            if (offset + 3 >= geoKeys.Length)
                break;

            var currentKeyId = (ushort)geoKeys[offset];
            if (currentKeyId == keyId)
            {
                // TIFFTagLocation = 0 means value is stored directly
                var tiffTagLocation = geoKeys[offset + 1];
                if (tiffTagLocation == 0)
                {
                    return (ushort)geoKeys[offset + 3];
                }
            }
        }

        return null;
    }

    #endregion

    #region Test Types

    private sealed class TestRasterSequence : IRasterSequence
    {
        private readonly TestRasterFrame _frame;

        public TestRasterSequence(string sequenceId, string displayName, RasterMetadata metadata)
        {
            SequenceId = sequenceId;
            DisplayName = displayName;
            Metadata = metadata;
            AvailableTicks = ImmutableArray.Create(new CanonicalTick(0));
            _frame = new TestRasterFrame(new CanonicalTick(0), metadata);
        }

        public string SequenceId { get; }
        public string DisplayName { get; }
        public ImmutableArray<CanonicalTick> AvailableTicks { get; }
        public RasterMetadata Metadata { get; }

        public IRasterFrame? GetFrameAt(CanonicalTick tick)
        {
            return tick.Value == 0 ? _frame : null;
        }

        public RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null)
        {
            var frame = GetFrameAt(tick);
            if (frame == null)
                return RasterQueryResult.NotFound(tick);

            var frameData = new RasterFrameData(
                frame.Width,
                frame.Height,
                frame.Bounds,
                frame.DataType,
                frame.NoDataValue,
                frame.GetRawData().ToArray()
            );

            return RasterQueryResult.Exact(tick, frameData);
        }

        public IEnumerable<IRasterFrame> GetFramesInRange(CanonicalTick startTick, CanonicalTick endTick)
        {
            if (startTick.Value <= 0 && endTick.Value >= 0)
                yield return _frame;
        }
    }

    private sealed class TestRasterFrame : IRasterFrame
    {
        private readonly byte[] _data;

        public TestRasterFrame(CanonicalTick tick, RasterMetadata metadata)
        {
            Tick = tick;
            Width = metadata.Width;
            Height = metadata.Height;
            Bounds = metadata.Bounds;
            DataType = metadata.DataType;
            NoDataValue = metadata.NoDataValue;

            // Create test data (all zeros)
            _data = new byte[Width * Height * sizeof(double)];
        }

        public CanonicalTick Tick { get; }
        public int Width { get; }
        public int Height { get; }
        public RasterBounds Bounds { get; }
        public RasterDataType DataType { get; }
        public double? NoDataValue { get; }

        public ReadOnlySpan<byte> GetRawData() => _data;

        public double? GetValue(int row, int col)
        {
            if (row < 0 || row >= Height || col < 0 || col >= Width)
                return null;
            return 0.0;
        }

        public double? GetValueAt(double longitude, double latitude)
        {
            if (!Bounds.Contains(longitude, latitude))
                return null;
            return 0.0;
        }
    }

    #endregion
}
