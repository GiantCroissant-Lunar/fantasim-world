# Progress — 002-unify-geometry

This document tracks what has been implemented so far on the `002-unify-geometry` branch/worktree.

## Scope

- This is **geometry operations + primitives**, not rendering.
- Outputs are engine-agnostic and intended to be consumed by downstream sampling/products (e.g., meshes, charts, Godot adapters).

## Project Layout (current)

- In `unify-maths` (reusable library):
  - `dotnet/src/UnifyGeometry.Primitives/` — stable primitives (`UGPoint2`, `UGPolyline2`, `UGPolygon2`, …)
  - `dotnet/src/UnifyGeometry.Operations/` — derived operators (algorithms)
- In `fantasim-world` (domain integration only):
  - `project/plugins/UnifyGeometry.Adapters.PlateTopology/` — interop only
  - `project/tests/UnifyGeometry.Tests/` — tests (will likely move to `unify-maths` later, except adapter-specific coverage)

## Primitives Added / In Use

- `UGPoint2`, `UGSegment2`, `UGPolyline2`, `UGPolygon2`, `UGTriangle2`
- `UGBounds2`
- `UGPolygonRegion2` (single outer + holes)
- `UGPolygonRegionSet2` (multiple regions)

## Operators Implemented

### Polyline

- Arc-length utilities:
  - point query: `PolylineArcLength.PointAtDistance`
  - slicing: `PolylineArcLength.SliceByDistance`
  - splitting: `PolylineArcLength.SplitByDistances`
- Resampling: `PolylineResample.ByPointCount`, `PolylineResample.BySpacing`
- Closest point / projection:
  - `PolylineProjectPoint.ClosestPoint`
  - `PolylineProjectPoint.ProjectPoint` (returns distance-along, segment index/t, side sign)
- Densify / simplify:
  - `PolylineDensify`
  - `PolylineSimplify`
- Offset: `PolylineOffset`
- Clipping:
  - bounds: `PolylineClip.ToBounds`
  - convex polygon: `PolylineClipConvex`
  - circle/annulus: `PolylineClipCircle`
  - half-plane: `PolylineClipHalfPlane`
- Intersections:
  - segment intersection core: `SegmentIntersection2`
  - polyline/polyline intersections: `PolylineIntersections`
  - split at intersections:
    - self: `PolylineSplitAtIntersections.SplitSelfIntersections`
    - A by B: `PolylineSplitAtIntersections.SplitByIntersections`

### Polygon

- Basic measurements / predicates:
  - `Polygon2.SignedArea`, `Polygon2.IsClockwise`, `Polygon2.ContainsPoint`
  - `PolygonCentroid.OfPolygon`
  - `PolygonValidateSimple.IsSimple`
  - `PolygonSelfIntersections.Find`
- Simplify / normalize:
  - `PolygonSimplify.RemoveCollinearAndDuplicates`
  - `PolygonNormalize.EnsureClockwise`, `PolygonNormalize.EnsureCounterClockwise`
- Triangulation:
  - `PolygonTriangulate.EarClip`
  - `PolygonRegionTriangulate.EarClip` (best-effort; hole bridging + ear clip)
- Projection / closest point:
  - `PolygonProjectPoint.ProjectPoint` / `ClosestPoint`
- Clipping:
  - `PolygonClipHalfPlane`
  - `PolygonClip.ToBounds`
  - `PolygonClipConvex`
  - `PolygonClipCircle` / annulus (approx; produces `UGPolygonRegion2` best-effort)
- Split building blocks:
  - `PolygonSplitHalfPlane.Split` (0–2 polygons)
  - `PolygonSplitAtIntersections.SplitSelfIntersections`
  - `PolygonSplitAtIntersections.SplitByIntersections(subject, cutter)`
- Boundary intersections:
  - `PolygonIntersections.IntersectBoundaries` (deduped points; overlaps → endpoints)
- Offsetting:
  - `PolygonOffset.ByDistance` (miter/bevel with miter limit)

### Polygon Booleans (derived)

- Face-level booleans:
  - `PolygonBooleanFaces.{Intersection,Union,Difference}` → `IReadOnlyList<UGPolygon2>` (simple faces)
- Region-level booleans:
  - `PolygonBooleanRegions.{Intersection,Union,Difference}` → `UGPolygonRegionSet2`
  - Implementation: build arrangement, classify boundary half-edges by left-face inclusion, trace boundary loops, assign holes to smallest containing outer.

## Known Limitations (current)

- Booleans and intersection-based splitting return **empty** on collinear/overlapping edge cases (by design for now).
- `PolygonClipCircle/Annulus` is an approximation (regular polygon sampling), not a full exact curved boolean.
- Boolean merging assumes the result can be represented as multiple `UGPolygonRegion2` (outers + holes) and does not yet attempt sophisticated hole/outer nesting beyond “assign hole to smallest containing outer”.

## Next Candidates

- `PolygonRegionSet2` utilities: `Area`, `ContainsPoint`, normalization, validation
- Boolean post-processing: boundary simplification / vertex snapping, merge adjacent regions
- More polygon building blocks: hulls, minkowski sums, robust winding/boundary classification
