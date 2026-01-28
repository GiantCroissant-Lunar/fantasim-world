using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;

/// <summary>
/// Solver for extracting plate polygons from boundary network.
/// RFC-V2-0041 §8.1.
/// </summary>
public interface IPlatePolygonizer
{
    /// <summary>
    /// Extracts all plate polygons at the given tick.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="options">Polygonization options (optional).</param>
    /// <returns>The set of plate polygons.</returns>
    /// <exception cref="PolygonizationException">
    /// Thrown when topology is invalid (open boundaries, non-manifold junctions).
    /// </exception>
    PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null);

    /// <summary>
    /// Extracts boundary-face adjacency map at the given tick.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="options">Polygonization options (optional).</param>
    /// <returns>The boundary-to-face adjacency mapping.</returns>
    BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null);

    /// <summary>
    /// Validates that the topology can be polygonized.
    /// Returns diagnostics without throwing.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="options">Polygonization options (optional).</param>
    /// <returns>Diagnostic result with validation details.</returns>
    PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null);
}
