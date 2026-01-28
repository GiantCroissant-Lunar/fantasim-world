using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;

/// <summary>
/// Builds a boundary combinatorial map from topology state.
///
/// RFC-V2-0041 §11.1: Build cmap from boundary network.
///
/// Algorithm:
/// 1. Create two darts per boundary (Forward/Backward)
/// 2. Link twin darts
/// 3. Set origin junctions
/// 4. Compute cyclic ordering at each junction (by angle)
/// 5. Set Next pointers based on cyclic ordering
/// </summary>
public sealed class BoundaryCMapBuilder : IBoundaryCMapBuilder
{
    /// <inheritdoc />
    public IBoundaryCMap Build(IPlateTopologyStateView topology)
    {
        var cmap = new InMemoryBoundaryCMap();
        var dartDirections = new Dictionary<BoundaryDart, Point3>();
        var dartDestinations = new Dictionary<BoundaryDart, JunctionId>();

        // 1. Add all junctions
        foreach (var junction in topology.Junctions.Values.Where(j => !j.IsRetired))
        {
            cmap.AddJunction(junction.JunctionId);
        }

        // 2. Create darts for each boundary
        foreach (var boundary in topology.Boundaries.Values.Where(b => !b.IsRetired))
        {
            var endpoints = GetBoundaryEndpoints(boundary, topology);
            if (endpoints == null)
            {
                throw new CMapBuildException(
                    $"Boundary {boundary.BoundaryId} is not connected to exactly two junctions");
            }

            var (startJunction, endJunction, startPoint, endPoint) = endpoints.Value;

            // Forward dart: start → end
            var forwardDart = new BoundaryDart
            {
                BoundaryId = boundary.BoundaryId,
                SegmentIndex = 0,
                Direction = DartDirection.Forward
            };

            // Backward dart: end → start
            var backwardDart = new BoundaryDart
            {
                BoundaryId = boundary.BoundaryId,
                SegmentIndex = 0,
                Direction = DartDirection.Backward
            };

            // Add darts with their origins
            cmap.AddDart(forwardDart, startJunction);
            cmap.AddDart(backwardDart, endJunction);

            // Link twins
            cmap.SetTwin(forwardDart, backwardDart);

            // Store directions for angle sorting (outgoing direction from origin)
            var forwardDir = ComputeDirection(startPoint, endPoint);
            var backwardDir = ComputeDirection(endPoint, startPoint);
            dartDirections[forwardDart] = forwardDir;
            dartDirections[backwardDart] = backwardDir;

            // Store destinations for Next pointer computation
            dartDestinations[forwardDart] = endJunction;
            dartDestinations[backwardDart] = startJunction;
        }

        // 3. Sort incidents at each junction by angle (CCW from +X)
        cmap.SortIncidentsByAngle(d => dartDirections[d]);

        // 4. Set Next pointers
        // For each dart d, Next(d) is the twin of the "next" dart in cyclic order at d's destination
        foreach (var dart in cmap.Darts)
        {
            var destination = dartDestinations[dart];
            var incidentsAtDest = cmap.IncidentOrdered(destination);

            if (incidentsAtDest.Count == 0)
            {
                throw new CMapBuildException(
                    $"Junction {destination} has no incident darts");
            }

            // Find the twin of dart in the incident list at destination
            var twinDart = cmap.Twin(dart);

            // Find index of twin in the cyclic order at destination
            var twinIndex = -1;
            for (int i = 0; i < incidentsAtDest.Count; i++)
            {
                if (incidentsAtDest[i] == twinDart)
                {
                    twinIndex = i;
                    break;
                }
            }

            if (twinIndex < 0)
            {
                throw new CMapBuildException(
                    $"Twin of dart {dart} not found in incidents at junction {destination}");
            }

            // Next in face = the dart that comes AFTER twin in CCW order (wrapped)
            // This gives us the "turn left" behavior for face walking
            var nextIndex = (twinIndex + 1) % incidentsAtDest.Count;
            var nextDart = incidentsAtDest[nextIndex];

            cmap.SetNext(dart, nextDart);
        }

        return cmap;
    }

    /// <summary>
    /// Gets the start and end junctions for a boundary.
    /// Returns null if the boundary is not properly connected to exactly two junctions.
    /// </summary>
    private static (JunctionId start, JunctionId end, Point3 startPt, Point3 endPt)?
        GetBoundaryEndpoints(Boundary boundary, IPlateTopologyStateView topology)
    {
        // Find junctions that contain this boundary
        var connectedJunctions = topology.Junctions.Values
            .Where(j => !j.IsRetired && j.BoundaryIds.Contains(boundary.BoundaryId))
            .ToList();

        if (connectedJunctions.Count != 2)
            return null;

        // Get geometry endpoints
        var (startPt, endPt) = GetPolylineEndpoints(boundary.Geometry);

        // Match junctions to endpoints by proximity
        var j0 = connectedJunctions[0];
        var j1 = connectedJunctions[1];

        var j0Pt = new Point3(j0.Location.X, j0.Location.Y, 0); // Junction is 2D, promote to 3D
        var j1Pt = new Point3(j1.Location.X, j1.Location.Y, 0);

        var d0ToStart = DistanceSquared(j0Pt, startPt);
        var d1ToStart = DistanceSquared(j1Pt, startPt);

        if (d0ToStart < d1ToStart)
        {
            // j0 is at start, j1 is at end
            return (j0.JunctionId, j1.JunctionId, startPt, endPt);
        }
        else
        {
            // j1 is at start, j0 is at end
            return (j1.JunctionId, j0.JunctionId, startPt, endPt);
        }
    }

    private static (Point3 start, Point3 end) GetPolylineEndpoints(IGeometry geometry)
    {
        if (geometry is Polyline3 polyline && !polyline.IsEmpty)
        {
            return (polyline.Points[0], polyline.Points[^1]);
        }

        throw new CMapBuildException($"Boundary geometry is not a non-empty Polyline3");
    }

    private static Point3 ComputeDirection(Point3 from, Point3 to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var dz = to.Z - from.Z;
        var len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len < 1e-12) return new Point3(1, 0, 0); // fallback
        return new Point3(dx / len, dy / len, dz / len);
    }

    private static double DistanceSquared(Point3 a, Point3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
