using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver;

/// <summary>
/// Extracts plate polygons from boundary network using combinatorial map face-walk.
/// RFC-V2-0041 implementation.
/// </summary>
/// <remarks>
/// <para>
/// Face→Plate attribution rule: The face lies on the LEFT side of each dart in the face loop.
/// For a dart on boundary B with direction Forward:
///   - Left side = B.PlateIdLeft
/// For a dart on boundary B with direction Backward:
///   - Left side = B.PlateIdRight (because we're walking the edge in reverse)
/// </para>
/// <para>
/// <b>Spherical-correct:</b> Uses <see cref="SphericalGeometry.ComputeSignedSphericalArea"/>
/// to determine outer vs inner rings and identify the "outside" face. This is correct
/// anywhere on the sphere, unlike planar signed area which breaks at poles and antimeridian.
/// </para>
/// <para>
/// Outside face policy: The face with largest absolute spherical area is treated as "exterior"
/// and excluded from the polygon set (or assigned PlateId.Empty).
/// </para>
/// </remarks>
public sealed class PlatePolygonizer : IPlatePolygonizer
{
    private readonly IBoundaryCMapBuilder _cmapBuilder;

    public PlatePolygonizer(IBoundaryCMapBuilder cmapBuilder)
    {
        _cmapBuilder = cmapBuilder ?? throw new ArgumentNullException(nameof(cmapBuilder));
    }

    /// <inheritdoc />
    public PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        options ??= new PolygonizationOptions();

        // Build combinatorial map
        var cmap = _cmapBuilder.Build(topology);

        // Enumerate all faces
        var faces = cmap.EnumerateFaces().ToList();

        if (faces.Count == 0)
        {
            return new PlatePolygonSet(tick, ImmutableArray<PlatePolygon>.Empty);
        }

        // Convert faces to attributed polygons
        var attributedFaces = new List<(PlateId plateId, Polyline3 ring, double signedArea)>();
        var errors = new List<string>();

        foreach (var faceDarts in faces)
        {
            var (plateId, ring, signedArea, error) = ProcessFace(faceDarts, cmap, topology);
            if (error != null)
            {
                errors.Add(error);
                continue;
            }
            attributedFaces.Add((plateId, ring, signedArea));
        }

        if (errors.Count > 0 && !options.Value.AllowPartialPolygonization)
        {
            throw new PolygonizationException(
                $"Face attribution failed: {string.Join("; ", errors)}",
                new PolygonizationDiagnostics(
                    false,
                    ImmutableArray<OpenBoundaryDiagnostic>.Empty,
                    ImmutableArray<NonManifoldJunctionDiagnostic>.Empty,
                    ImmutableArray<DisconnectedComponentDiagnostic>.Empty));
        }

        // Identify outside face (largest absolute area)
        var outsideFaceIndex = FindOutsideFaceIndex(attributedFaces);

        // Group by PlateId (excluding outside face), keeping signed area for outer/hole determination
        var ringsByPlate = new Dictionary<PlateId, List<(Polyline3 ring, double signedArea)>>();
        for (int i = 0; i < attributedFaces.Count; i++)
        {
            if (i == outsideFaceIndex) continue;

            var (plateId, ring, signedArea) = attributedFaces[i];
            if (plateId.IsEmpty) continue; // Skip unattributed faces

            if (!ringsByPlate.TryGetValue(plateId, out var rings))
            {
                rings = new List<(Polyline3, double)>();
                ringsByPlate[plateId] = rings;
            }
            rings.Add((ring, signedArea));
        }

        // Build PlatePolygons with proper outer/hole determination
        var polygons = new List<PlatePolygon>();
        foreach (var (plateId, rings) in ringsByPlate)
        {
            if (rings.Count == 0) continue;

            // Separate outers (positive signed area) from holes (negative signed area)
            // On a unit sphere with CCW winding: positive = outer, negative = hole
            var outers = rings.Where(r => r.signedArea > 0).Select(r => CanonicalizeRing(r.ring)).ToList();
            var holes = rings.Where(r => r.signedArea <= 0).Select(r => CanonicalizeRing(r.ring)).ToList();

            if (outers.Count == 0)
            {
                // Edge case: all rings are holes (impossible on a closed surface for a valid plate)
                // Take the largest absolute area as outer
                var largest = rings.OrderByDescending(r => Math.Abs(r.signedArea)).First();
                outers.Add(CanonicalizeRing(largest.ring));
                holes = rings.Where(r => r != largest).Select(r => CanonicalizeRing(r.ring)).ToList();
            }

            if (outers.Count == 1)
            {
                // Simple case: one outer, all holes belong to it
                polygons.Add(new PlatePolygon(plateId, outers[0], holes.ToImmutableArray()));
            }
            else
            {
                // Complex case: multiple outers (archipelago) - assign holes via centroid containment
                // For now, create separate polygons for each outer; proper hole assignment
                // would require point-in-polygon testing using spherical containment
                foreach (var outer in outers)
                {
                    // Find holes whose centroids are contained in this outer
                    // For MVP, we skip complex assignment and create outer-only polygons
                    // TODO: Implement spherical point-in-polygon for proper hole assignment
                    polygons.Add(new PlatePolygon(plateId, outer, ImmutableArray<Polyline3>.Empty));
                }
                // Orphan holes go to the largest outer as a fallback
                if (holes.Count > 0)
                {
                    var largestOuter = outers.OrderByDescending(o => Math.Abs(SphericalGeometry.ComputeSignedSphericalArea(o))).First();
                    var largestIndex = polygons.FindIndex(p => p.PlateId == plateId && p.OuterRing == largestOuter);
                    if (largestIndex >= 0)
                    {
                        var existing = polygons[largestIndex];
                        polygons[largestIndex] = new PlatePolygon(plateId, existing.OuterRing, holes.ToImmutableArray());
                    }
                }
            }
        }

        // Sort by PlateId for determinism
        polygons.Sort((a, b) => a.PlateId.Value.CompareTo(b.PlateId.Value));

        return new PlatePolygonSet(tick, polygons.ToImmutableArray());
    }

    /// <inheritdoc />
    public BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        // Build adjacencies from boundary truth (not cmap - direct from source)
        var adjacencies = new List<BoundaryFaceAdjacency>();

        foreach (var boundary in topology.Boundaries.Values.Where(b => !b.IsRetired))
        {
            // Each boundary segment maps directly to its left/right plates
            adjacencies.Add(new BoundaryFaceAdjacency(
                boundary.BoundaryId,
                SegmentIndex: 0, // Single segment for now
                boundary.PlateIdLeft,
                boundary.PlateIdRight));
        }

        // Sort for determinism
        adjacencies.Sort((a, b) =>
        {
            var cmp = a.BoundaryId.Value.CompareTo(b.BoundaryId.Value);
            return cmp != 0 ? cmp : a.SegmentIndex.CompareTo(b.SegmentIndex);
        });

        return new BoundaryFaceAdjacencyMap(tick, adjacencies.ToImmutableArray());
    }

    /// <inheritdoc />
    public PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var openBoundaries = new List<OpenBoundaryDiagnostic>();
        var nonManifoldJunctions = new List<NonManifoldJunctionDiagnostic>();

        try
        {
            // Check for dangling boundaries (not connected to 2 junctions)
            foreach (var boundary in topology.Boundaries.Values.Where(b => !b.IsRetired))
            {
                var connectedJunctions = topology.Junctions.Values
                    .Where(j => !j.IsRetired && j.BoundaryIds.Contains(boundary.BoundaryId))
                    .ToList();

                if (connectedJunctions.Count < 2)
                {
                    // Find the open endpoint
                    var endpoint = GetBoundaryEndpoint(boundary, connectedJunctions);
                    openBoundaries.Add(new OpenBoundaryDiagnostic(
                        boundary.BoundaryId,
                        endpoint,
                        $"Boundary connected to {connectedJunctions.Count} junctions (expected 2)"));
                }
            }

            // Check for non-manifold junctions (odd incident count is suspicious)
            foreach (var junction in topology.Junctions.Values.Where(j => !j.IsRetired))
            {
                var incidentCount = junction.BoundaryIds
                    .Count(bid => topology.Boundaries.TryGetValue(bid, out var b) && !b.IsRetired);

                // A valid manifold junction should have even incident count (each boundary enters and exits)
                // Single incident (degree 1) is always invalid for closed surfaces
                if (incidentCount == 1)
                {
                    var pos = junction.Location.ToPositionVector();
                    nonManifoldJunctions.Add(new NonManifoldJunctionDiagnostic(
                        junction.JunctionId,
                        new Point3(pos.X, pos.Y, pos.Z),
                        incidentCount,
                        "Junction has only one incident boundary (dangling)"));
                }
            }

            var isValid = openBoundaries.Count == 0 && nonManifoldJunctions.Count == 0;

            return new PolygonizationDiagnostics(
                isValid,
                openBoundaries.ToImmutableArray(),
                nonManifoldJunctions.ToImmutableArray(),
                ImmutableArray<DisconnectedComponentDiagnostic>.Empty);
        }
        catch (CMapBuildException ex)
        {
            // CMap build failure indicates fundamental topology issues
            return new PolygonizationDiagnostics(
                false,
                openBoundaries.ToImmutableArray(),
                nonManifoldJunctions.ToImmutableArray(),
                ImmutableArray.Create(new DisconnectedComponentDiagnostic(
                    0,
                    ImmutableArray<BoundaryId>.Empty,
                    $"CMap build failed: {ex.Message}")));
        }
    }

    private static Point3 GetBoundaryEndpoint(Boundary boundary, List<Junction> connectedJunctions)
    {
        if (boundary.Geometry is Polyline3 polyline && !polyline.IsEmpty)
        {
            // If we have one connected junction, return the OTHER endpoint
            if (connectedJunctions.Count == 1)
            {
                var jPos = connectedJunctions[0].Location.ToPositionVector();
                var junctionPt = new Point3(jPos.X, jPos.Y, jPos.Z);
                var startPt = polyline.Points[0];
                var endPt = polyline.Points[^1];

                // Return the endpoint farther from the connected junction
                var d0 = DistanceSquared(junctionPt, startPt);
                var d1 = DistanceSquared(junctionPt, endPt);
                return d0 > d1 ? startPt : endPt;
            }

            // No connected junctions - return start
            return polyline.Points[0];
        }

        return new Point3(0, 0, 0);
    }

    private static double DistanceSquared(Point3 a, Point3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Process a face: extract ring geometry and attribute to plate.
    /// </summary>
    private static (PlateId plateId, Polyline3 ring, double signedArea, string? error)
        ProcessFace(
            IReadOnlyList<BoundaryDart> faceDarts,
            IBoundaryCMap cmap,
            IPlateTopologyStateView topology)
    {
        if (faceDarts.Count == 0)
        {
            return (default, Polyline3.Empty, 0, "Empty face");
        }

        // Collect junction positions along the face
        var points = new List<Point3>();
        PlateId? attributedPlate = null;
        string? attributionError = null;

        foreach (var dart in faceDarts)
        {
            var junctionId = cmap.Origin(dart);
            if (!topology.Junctions.TryGetValue(junctionId, out var junction))
            {
                attributionError = $"Junction {junctionId} not found";
                continue;
            }

            // Add junction position (convert from SurfacePoint to Point3)
            var junctionPos = junction.Location.ToPositionVector();
            points.Add(new Point3(junctionPos.X, junctionPos.Y, junctionPos.Z));

            // Attribute: face is on LEFT side of each dart
            var leftPlate = GetLeftPlate(dart, topology);

            if (attributedPlate == null)
            {
                attributedPlate = leftPlate;
            }
            else if (leftPlate != attributedPlate)
            {
                // Inconsistent attribution within same face - topology error
                attributionError = $"Inconsistent plate attribution: dart {dart} has left plate {leftPlate}, but face already attributed to {attributedPlate}";
            }
        }

        // Close the ring (first point = last point)
        if (points.Count > 0)
        {
            points.Add(points[0]);
        }

        var ring = new Polyline3(points);
        // Use spherical area computation for correct results on the sphere
        var signedArea = SphericalGeometry.ComputeSignedSphericalArea(ring);

        return (attributedPlate ?? default, ring, signedArea, attributionError);
    }

    /// <summary>
    /// Gets the plate on the LEFT side of a dart.
    /// For Forward dart: left side = boundary.PlateIdLeft
    /// For Backward dart: left side = boundary.PlateIdRight
    /// </summary>
    private static PlateId GetLeftPlate(BoundaryDart dart, IPlateTopologyStateView topology)
    {
        if (!topology.Boundaries.TryGetValue(dart.BoundaryId, out var boundary))
        {
            return default; // Unknown
        }

        return dart.Direction == DartDirection.Forward
            ? boundary.PlateIdLeft
            : boundary.PlateIdRight;
    }

    /// <summary>
    /// Find the index of the outside face (largest absolute signed area).
    /// </summary>
    private static int FindOutsideFaceIndex(List<(PlateId plateId, Polyline3 ring, double signedArea)> faces)
    {
        if (faces.Count == 0) return -1;

        int maxIndex = 0;
        double maxAbsArea = Math.Abs(faces[0].signedArea);

        for (int i = 1; i < faces.Count; i++)
        {
            var absArea = Math.Abs(faces[i].signedArea);
            if (absArea > maxAbsArea)
            {
                maxAbsArea = absArea;
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /// <summary>
    /// Compute signed area of a 2D polygon (shoelace formula).
    /// Positive = CCW, Negative = CW.
    /// </summary>
    private static double ComputeSignedArea(IReadOnlyList<Point3> points)
    {
        if (points.Count < 3) return 0;

        double sum = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            sum += (p1.X - p0.X) * (p1.Y + p0.Y);
        }

        return -sum / 2.0; // Negate for standard CCW=positive convention
    }

    /// <summary>
    /// Canonicalizes a ring by rotating its vertices so that the lexicographically
    /// smallest vertex (by X, then Y, then Z) comes first.
    ///
    /// This ensures deterministic ring representation regardless of where the
    /// face-walk started. The closing point (first=last) is preserved.
    ///
    /// Example: Ring [B, C, D, A, B] with min vertex A becomes [A, B, C, D, A].
    /// </summary>
    private static Polyline3 CanonicalizeRing(Polyline3 ring)
    {
        var points = ring.Points;
        if (points.Length < 3) return ring;

        // Find the lexicographically smallest vertex (excluding the closing duplicate)
        // Ring is closed: points[0] == points[^1], so we look at points[0..^1]
        int minIndex = 0;
        var minPoint = points[0];

        for (int i = 1; i < points.Length - 1; i++)
        {
            var p = points[i];
            if (CompareLexicographic(p, minPoint) < 0)
            {
                minIndex = i;
                minPoint = p;
            }
        }

        if (minIndex == 0)
        {
            return ring; // Already canonical
        }

        // Rotate: take points from minIndex to end-1, then 0 to minIndex-1, then close
        var rotated = new Point3[points.Length];
        int n = points.Length - 1; // Exclude closing point for rotation
        for (int i = 0; i < n; i++)
        {
            rotated[i] = points[(minIndex + i) % n];
        }
        rotated[n] = rotated[0]; // Close the ring

        return new Polyline3(rotated);
    }

    /// <summary>
    /// Lexicographic comparison of two 3D points: X, then Y, then Z.
    /// </summary>
    private static int CompareLexicographic(Point3 a, Point3 b)
    {
        var cmpX = a.X.CompareTo(b.X);
        if (cmpX != 0) return cmpX;

        var cmpY = a.Y.CompareTo(b.Y);
        if (cmpY != 0) return cmpY;

        return a.Z.CompareTo(b.Z);
    }
}
