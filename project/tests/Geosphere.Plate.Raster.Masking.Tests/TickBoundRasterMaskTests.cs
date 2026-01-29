using System.Collections.Immutable;
using FluentAssertions;
using NSubstitute;
using Xunit;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Raster.Masking.Tests;

/// <summary>
/// Tests for tick-bound raster masking functionality.
/// RFC-V2-0028 Phase 4 compliance tests.
/// </summary>
public sealed class TickBoundRasterMaskTests
{
    private const double NoDataValue = -9999.0;

    // Stable test GUIDs for deterministic tests
    private static readonly Guid Plate1Guid = new("00000001-0000-0000-0000-000000000001");
    private static readonly Guid Plate2Guid = new("00000002-0000-0000-0000-000000000002");

    #region PlatePolygonRasterMaskFactory Tests

    [Fact]
    public void MaskFactory_Deterministic_ForSameTick()
    {
        // Arrange: Create a factory with a deterministic polygonizer
        var tick = CanonicalTick.Genesis;
        var polygonSet = CreateTestPolygonSet(new PlateId(Plate1Guid));

        var polygonizer = Substitute.For<IPlatePolygonizer>();
        polygonizer.PolygonizeAtTick(tick, Arg.Any<IPlateTopologyStateView>(), Arg.Any<PolygonizationOptions?>())
            .Returns(polygonSet);

        var topology = Substitute.For<IPlateTopologyStateView>();
        var factory = new PlatePolygonRasterMaskFactory(polygonizer, topology);

        // Act: Create masks for the same tick twice
        var mask1 = factory.CreateMask(tick);
        var mask2 = factory.CreateMask(tick);

        // Assert: Both masks should produce the same result for the same point
        var lon = 0.5;
        var lat = 0.5;
        mask1.Contains(lon, lat).Should().Be(mask2.Contains(lon, lat),
            "masks for the same tick should be deterministic");
    }

    [Fact]
    public void MaskFactory_CallsPolygonizer_WithCorrectTick()
    {
        // Arrange
        var tick1 = CanonicalTick.Genesis;
        var tick2 = new CanonicalTick(100);
        var polygonSet = CreateTestPolygonSet(new PlateId(Plate1Guid));

        var polygonizer = Substitute.For<IPlatePolygonizer>();
        polygonizer.PolygonizeAtTick(Arg.Any<CanonicalTick>(), Arg.Any<IPlateTopologyStateView>(), Arg.Any<PolygonizationOptions?>())
            .Returns(polygonSet);

        var topology = Substitute.For<IPlateTopologyStateView>();
        var factory = new PlatePolygonRasterMaskFactory(polygonizer, topology);

        // Act
        _ = factory.CreateMask(tick1);
        _ = factory.CreateMask(tick2);

        // Assert: Polygonizer should have been called with both ticks
        polygonizer.Received(1).PolygonizeAtTick(tick1, topology, Arg.Any<PolygonizationOptions?>());
        polygonizer.Received(1).PolygonizeAtTick(tick2, topology, Arg.Any<PolygonizationOptions?>());
    }

    #endregion

    #region Mask_Contains Tests (Point-in-Polygon)

    [Fact]
    public void Mask_IncludesInsidePolygon()
    {
        // Arrange: Simple square polygon from (0,0) to (1,1)
        var plateId = new PlateId(Plate1Guid);
        var polygonSet = CreateTestPolygonSet(plateId, minX: 0, minY: 0, maxX: 1, maxY: 1);
        var mask = new PlatePolygonRasterMask(polygonSet);

        // Act & Assert: Point inside should be included
        mask.Contains(0.5, 0.5).Should().BeTrue("point (0.5, 0.5) is inside the polygon (0,0)-(1,1)");
        mask.Contains(0.1, 0.1).Should().BeTrue("point (0.1, 0.1) is inside the polygon");
        mask.Contains(0.9, 0.9).Should().BeTrue("point (0.9, 0.9) is inside the polygon");
    }

    [Fact]
    public void Mask_ExcludesOutsidePolygon()
    {
        // Arrange: Simple square polygon from (0,0) to (1,1)
        var plateId = new PlateId(Plate1Guid);
        var polygonSet = CreateTestPolygonSet(plateId, minX: 0, minY: 0, maxX: 1, maxY: 1);
        var mask = new PlatePolygonRasterMask(polygonSet);

        // Act & Assert: Points outside should be excluded
        mask.Contains(-0.5, 0.5).Should().BeFalse("point (-0.5, 0.5) is outside the polygon");
        mask.Contains(1.5, 0.5).Should().BeFalse("point (1.5, 0.5) is outside the polygon");
        mask.Contains(0.5, -0.5).Should().BeFalse("point (0.5, -0.5) is outside the polygon");
        mask.Contains(0.5, 1.5).Should().BeFalse("point (0.5, 1.5) is outside the polygon");
    }

    [Fact]
    public void Mask_HandlesHolesCorrectly()
    {
        // Arrange: Polygon with a hole
        var plateId = new PlateId(Plate1Guid);
        var outerRing = CreateRing(0, 0, 10, 10);  // Outer: 0,0 to 10,10
        var hole = CreateRing(3, 3, 7, 7);          // Hole: 3,3 to 7,7
        var polygon = new PlatePolygon(plateId, outerRing, ImmutableArray.Create(hole));
        var polygonSet = new PlatePolygonSet(
            CanonicalTick.Genesis,
            ImmutableArray.Create(polygon));
        var mask = new PlatePolygonRasterMask(polygonSet);

        // Act & Assert
        mask.Contains(1.0, 1.0).Should().BeTrue("point (1,1) is in outer ring but not in hole");
        mask.Contains(5.0, 5.0).Should().BeFalse("point (5,5) is inside the hole");
        mask.Contains(8.0, 8.0).Should().BeTrue("point (8,8) is in outer ring but not in hole");
        mask.Contains(15.0, 15.0).Should().BeFalse("point (15,15) is completely outside");
    }

    [Fact]
    public void Mask_FiltersSpecificPlates()
    {
        // Arrange: Two plates, only select one
        var plate1 = new PlateId(Plate1Guid);
        var plate2 = new PlateId(Plate2Guid);

        var polygon1 = CreateTestPolygon(plate1, 0, 0, 5, 5);    // Plate 1: 0,0 to 5,5
        var polygon2 = CreateTestPolygon(plate2, 10, 10, 15, 15); // Plate 2: 10,10 to 15,15

        var polygonSet = new PlatePolygonSet(
            CanonicalTick.Genesis,
            ImmutableArray.Create(polygon1, polygon2));

        // Only include plate 1
        var mask = new PlatePolygonRasterMask(polygonSet, new[] { plate1 });

        // Act & Assert
        mask.Contains(2.5, 2.5).Should().BeTrue("point in plate 1 should be included");
        mask.Contains(12.5, 12.5).Should().BeFalse("point in plate 2 should be excluded (not in filter)");
    }

    #endregion

    #region TickBoundMaskedRasterSequence Tests

    [Fact]
    public void MaskedSequence_RespectsTickFrameSelection()
    {
        // Arrange
        var tick1 = CanonicalTick.Genesis;
        var tick2 = new CanonicalTick(100);

        var frame1 = CreateTestFrame(tick1, width: 4, height: 4, fillValue: 1.0);
        var frame2 = CreateTestFrame(tick2, width: 4, height: 4, fillValue: 2.0);

        var sourceSequence = Substitute.For<IRasterSequence>();
        sourceSequence.AvailableTicks.Returns(ImmutableArray.Create(tick1, tick2));
        sourceSequence.SequenceId.Returns("test");
        sourceSequence.DisplayName.Returns("Test");
        sourceSequence.GetFrameAt(tick1).Returns(frame1);
        sourceSequence.GetFrameAt(tick2).Returns(frame2);
        sourceSequence.Metadata.Returns(new RasterMetadata(
            Width: 4,
            Height: 4,
            Bounds: new RasterBounds(0, 1, 0, 1),
            DataType: RasterDataType.Float64,
            NoDataValue: null,
            CoordinateSystem: null,
            Units: null));

        var maskFactory = Substitute.For<ITickBoundRasterMaskFactory>();
        var passThroughMask = new PassThroughMask(); // Mask that includes everything
        maskFactory.CreateMask(Arg.Any<CanonicalTick>()).Returns(passThroughMask);

        var maskedSequence = new TickBoundMaskedRasterSequence(sourceSequence, maskFactory);

        // Act
        var result1 = maskedSequence.GetFrameAt(tick1);
        var result2 = maskedSequence.GetFrameAt(tick2);

        // Assert: Each tick should request its own mask
        maskFactory.Received(1).CreateMask(tick1);
        maskFactory.Received(1).CreateMask(tick2);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public void MaskedSequence_AppliesMaskToFrame()
    {
        // Arrange
        var tick = CanonicalTick.Genesis;
        var frame = CreateTestFrame(tick, width: 4, height: 4, fillValue: 100.0);

        var sourceSequence = Substitute.For<IRasterSequence>();
        sourceSequence.AvailableTicks.Returns(ImmutableArray.Create(tick));
        sourceSequence.SequenceId.Returns("test");
        sourceSequence.DisplayName.Returns("Test");
        sourceSequence.GetFrameAt(tick).Returns(frame);
        sourceSequence.Metadata.Returns(new RasterMetadata(
            Width: 4,
            Height: 4,
            Bounds: new RasterBounds(0, 1, 0, 1),
            DataType: RasterDataType.Float64,
            NoDataValue: null,
            CoordinateSystem: null,
            Units: null));

        // Create mask that covers top-left quadrant in raster coordinates.
        // Frame bounds: MinLon=0, MaxLon=1, MinLat=0, MaxLat=1
        // Top-left pixel (row=0, col=0) maps to (lon=0, lat=1) - raster Y is inverted
        // So mask polygon (0, 0.5) to (0.5, 1) covers the top-left corner
        var polygonSet = CreateTestPolygonSet(new PlateId(Plate1Guid), minX: 0.0, minY: 0.5, maxX: 0.5, maxY: 1.0);
        var plateMask = new PlatePolygonRasterMask(polygonSet);

        var maskFactory = Substitute.For<ITickBoundRasterMaskFactory>();
        maskFactory.CreateMask(tick).Returns(plateMask);

        var maskedSequence = new TickBoundMaskedRasterSequence(sourceSequence, maskFactory, NoDataValue);

        // Act
        var maskedFrame = maskedSequence.GetFrameAt(tick);

        // Assert
        maskedFrame.Should().NotBeNull();

        // Top-left pixel (row=0, col=0) -> (lon=0, lat=1) is inside mask polygon (0, 0.5)-(0.5, 1)
        var topLeftValue = maskedFrame!.GetValue(0, 0);
        topLeftValue.Should().Be(100.0, "top-left pixel (lon=0, lat=1) is inside mask");

        // Bottom-right pixel (row=3, col=3) -> (lon=0.75, lat=0.25) is outside the mask
        var bottomRightValue = maskedFrame.GetValue(3, 3);
        bottomRightValue.Should().BeNull("bottom-right pixel (lon=0.75, lat=0.25) is outside mask");
    }

    #endregion

    #region Test Helpers

    private static PlatePolygonSet CreateTestPolygonSet(PlateId plateId, double minX = 0, double minY = 0, double maxX = 1, double maxY = 1)
    {
        var polygon = CreateTestPolygon(plateId, minX, minY, maxX, maxY);
        return new PlatePolygonSet(CanonicalTick.Genesis, ImmutableArray.Create(polygon));
    }

    private static PlatePolygon CreateTestPolygon(PlateId plateId, double minX, double minY, double maxX, double maxY)
    {
        var ring = CreateRing(minX, minY, maxX, maxY);
        return new PlatePolygon(plateId, ring, ImmutableArray<Polyline3>.Empty);
    }

    private static Polyline3 CreateRing(double minX, double minY, double maxX, double maxY)
    {
        // Create a closed ring (clockwise for outer, counter-clockwise for hole)
        var points = new Point3[]
        {
            new(minX, minY, 0),
            new(maxX, minY, 0),
            new(maxX, maxY, 0),
            new(minX, maxY, 0),
            new(minX, minY, 0), // Close the ring
        };
        return new Polyline3(points);
    }

    private static IRasterFrame CreateTestFrame(CanonicalTick tick, int width, int height, double fillValue)
    {
        return new TestRasterFrame(tick, width, height, fillValue);
    }

    /// <summary>
    /// A simple test raster frame with uniform value.
    /// </summary>
    private sealed class TestRasterFrame : IRasterFrame
    {
        private readonly byte[] _rawData;
        private readonly double _fillValue;

        public TestRasterFrame(CanonicalTick tick, int width, int height, double fillValue)
        {
            Tick = tick;
            Width = width;
            Height = height;
            _fillValue = fillValue;
            // RasterBounds: MinLon, MaxLon, MinLat, MaxLat
            Bounds = new RasterBounds(0, 1, 0, 1); // Unit square: lon 0-1, lat 0-1

            // Fill with uniform value
            var values = new double[width * height];
            Array.Fill(values, fillValue);
            _rawData = new byte[values.Length * sizeof(double)];
            Buffer.BlockCopy(values, 0, _rawData, 0, _rawData.Length);
        }

        public CanonicalTick Tick { get; }
        public int Width { get; }
        public int Height { get; }
        public RasterBounds Bounds { get; }
        public RasterDataType DataType => RasterDataType.Float64;
        public double? NoDataValue => null;

        public ReadOnlySpan<byte> GetRawData() => _rawData;

        public double? GetValue(int row, int col)
        {
            if (row < 0 || row >= Height || col < 0 || col >= Width)
                throw new ArgumentOutOfRangeException();
            return _fillValue;
        }

        public double? GetValueAt(double longitude, double latitude)
        {
            if (!Bounds.Contains(longitude, latitude))
                return null;
            return _fillValue;
        }
    }

    /// <summary>
    /// A mask that includes everything (pass-through).
    /// </summary>
    private sealed class PassThroughMask : IRasterMask
    {
        public IRasterFrame ApplyMask(IRasterFrame sourceFrame, double noDataValue) => sourceFrame;
        public bool Contains(double longitude, double latitude) => true;
    }

    #endregion
}
