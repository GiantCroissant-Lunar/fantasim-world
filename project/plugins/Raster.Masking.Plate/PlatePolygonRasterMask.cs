using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Raster.Core;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Raster.Masking.Plate;

/// <summary>
/// A mask based on plate polygons.
/// Pixels inside/outside plate boundaries are masked.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class PlatePolygonRasterMask : IRasterMask
{
    private readonly PlatePolygonSet _polygonSet;
    private readonly IReadOnlySet<PlateId>? _specificPlates;

    /// <summary>
    /// Creates a new PlatePolygonRasterMask.
    /// </summary>
    /// <param name="polygonSet">The polygon set containing plate boundaries.</param>
    /// <param name="specificPlates">Optional: limit masking to specific plates. If null, all plates are included.</param>
    public PlatePolygonRasterMask(
        PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null)
    {
        if (polygonSet.Polygons.IsDefaultOrEmpty)
            throw new ArgumentException("Polygon set cannot be empty", nameof(polygonSet));
        _polygonSet = polygonSet;
        _specificPlates = specificPlates?.ToHashSet();
    }

    /// <inheritdoc />
    public IRasterFrame ApplyMask(IRasterFrame sourceFrame, double noDataValue)
    {
        if (sourceFrame == null)
            throw new ArgumentNullException(nameof(sourceFrame));

        var rawData = sourceFrame.GetRawData();
        var sourceData = new double[rawData.Length / sizeof(double)];
        Buffer.BlockCopy(rawData.ToArray(), 0, sourceData, 0, rawData.Length);

        var maskedData = new double[sourceData.Length];

        for (int row = 0; row < sourceFrame.Height; row++)
        {
            for (int col = 0; col < sourceFrame.Width; col++)
            {
                var index = row * sourceFrame.Width + col;
                var value = sourceData[index];

                // Calculate geographic coordinates for this cell
                var lon = sourceFrame.Bounds.MinLongitude +
                    (col / (double)sourceFrame.Width) * sourceFrame.Bounds.Width;
                var lat = sourceFrame.Bounds.MaxLatitude -
                    (row / (double)sourceFrame.Height) * sourceFrame.Bounds.Height;

                // Check if point is in any plate polygon
                var inPlate = Contains(lon, lat);
                maskedData[index] = inPlate ? value : noDataValue;
            }
        }

        return new ArrayRasterFrame(
            sourceFrame.Tick,
            sourceFrame.Width,
            sourceFrame.Height,
            sourceFrame.Bounds,
            sourceFrame.DataType,
            maskedData,
            noDataValue);
    }

    /// <inheritdoc />
    public bool Contains(double longitude, double latitude)
    {
        if (_specificPlates != null)
        {
            foreach (var plateId in _specificPlates)
            {
                var polygon = _polygonSet.GetPolygon(plateId);
                if (polygon.HasValue && IsPointInPolygon(polygon.Value, longitude, latitude))
                {
                    return true;
                }
            }
            return false;
        }

        // Check all plates
        foreach (var polygon in _polygonSet.Polygons)
        {
            if (IsPointInPolygon(polygon, longitude, latitude))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a point is inside a polygon (including holes) using the ray casting algorithm.
    /// </summary>
    private static bool IsPointInPolygon(PlatePolygon polygon, double lon, double lat)
    {
        // Check outer ring first
        if (!IsPointInRing(polygon.OuterRing, lon, lat))
            return false;

        // Check holes - if point is in any hole, it's outside the polygon
        foreach (var hole in polygon.Holes)
        {
            if (IsPointInRing(hole, lon, lat))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Ray casting algorithm for point-in-polygon testing.
    /// </summary>
    private static bool IsPointInRing(Polyline3 ring, double lon, double lat)
    {
        var points = ring.Points;
        if (points.Length < 3)
            return false;

        bool inside = false;
        int j = points.Length - 1;

        for (int i = 0; i < points.Length; i++)
        {
            var vi = points[i];
            var vj = points[j];

            // Check if point is on vertex
            if (Math.Abs(vi.X - lon) < 1e-10 && Math.Abs(vi.Y - lat) < 1e-10)
                return true;

            // Check if edge straddles the horizontal line at y=lat
            bool intersect = ((vi.Y > lat) != (vj.Y > lat)) &&
                (lon < (vj.X - vi.X) * (lat - vi.Y) / (vj.Y - vi.Y + 1e-10) + vi.X);

            if (intersect)
                inside = !inside;

            j = i;
        }

        return inside;
    }
}
