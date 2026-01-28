using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Factory for building a boundary combinatorial map from topology state.
///
/// RFC-V2-0041 §11.1: Build cmap from boundary network.
/// </summary>
public interface IBoundaryCMapBuilder
{
    /// <summary>
    /// Builds a combinatorial map from the topology state at a given tick.
    ///
    /// The builder:
    /// 1. Creates two darts per boundary segment (Forward/Backward)
    /// 2. Links twin darts
    /// 3. Sets origin junctions
    /// 4. Computes cyclic ordering at each junction
    /// 5. Sets Next pointers based on cyclic ordering
    /// </summary>
    /// <param name="topology">The topology state view to build from.</param>
    /// <returns>The constructed combinatorial map.</returns>
    /// <exception cref="CMapBuildException">
    /// Thrown when topology is invalid for cmap construction
    /// (e.g., boundary not connected to junctions).
    /// </exception>
    IBoundaryCMap Build(IPlateTopologyStateView topology);
}
