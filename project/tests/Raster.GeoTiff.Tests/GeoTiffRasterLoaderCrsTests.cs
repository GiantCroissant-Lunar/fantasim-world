// =========================================================================
// RFC-V2-0028: GeoTiff Loader CRS Symmetry Tests
// =========================================================================
// These tests verify that the GeoTiff loader correctly parses coordinate
// reference system information, ensuring symmetry with the exporter:
// - Missing GeoKeyDirectory → CoordinateSystem == null
// - EPSG codes normalized to canonical "EPSG:XXXX" format
// =========================================================================

using System.Collections.Immutable;
using BitMiracle.LibTiff.Classic;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Export;
using FantaSim.Raster.GeoTiff.Internal;
using Xunit;

namespace FantaSim.Raster.GeoTiff.Tests;

/// <summary>
/// Tests for GeoTiff loader CRS symmetry per RFC-V2-0028.
/// Ensures round-trip consistency with the exporter.
/// </summary>
public sealed class GeoTiffRasterLoaderCrsTests : IDisposable
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

    public GeoTiffRasterLoaderCrsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"geotiff_loader_crs_test_{Guid.NewGuid():N}");
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

    #region 1️⃣ Loader Symmetry: Missing GeoKeyDirectory

    /// <summary>
    /// RFC-V2-0028 Symmetry: When GeoKeyDirectory is missing, loader must return null CRS.
    /// This prevents "export is planet-agnostic but import silently normalizes into Earth CRS" drift.
    /// </summary>
    [Fact]
    public void Load_ReturnsNullCoordinateSystem_WhenGeoKeyDirectoryMissing()
    {
        // Arrange: Create a GeoTiff without GeoKeyDirectory
        var sequence = CreateTestSequence(coordinateSystem: null);
        var spec = CreateExportSpec();
        var result = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        var tiffPath = result.OutputFiles[0];

        // Act: Read using TiffReader
        using var reader = TiffReader.Open(tiffPath);

        // Assert: CoordinateReferenceSystem should be null
        reader.CoordinateReferenceSystem.Should().BeNull(
            "RFC-V2-0028 Symmetry: Missing GeoKeyDirectory must result in null CRS");
    }

    /// <summary>
    /// RFC-V2-0028 Symmetry: Custom CRS without EPSG code must not produce a CRS string.
    /// </summary>
    [Fact]
    public void Load_ReturnsNullCoordinateSystem_WhenCrsIsCustom()
    {
        // Arrange: Create a GeoTiff with custom CRS (no EPSG)
        var sequence = CreateTestSequence(coordinateSystem: "Custom_Lambert_Conformal");
        var spec = CreateExportSpec();
        var result = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        var tiffPath = result.OutputFiles[0];

        // Act: Read using TiffReader
        using var reader = TiffReader.Open(tiffPath);

        // Assert: No GeoKeyDirectory means no CRS
        reader.CoordinateReferenceSystem.Should().BeNull(
            "RFC-V2-0028 Symmetry: Custom CRS without EPSG must result in null CRS");
    }

    #endregion

    #region 2️⃣ Loader Symmetry: EPSG Normalization

    /// <summary>
    /// RFC-V2-0028 Symmetry: EPSG codes must be normalized to canonical "EPSG:XXXX" format.
    /// </summary>
    [Theory]
    [InlineData("EPSG:4326")]
    [InlineData("epsg:4326")]
    [InlineData("4326")]
    public void Load_NormalizesEpsgToCanonicalFormat(string inputCrs)
    {
        // Arrange: Create a GeoTiff with various EPSG formats
        var sequence = CreateTestSequence(coordinateSystem: inputCrs);
        var spec = CreateExportSpec();
        var result = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        var tiffPath = result.OutputFiles[0];

        // Act: Read using TiffReader
        using var reader = TiffReader.Open(tiffPath);

        // Assert: CRS should be normalized to "EPSG:4326"
        reader.CoordinateReferenceSystem.Should().Be("EPSG:4326",
            $"RFC-V2-0028 Symmetry: Input '{inputCrs}' must normalize to 'EPSG:4326'");
    }

    /// <summary>
    /// RFC-V2-0028 Symmetry: Different EPSG codes must be correctly read.
    /// </summary>
    [Theory]
    [InlineData("EPSG:32610", 32610)]   // UTM Zone 10N
    [InlineData("EPSG:3857", 3857)]     // Web Mercator
    [InlineData("EPSG:4269", 4269)]     // NAD83
    public void Load_ReadsCorrectEpsgCode(string inputCrs, int expectedCode)
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: inputCrs);
        var spec = CreateExportSpec();
        var result = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        var tiffPath = result.OutputFiles[0];

        // Act
        using var reader = TiffReader.Open(tiffPath);

        // Assert
        reader.CoordinateReferenceSystem.Should().Be($"EPSG:{expectedCode}",
            $"EPSG code {expectedCode} should be correctly parsed from GeoKeyDirectory");
    }

    #endregion

    #region 3️⃣ Round-Trip Consistency

    /// <summary>
    /// RFC-V2-0028: Export→Load round-trip must preserve CRS for EPSG codes.
    /// </summary>
    [Fact]
    public void RoundTrip_PreservesCrs_ForEpsgCode()
    {
        // Arrange
        const string originalCrs = "EPSG:4326";
        var sequence = CreateTestSequence(coordinateSystem: originalCrs);
        var spec = CreateExportSpec();

        // Act: Export
        var exportResult = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();
        exportResult.Success.Should().BeTrue();

        // Act: Load
        using var reader = TiffReader.Open(exportResult.OutputFiles[0]);

        // Assert: CRS preserved
        reader.CoordinateReferenceSystem.Should().Be(originalCrs,
            "Round-trip must preserve EPSG CRS exactly");
    }

    /// <summary>
    /// RFC-V2-0028: Export→Load round-trip must preserve null CRS.
    /// </summary>
    [Fact]
    public void RoundTrip_PreservesNullCrs()
    {
        // Arrange
        var sequence = CreateTestSequence(coordinateSystem: null);
        var spec = CreateExportSpec();

        // Act: Export
        var exportResult = _exporter.ExportAsync(sequence, spec).GetAwaiter().GetResult();
        exportResult.Success.Should().BeTrue();

        // Act: Load
        using var reader = TiffReader.Open(exportResult.OutputFiles[0]);

        // Assert: CRS still null
        reader.CoordinateReferenceSystem.Should().BeNull(
            "Round-trip must preserve null CRS (planet-agnostic data)");
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
