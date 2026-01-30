using System;
using UnifyGeometry;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Contract;

/// <summary>
/// Unit tests for geometry contract per FR-004.
///
/// Tests verify that geometry implementations satisfy the substrate-agnostic,
/// topology-first requirements without committing to specific coordinate systems
/// or spatial substrates.
/// </summary>
public class GeometryContractTests
{
    #region Test Implementation - Mock Geometry

    /// <summary>
    /// Mock implementation of IGeometry for testing contract compliance.
    /// This represents a simple point geometry.
    /// </summary>
    private readonly struct PointGeometry : IGeometry
    {
        public int Dimension => 0;
        public bool IsEmpty { get; }
        GeometryType IGeometry.GeometryType => GeometryType.Point;

        public PointGeometry(bool isEmpty = false)
        {
            IsEmpty = isEmpty;
        }

        public double ComputeLength()
        {
            return 0.0; // Points have zero length
        }

        public IGeometry Clone()
        {
            return new PointGeometry(IsEmpty);
        }
    }

    /// <summary>
    /// Mock implementation of IGeometry for testing contract compliance.
    /// This represents a simple line segment geometry.
    /// </summary>
    private readonly struct LineGeometry : IGeometry
    {
        private readonly double _length;

        public int Dimension => 1;
        public bool IsEmpty { get; }
        GeometryType IGeometry.GeometryType => GeometryType.LineSegment;

        public LineGeometry(double length = 1.0, bool isEmpty = false)
        {
            _length = length;
            IsEmpty = isEmpty;
        }

        public double ComputeLength()
        {
            return IsEmpty ? 0.0 : _length;
        }

        public IGeometry Clone()
        {
            return new LineGeometry(_length, IsEmpty);
        }
    }

    #endregion

    #region Dimension Tests

    [Fact]
    public void Geometry_DimensionZero_PointGeometry()
    {
        // Arrange
        var geometry = new PointGeometry();

        // Assert
        Assert.Equal(0, geometry.Dimension);
    }

    [Fact]
    public void Geometry_DimensionOne_LineGeometry()
    {
        // Arrange
        var geometry = new LineGeometry();

        // Assert
        Assert.Equal(1, geometry.Dimension);
    }

    [Fact]
    public void Geometry_Dimension_CanRepresentDifferentDimensions()
    {
        // Arrange
        var pointGeometry = new PointGeometry();
        var lineGeometry = new LineGeometry();

        // Assert - Different geometry types can have different dimensions
        Assert.NotEqual(pointGeometry.Dimension, lineGeometry.Dimension);
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    public void Geometry_IsEmpty_NonEmptyPointReturnsFalse()
    {
        // Arrange
        var geometry = new PointGeometry(isEmpty: false);

        // Assert
        Assert.False(geometry.IsEmpty);
    }

    [Fact]
    public void Geometry_IsEmpty_EmptyPointReturnsTrue()
    {
        // Arrange
        var geometry = new PointGeometry(isEmpty: true);

        // Assert
        Assert.True(geometry.IsEmpty);
    }

    [Fact]
    public void Geometry_IsEmpty_NonEmptyLineReturnsFalse()
    {
        // Arrange
        var geometry = new LineGeometry(length: 10.0, isEmpty: false);

        // Assert
        Assert.False(geometry.IsEmpty);
    }

    [Fact]
    public void Geometry_IsEmpty_EmptyLineReturnsTrue()
    {
        // Arrange
        var geometry = new LineGeometry(length: 0.0, isEmpty: true);

        // Assert
        Assert.True(geometry.IsEmpty);
    }

    #endregion

    #region ComputeLength Tests

    [Fact]
    public void Geometry_ComputeLength_PointGeometryReturnsZero()
    {
        // Arrange
        var geometry = new PointGeometry(isEmpty: false);

        // Act
        var length = geometry.ComputeLength();

        // Assert
        Assert.Equal(0.0, length);
    }

    [Fact]
    public void Geometry_ComputeLength_LineGeometryReturnsPositiveLength()
    {
        // Arrange
        var expectedLength = 42.5;
        var geometry = new LineGeometry(length: expectedLength, isEmpty: false);

        // Act
        var actualLength = geometry.ComputeLength();

        // Assert
        Assert.Equal(expectedLength, actualLength);
    }

    [Fact]
    public void Geometry_ComputeLength_EmptyGeometryReturnsZero()
    {
        // Arrange
        var pointGeometry = new PointGeometry(isEmpty: true);
        var lineGeometry = new LineGeometry(length: 10.0, isEmpty: true);

        // Act
        var pointLength = pointGeometry.ComputeLength();
        var lineLength = lineGeometry.ComputeLength();

        // Assert - Empty geometries should have zero length regardless of dimension
        Assert.Equal(0.0, pointLength);
        Assert.Equal(0.0, lineLength);
    }

    [Fact]
    public void Geometry_ComputeLength_SubstrateAgnostic()
    {
        // Arrange
        // Different implementations can use different metrics
        // This test verifies the contract doesn't enforce a specific metric
        var geometry1 = new LineGeometry(length: 100.0);
        var geometry2 = new LineGeometry(length: 50.0);

        // Act
        var length1 = geometry1.ComputeLength();
        var length2 = geometry2.ComputeLength();

        // Assert - Length is implementation-defined but should be consistent
        Assert.Equal(100.0, length1);
        Assert.Equal(50.0, length2);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Geometry_Clone_PointGeometryCreatesIndependentCopy()
    {
        // Arrange
        var original = new PointGeometry(isEmpty: false);

        // Act
        var clone = (PointGeometry)original.Clone();

        // Assert
        Assert.Equal(original.Dimension, clone.Dimension);
        Assert.Equal(original.IsEmpty, clone.IsEmpty);
        Assert.Equal(original.ComputeLength(), clone.ComputeLength());
    }

    [Fact]
    public void Geometry_Clone_LineGeometryCreatesIndependentCopy()
    {
        // Arrange
        var original = new LineGeometry(length: 15.5, isEmpty: false);

        // Act
        var clone = (LineGeometry)original.Clone();

        // Assert
        Assert.Equal(original.Dimension, clone.Dimension);
        Assert.Equal(original.IsEmpty, clone.IsEmpty);
        Assert.Equal(original.ComputeLength(), clone.ComputeLength());
    }

    [Fact]
    public void Geometry_Clone_EmptyGeometryPreservesEmptyState()
    {
        // Arrange
        var original = new LineGeometry(length: 10.0, isEmpty: true);

        // Act
        var clone = (LineGeometry)original.Clone();

        // Assert
        Assert.Equal(original.IsEmpty, clone.IsEmpty);
        Assert.Equal(0.0, clone.ComputeLength());
    }

    [Fact]
    public void Geometry_Clone_ReturnsNewInstance()
    {
        // Arrange
        var original = new PointGeometry();

        // Act
        var clone = original.Clone();

        // Assert - Clone should be a distinct copy (not reference equality for reference types)
        // For value types like PointGeometry, the copy is by definition independent
        Assert.Equal(original.Dimension, clone.Dimension);
        Assert.Equal(original.IsEmpty, clone.IsEmpty);
        Assert.Equal(original.ComputeLength(), clone.ComputeLength());
    }

    #endregion

    #region GeometryType Tests

    [Fact]
    public void GeometryType_Point2D_ReturnsPoint()
    {
        // Arrange
        var point = new Point2(1.0, 2.0);

        // Act
        var geometryType = ((IGeometry)point).GeometryType;

        // Assert
        Assert.Equal(GeometryType.Point, geometryType);
    }

    [Fact]
    public void GeometryType_LineSegment_ReturnsLineSegment()
    {
        // Arrange
        var segment = new Segment2(0.0, 0.0, 5.0, 0.0);

        // Act
        var geometryType = ((IGeometry)segment).GeometryType;

        // Assert
        Assert.Equal(GeometryType.LineSegment, geometryType);
    }

    [Fact]
    public void GeometryType_Polyline_ReturnsPolyline()
    {
        // Arrange
        var polyline = new Polyline2(new[] { new Point2(0.0, 0.0), new Point2(1.0, 1.0) });

        // Act
        var geometryType = ((IGeometry)polyline).GeometryType;

        // Assert
        Assert.Equal(GeometryType.Polyline, geometryType);
    }

    [Fact]
    public void GeometryType_CanDiscriminateLineSegmentFromPolyline()
    {
        // Arrange
        // Both Segment2 and Polyline2 have Dimension=1
        var lineSegment = new Segment2(0.0, 0.0, 1.0, 1.0);
        var polyline = new Polyline2(new[] { new Point2(0.0, 0.0), new Point2(1.0, 1.0) });

        // Act
        var lineSegmentType = ((IGeometry)lineSegment).GeometryType;
        var polylineType = ((IGeometry)polyline).GeometryType;

        // Assert - Both have same dimension but different types
        Assert.Equal(1, lineSegment.Dimension);
        Assert.Equal(1, polyline.Dimension);
        Assert.NotEqual(lineSegmentType, polylineType);
        Assert.Equal(GeometryType.LineSegment, lineSegmentType);
        Assert.Equal(GeometryType.Polyline, polylineType);
    }

    [Fact]
    public void GeometryType_PolymorphicDiscrimination()
    {
        // Arrange
        IGeometry point = new Point2(1.0, 2.0);
        IGeometry segment = new Segment2(0.0, 0.0, 5.0, 0.0);
        IGeometry polyline = new Polyline2(new[] { new Point2(0.0, 0.0), new Point2(1.0, 1.0) });

        // Act
        var pointType = point.GeometryType;
        var segmentType = segment.GeometryType;
        var polylineType = polyline.GeometryType;

        // Assert - Can discriminate types through IGeometry interface
        Assert.Equal(GeometryType.Point, pointType);
        Assert.Equal(GeometryType.LineSegment, segmentType);
        Assert.Equal(GeometryType.Polyline, polylineType);
        Assert.NotEqual(pointType, segmentType);
        Assert.NotEqual(segmentType, polylineType);
    }

    [Fact]
    public void GeometryType_SwitchStatement_UsesGeometryType()
    {
        // Arrange
        var geometries = new IGeometry[]
        {
            new Point2(0.0, 0.0),
            new Segment2(0.0, 0.0, 1.0, 1.0),
            new Polyline2(new[] { new Point2(0.0, 0.0), new Point2(1.0, 1.0), new Point2(2.0, 0.0) })
        };

        // Act & Assert - Can use GeometryType in switch statement
        int pointCount = 0, segmentCount = 0, polylineCount = 0;

        foreach (var geometry in geometries)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    pointCount++;
                    break;
                case GeometryType.LineSegment:
                    segmentCount++;
                    break;
                case GeometryType.Polyline:
                    polylineCount++;
                    break;
            }
        }

        Assert.Equal(1, pointCount);
        Assert.Equal(1, segmentCount);
        Assert.Equal(1, polylineCount);
    }

    #endregion

    #region Contract Compliance Tests

    [Fact]
    public void Geometry_Contract_PointGeometryComplies()
    {
        // Arrange
        var geometry = new PointGeometry();

        // Assert - Verify all contract members are accessible and functional
        Assert.InRange(geometry.Dimension, 0, int.MaxValue);
        Assert.InRange(geometry.ComputeLength(), double.MinValue, double.MaxValue);
        Assert.NotNull(geometry.Clone());
    }

    [Fact]
    public void Geometry_Contract_LineGeometryComplies()
    {
        // Arrange
        var geometry = new LineGeometry();

        // Assert - Verify all contract members are accessible and functional
        Assert.InRange(geometry.Dimension, 0, int.MaxValue);
        Assert.InRange(geometry.ComputeLength(), double.MinValue, double.MaxValue);
        Assert.NotNull(geometry.Clone());
    }

    [Fact]
    public void Geometry_Contract_SubstrateAgnostic_NoCoordinateSystem()
    {
        // Arrange
        var geometry = new LineGeometry(length: 10.0);

        // Assert
        // The IGeometry interface does not expose coordinate system details
        // This is by design: FR-004 requires substrate-agnostic representation
        Assert.DoesNotContain("Coordinate", geometry.GetType().Name, StringComparison.Ordinal);
        Assert.DoesNotContain("Spatial", geometry.GetType().Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Geometry_Contract_AbstractRepresentation_AllowsMultipleImplementations()
    {
        // Arrange
        // Multiple concrete implementations can satisfy the same abstract contract
        var pointGeometry = new PointGeometry();
        var lineGeometry = new LineGeometry();

        // Assert - Both implement IGeometry despite different internal representations
        Assert.IsAssignableFrom<IGeometry>(pointGeometry);
        Assert.IsAssignableFrom<IGeometry>(lineGeometry);
        Assert.NotEqual(pointGeometry.GetType(), lineGeometry.GetType());
    }

    #endregion

    #region Concrete Type Tests - Point2

    [Fact]
    public void Point2D_Constructor_SetsCoordinates()
    {
        // Arrange & Act
        var point = new Point2(3.5, 7.2);

        // Assert
        Assert.Equal(3.5, point.X);
        Assert.Equal(7.2, point.Y);
    }

    [Fact]
    public void Point2D_Empty_ReturnsNaNCoordinates()
    {
        // Arrange & Act
        var point = Point2.Empty;

        // Assert
        Assert.True(point.IsEmpty);
        Assert.True(double.IsNaN(point.X));
        Assert.True(double.IsNaN(point.Y));
    }

    [Fact]
    public void Point2D_Origin_ReturnsZeroCoordinates()
    {
        // Arrange & Act
        var point = Point2.Origin;

        // Assert
        Assert.Equal(0.0, point.X);
        Assert.Equal(0.0, point.Y);
        Assert.False(point.IsEmpty);
    }

    [Fact]
    public void Point2D_Dimension_ReturnsZero()
    {
        // Arrange
        var point = new Point2(1.0, 2.0);

        // Act & Assert
        Assert.Equal(0, point.Dimension);
    }

    [Fact]
    public void Point2D_ComputeLength_ReturnsZero()
    {
        // Arrange
        var point = new Point2(5.0, 10.0);

        // Act
        var length = point.ComputeLength();

        // Assert
        Assert.Equal(0.0, length);
    }

    [Fact]
    public void Point2D_DistanceTo_ComputesCorrectDistance()
    {
        // Arrange
        var point1 = new Point2(0.0, 0.0);
        var point2 = new Point2(3.0, 4.0);

        // Act
        var distance = point1.DistanceTo(point2);

        // Assert - 3-4-5 triangle
        Assert.Equal(5.0, distance);
    }

    [Fact]
    public void Point2D_DistanceTo_EmptyPoint_ReturnsNaN()
    {
        // Arrange
        var point1 = new Point2(1.0, 2.0);
        var point2 = Point2.Empty;

        // Act
        var distance = point1.DistanceTo(point2);

        // Assert
        Assert.True(double.IsNaN(distance));
    }

    [Fact]
    public void Point2D_Clone_ReturnsThisInstance()
    {
        // Arrange
        var point = new Point2(1.5, 2.5);

        // Act
        var clone = ((IGeometry)point).Clone();

        // Assert - Since it's a value type, Clone returns this
        Assert.Equal(point.X, ((Point2)clone).X);
        Assert.Equal(point.Y, ((Point2)clone).Y);
    }

    [Fact]
    public void Point2D_ToString_FormatsCorrectly()
    {
        // Arrange
        var point = new Point2(1.5, 2.5);
        var emptyPoint = Point2.Empty;

        // Act
        var formatted = point.ToString();
        var emptyFormatted = emptyPoint.ToString();

        // Assert
        Assert.Equal("Point2(1.5, 2.5)", formatted);
        Assert.Equal("Point2(Empty)", emptyFormatted);
    }

    [Fact]
    public void Point2D_Equality_ValueSemantics()
    {
        // Arrange
        var point1 = new Point2(1.0, 2.0);
        var point2 = new Point2(1.0, 2.0);
        var point3 = new Point2(1.0, 3.0);

        // Assert
        Assert.Equal(point1, point2);
        Assert.NotEqual(point1, point3);
    }

    #endregion

    #region Concrete Type Tests - Segment2

    [Fact]
    public void LineSegment_Constructor_SetsEndpoints()
    {
        // Arrange
        var start = new Point2(0.0, 0.0);
        var end = new Point2(5.0, 0.0);

        // Act
        var segment = new Segment2(start, end);

        // Assert
        Assert.Equal(start, segment.Start);
        Assert.Equal(end, segment.End);
    }

    [Fact]
    public void LineSegment_Constructor_Coordinates_SetsEndpoints()
    {
        // Arrange & Act
        var segment = new Segment2(0.0, 0.0, 5.0, 0.0);

        // Assert
        Assert.Equal(0.0, segment.Start.X);
        Assert.Equal(0.0, segment.Start.Y);
        Assert.Equal(5.0, segment.End.X);
        Assert.Equal(0.0, segment.End.Y);
    }

    [Fact]
    public void LineSegment_Empty_ReturnsEmptyEndpoints()
    {
        // Arrange & Act
        var segment = Segment2.Empty;

        // Assert
        Assert.True(segment.IsEmpty);
        Assert.True(segment.Start.IsEmpty);
        Assert.True(segment.End.IsEmpty);
    }

    [Fact]
    public void LineSegment_Dimension_ReturnsOne()
    {
        // Arrange
        var segment = new Segment2(0.0, 0.0, 5.0, 0.0);

        // Act & Assert
        Assert.Equal(1, segment.Dimension);
    }

    [Fact]
    public void LineSegment_ComputeLength_ReturnsCorrectLength()
    {
        // Arrange
        var segment = new Segment2(0.0, 0.0, 3.0, 4.0);

        // Act
        var length = segment.ComputeLength();

        // Assert - 3-4-5 triangle
        Assert.Equal(5.0, length);
    }

    [Fact]
    public void LineSegment_ComputeLength_EmptySegment_ReturnsNaN()
    {
        // Arrange
        var segment = Segment2.Empty;

        // Act
        var length = segment.ComputeLength();

        // Assert
        Assert.True(double.IsNaN(length));
    }

    [Fact]
    public void LineSegment_Clone_ReturnsThisInstance()
    {
        // Arrange
        var segment = new Segment2(0.0, 0.0, 5.0, 5.0);

        // Act
        var clone = ((IGeometry)segment).Clone();

        // Assert
        Assert.Equal(segment.Start, ((Segment2)clone).Start);
        Assert.Equal(segment.End, ((Segment2)clone).End);
    }

    [Fact]
    public void LineSegment_ToString_FormatsCorrectly()
    {
        // Arrange
        var segment = new Segment2(1.0, 2.0, 3.0, 4.0);
        var emptySegment = Segment2.Empty;

        // Act
        var formatted = segment.ToString();
        var emptyFormatted = emptySegment.ToString();

        // Assert
        Assert.Contains("Segment2", formatted, StringComparison.Ordinal);
        Assert.Contains("Point2(1, 2)", formatted, StringComparison.Ordinal);
        Assert.Contains("Point2(3, 4)", formatted, StringComparison.Ordinal);
        Assert.Equal("Segment2(Empty)", emptyFormatted);
    }

    [Fact]
    public void LineSegment_Equality_ValueSemantics()
    {
        // Arrange
        var segment1 = new Segment2(0.0, 0.0, 5.0, 0.0);
        var segment2 = new Segment2(0.0, 0.0, 5.0, 0.0);
        var segment3 = new Segment2(0.0, 0.0, 5.0, 1.0);

        // Assert
        Assert.Equal(segment1, segment2);
        Assert.NotEqual(segment1, segment3);
    }

    #endregion

    #region Concrete Type Tests - Polyline2

    [Fact]
    public void Polyline_Constructor_CreatesEmptyPolyline()
    {
        // Arrange & Act
        var polyline = new Polyline2();

        // Assert
        Assert.True(polyline.IsEmpty);
        Assert.Equal(0, polyline.PointCount);
        Assert.Equal(0, polyline.Points.Length);
    }

    [Fact]
    public void Polyline_Constructor_Points_CreatesPolyline()
    {
        // Arrange
        var points = new[]
        {
            new Point2(0.0, 0.0),
            new Point2(1.0, 1.0),
            new Point2(2.0, 0.0)
        };

        // Act
        var polyline = new Polyline2(points);

        // Assert
        Assert.False(polyline.IsEmpty);
        Assert.Equal(3, polyline.PointCount);
    }

    [Fact]
    public void Polyline_Empty_ReturnsEmptyPolyline()
    {
        // Arrange & Act
        var polyline = Polyline2.Empty;

        // Assert
        Assert.True(polyline.IsEmpty);
        Assert.Equal(0, polyline.PointCount);
    }

    [Fact]
    public void Polyline_FromCoordinates_CreatesPolyline()
    {
        // Arrange & Act
        var polyline = Polyline2.FromCoordinates(0.0, 0.0, 1.0, 1.0, 2.0, 0.0);

        // Assert
        Assert.False(polyline.IsEmpty);
        Assert.Equal(3, polyline.PointCount);
        Assert.Equal(0.0, polyline.Points[0].X);
        Assert.Equal(0.0, polyline.Points[0].Y);
        Assert.Equal(1.0, polyline.Points[1].X);
        Assert.Equal(1.0, polyline.Points[1].Y);
        Assert.Equal(2.0, polyline.Points[2].X);
        Assert.Equal(0.0, polyline.Points[2].Y);
    }

    [Fact]
    public void Polyline_FromCoordinates_OddCount_ThrowsException()
    {
        // Arrange
        var coordinates = new[] { 1.0, 2.0, 3.0 }; // Odd number

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Polyline2.FromCoordinates(coordinates));
    }

    [Fact]
    public void Polyline_AddPoint_AddsPoint()
    {
        // Arrange
        var polyline = new Polyline2();
        var point = new Point2(5.0, 5.0);

        // Act
        polyline.AddPoint(point);

        // Assert
        Assert.Equal(1, polyline.PointCount);
        Assert.Equal(point, polyline.Points[0]);
    }

    [Fact]
    public void Polyline_Dimension_ReturnsOne()
    {
        // Arrange
        var polyline = new Polyline2(new[] { new Point2(0.0, 0.0) });

        // Act & Assert
        Assert.Equal(1, polyline.Dimension);
    }

    [Fact]
    public void Polyline_ComputeLength_Empty_ReturnsZero()
    {
        // Arrange
        var polyline = new Polyline2();

        // Act
        var length = polyline.ComputeLength();

        // Assert
        Assert.Equal(0.0, length);
    }

    [Fact]
    public void Polyline_ComputeLength_SinglePoint_ReturnsZero()
    {
        // Arrange
        var polyline = new Polyline2(new[] { new Point2(1.0, 1.0) });

        // Act
        var length = polyline.ComputeLength();

        // Assert
        Assert.Equal(0.0, length);
    }

    [Fact]
    public void Polyline_ComputeLength_MultipleSegments_ReturnsCorrectLength()
    {
        // Arrange
        var polyline = Polyline2.FromCoordinates(0.0, 0.0, 3.0, 4.0, 6.0, 4.0);
        // Segments: (0,0) to (3,4) = 5, (3,4) to (6,4) = 3, total = 8

        // Act
        var length = polyline.ComputeLength();

        // Assert
        Assert.Equal(8.0, length, precision: 6);
    }

    [Fact]
    public void Polyline_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new Polyline2(new[] { new Point2(1.0, 1.0) });

        // Act
        var clone = (Polyline2)original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.PointCount, clone.PointCount);
        Assert.Equal(original.Points[0], clone.Points[0]);
    }

    [Fact]
    public void Polyline_Clone_IndependentModification()
    {
        // Arrange
        var original = new Polyline2(new[] { new Point2(1.0, 1.0) });
        var clone = (Polyline2)original.Clone();

        // Act - Modify the clone
        clone.AddPoint(new Point2(2.0, 2.0));

        // Assert - Original should be unchanged
        Assert.Equal(1, original.PointCount);
        Assert.Equal(2, clone.PointCount);
    }

    [Fact]
    public void Polyline_ToString_FormatsCorrectly()
    {
        // Arrange
        var polyline = Polyline2.FromCoordinates(1.0, 2.0, 3.0, 4.0);
        var emptyPolyline = new Polyline2();

        // Act
        var formatted = polyline.ToString();
        var emptyFormatted = emptyPolyline.ToString();

        // Assert
        Assert.StartsWith("Polyline2([", formatted, StringComparison.Ordinal);
        Assert.Contains("Point2(1, 2)", formatted, StringComparison.Ordinal);
        Assert.Contains("Point2(3, 4)", formatted, StringComparison.Ordinal);
        Assert.Equal("Polyline2(Empty)", emptyFormatted);
    }

    [Fact]
    public void Polyline_Points_ReturnsReadOnlyCopy()
    {
        // Arrange
        var polyline = Polyline2.FromCoordinates(0.0, 0.0, 1.0, 1.0);
        var points = polyline.PointsList;

        // Act & Assert - Points should be a copy/array, not the internal list
        Assert.IsType<Point2[]>(points);
        Assert.Equal(2, points.Count);
    }

    #endregion
}
