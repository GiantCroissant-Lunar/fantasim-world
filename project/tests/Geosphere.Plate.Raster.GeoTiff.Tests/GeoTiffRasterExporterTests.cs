using System.Collections.Immutable;
using BitMiracle.LibTiff.Classic;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Export;
using NSubstitute;

namespace FantaSim.Geosphere.Plate.Raster.GeoTiff.Tests;

/// <summary>
/// Tests for <see cref="GeoTiffRasterExporter"/>.
/// Includes planet-agnostic CRS policy tests per RFC-V2-0028.
/// </summary>
public class GeoTiffRasterExporterTests : IDisposable
{
    // GeoTiff tag constants for verification
    private const TiffTag TIFFTAG_GEOPIXELSCALE = (TiffTag)33550;
    private const TiffTag TIFFTAG_GEOTIEPOINTS = (TiffTag)33922;
    private const TiffTag TIFFTAG_GEOKEYDIRECTORY = (TiffTag)34735;

    private readonly string _testOutputDir;

    public GeoTiffRasterExporterTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"geotiff_export_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Planet-Agnostic CRS Policy Tests

    /// <summary>
    /// Verifies that GeoKeyDirectory is omitted when CoordinateSystem is null.
    /// This enforces the planet-agnostic doctrine: never invent Earth CRS.
    /// RFC-V2-0028 compliance test.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemMissing()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: null);
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "nocrs_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputFiles.Should().HaveCount(1);

        // Verify GeoKeyDirectory is absent
        var outputFile = result.OutputFiles.First();
        using var tiff = Tiff.Open(outputFile, "r");
        tiff.Should().NotBeNull("Output file should be readable");

        // GeoKeyDirectory should NOT be present (planet-agnostic: no CRS assumption)
        var geoKeyResult = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyResult.Should().BeNull("GeoKeyDirectory should be absent when CoordinateSystem is null");

        // Geotransform tags SHOULD still be present (pixel↔coordinate mapping)
        var pixelScaleResult = tiff.GetField(TIFFTAG_GEOPIXELSCALE);
        pixelScaleResult.Should().NotBeNull("PixelScale should always be written for geotransform");

        var tiePointsResult = tiff.GetField(TIFFTAG_GEOTIEPOINTS);
        tiePointsResult.Should().NotBeNull("TiePoints should always be written for geotransform");
    }

    /// <summary>
    /// Verifies that GeoKeyDirectory is omitted when CoordinateSystem is empty string.
    /// RFC-V2-0028 compliance test.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemEmpty()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: "");
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "emptycrs_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();

        var outputFile = result.OutputFiles.First();
        using var tiff = Tiff.Open(outputFile, "r");

        var geoKeyResult = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyResult.Should().BeNull("GeoKeyDirectory should be absent when CoordinateSystem is empty");
    }

    /// <summary>
    /// Verifies that GeoKeyDirectory is written when CoordinateSystem contains valid EPSG code.
    /// RFC-V2-0028 compliance test.
    /// </summary>
    [Fact]
    public async Task Export_WritesGeoKeyDirectory_WhenCoordinateSystemHasEpsg()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: "EPSG:4326");
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "withcrs_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputFiles.Should().HaveCount(1);

        var outputFile = result.OutputFiles.First();
        using var tiff = Tiff.Open(outputFile, "r");
        tiff.Should().NotBeNull("Output file should be readable");

        // GeoKeyDirectory SHOULD be present with EPSG code
        var geoKeyResult = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyResult.Should().NotBeNull("GeoKeyDirectory should be present when CoordinateSystem has EPSG code");

        // Verify EPSG code is correct (4326 in this case)
        var geoKeys = geoKeyResult![1].ToShortArray();
        geoKeys.Should().NotBeNull();

        // GeoKeyDirectory format: Version(1,1,0), NumKeys, then key entries
        // Key 2048 (GeographicTypeGeoKey) should have value 4326
        // Structure: KeyID=2048, TIFFTagLocation=0, Count=1, Value=4326
        geoKeys.Should().Contain((short)4326, "EPSG:4326 should be encoded in GeoKeyDirectory");
    }

    /// <summary>
    /// Verifies that raw integer EPSG codes are also supported (e.g., "4326" without "EPSG:" prefix).
    /// RFC-V2-0028 compliance test.
    /// </summary>
    [Fact]
    public async Task Export_WritesGeoKeyDirectory_WhenCoordinateSystemIsRawEpsgCode()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: "4326"); // Raw integer format
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "rawepsg_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();

        var outputFile = result.OutputFiles.First();
        using var tiff = Tiff.Open(outputFile, "r");

        var geoKeyResult = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyResult.Should().NotBeNull("GeoKeyDirectory should be present for raw EPSG code");

        var geoKeys = geoKeyResult![1].ToShortArray();
        geoKeys.Should().Contain((short)4326);
    }

    /// <summary>
    /// Verifies that non-EPSG coordinate systems (custom/body-frame CRS) result in omitted GeoKeyDirectory.
    /// This is planet-agnostic safe: we don't lie about what CRS we have.
    /// RFC-V2-0028 compliance test.
    /// </summary>
    [Fact]
    public async Task Export_OmitsGeoKeyDirectory_WhenCoordinateSystemIsCustom()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: "BODY_FRAME:MARS"); // Custom CRS
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "customcrs_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();

        var outputFile = result.OutputFiles.First();
        using var tiff = Tiff.Open(outputFile, "r");

        // Custom CRS should NOT produce GeoKeyDirectory (can't encode as EPSG)
        var geoKeyResult = tiff!.GetField(TIFFTAG_GEOKEYDIRECTORY);
        geoKeyResult.Should().BeNull("GeoKeyDirectory should be absent for non-EPSG coordinate systems");

        // But geotransform should still be present
        var pixelScaleResult = tiff.GetField(TIFFTAG_GEOPIXELSCALE);
        pixelScaleResult.Should().NotBeNull("PixelScale should be written even without CRS");
    }

    #endregion

    #region Basic Export Tests

    [Fact]
    public async Task Export_SingleFrame_CreatesFile()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var sequence = CreateTestSequence(coordinateSystem: null);
        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: CanonicalTick.Genesis,
            EndTick: CanonicalTick.Genesis,
            TickStep: 1,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "single_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputFiles.Should().HaveCount(1);
        result.FramesExported.Should().Be(1);
        result.Errors.Should().BeEmpty();
        File.Exists(result.OutputFiles.First()).Should().BeTrue();
    }

    [Fact]
    public async Task Export_MultipleFrames_CreatesMultipleFiles()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();
        var tick0 = new CanonicalTick(0);
        var tick10 = new CanonicalTick(10);
        var tick20 = new CanonicalTick(20);
        var sequence = CreateTestSequence(
            coordinateSystem: null,
            ticks: new[] { tick0, tick10, tick20 });

        var spec = new RasterExportSpec(
            SequenceId: "test-sequence",
            StartTick: tick0,
            EndTick: tick20,
            TickStep: 10,
            Format: RasterExportFormat.GeoTiff,
            QueryOptions: default,
            OutputDirectory: _testOutputDir,
            FileNameTemplate: "multi_{tick}.tif");

        // Act
        var result = await exporter.ExportAsync(sequence, spec);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputFiles.Should().HaveCount(3);
        result.FramesExported.Should().Be(3);
    }

    [Fact]
    public void SupportedFormats_IncludesGeoTiff()
    {
        // Arrange
        var exporter = new GeoTiffRasterExporter();

        // Act & Assert
        exporter.SupportedFormats.Should().Contain(RasterExportFormat.GeoTiff);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Creates a test sequence with configurable CRS for testing export behavior.
    /// </summary>
    private static IRasterSequence CreateTestSequence(
        string? coordinateSystem,
        CanonicalTick[]? ticks = null)
    {
        ticks ??= new[] { CanonicalTick.Genesis };

        var bounds = new RasterBounds(-180, 180, -90, 90);
        var metadata = new RasterMetadata(
            Width: 10,
            Height: 10,
            Bounds: bounds,
            DataType: RasterDataType.Float64,
            NoDataValue: -9999.0,
            CoordinateSystem: coordinateSystem,
            Units: null);

        return new TestRasterSequence("test-sequence", metadata, ticks);
    }

    /// <summary>
    /// Simple test implementation of IRasterSequence that uses concrete frames.
    /// </summary>
    private sealed class TestRasterSequence : IRasterSequence
    {
        private readonly Dictionary<CanonicalTick, IRasterFrame> _frames;

        public TestRasterSequence(string sequenceId, RasterMetadata metadata, CanonicalTick[] ticks)
        {
            SequenceId = sequenceId;
            DisplayName = sequenceId;
            Metadata = metadata;
            AvailableTicks = ticks.ToImmutableArray();

            _frames = new Dictionary<CanonicalTick, IRasterFrame>();
            foreach (var tick in ticks)
            {
                _frames[tick] = new TestRasterFrame(tick, metadata);
            }
        }

        public string SequenceId { get; }
        public string DisplayName { get; }
        public RasterMetadata Metadata { get; }
        public ImmutableArray<CanonicalTick> AvailableTicks { get; }

        public IRasterFrame? GetFrameAt(CanonicalTick tick)
        {
            return _frames.TryGetValue(tick, out var frame) ? frame : null;
        }

        public RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null)
        {
            var frame = GetFrameAt(tick);
            if (frame == null)
            {
                return RasterQueryResult.NotFound(tick);
            }

            var frameData = new RasterFrameData(
                Width: frame.Width,
                Height: frame.Height,
                Bounds: frame.Bounds,
                DataType: frame.DataType,
                NoDataValue: frame.NoDataValue,
                RawData: frame.GetRawData().ToArray());

            return RasterQueryResult.Exact(tick, frameData);
        }

        public IEnumerable<IRasterFrame> GetFramesInRange(CanonicalTick startTick, CanonicalTick endTick)
        {
            return _frames.Where(kv => kv.Key >= startTick && kv.Key <= endTick)
                          .OrderBy(kv => kv.Key)
                          .Select(kv => kv.Value);
        }
    }

    /// <summary>
    /// Simple test implementation of IRasterFrame with concrete data.
    /// </summary>
    private sealed class TestRasterFrame : IRasterFrame
    {
        private readonly byte[] _rawData;
        private readonly double[] _values;

        public TestRasterFrame(CanonicalTick tick, RasterMetadata metadata)
        {
            Tick = tick;
            Width = metadata.Width;
            Height = metadata.Height;
            Bounds = metadata.Bounds;
            DataType = metadata.DataType;
            NoDataValue = metadata.NoDataValue;

            // Create test data (10x10 = 100 doubles = 800 bytes)
            _values = new double[Width * Height];
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = i * 0.1;
            }

            _rawData = new byte[_values.Length * sizeof(double)];
            Buffer.BlockCopy(_values, 0, _rawData, 0, _rawData.Length);
        }

        public CanonicalTick Tick { get; }
        public int Width { get; }
        public int Height { get; }
        public RasterBounds Bounds { get; }
        public RasterDataType DataType { get; }
        public double? NoDataValue { get; }

        public ReadOnlySpan<byte> GetRawData() => _rawData;

        public double? GetValue(int row, int col)
        {
            if (row < 0 || row >= Height)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (col < 0 || col >= Width)
                throw new ArgumentOutOfRangeException(nameof(col));

            var index = row * Width + col;
            var value = _values[index];

            // Check for no-data
            if (NoDataValue.HasValue && Math.Abs(value - NoDataValue.Value) < 1e-10)
                return null;

            return value;
        }

        public double? GetValueAt(double longitude, double latitude)
        {
            // Simple implementation for testing - just return a value if in bounds
            if (longitude < Bounds.MinLongitude || longitude > Bounds.MaxLongitude ||
                latitude < Bounds.MinLatitude || latitude > Bounds.MaxLatitude)
                return null;

            return 0.0;
        }
    }

    #endregion
}
