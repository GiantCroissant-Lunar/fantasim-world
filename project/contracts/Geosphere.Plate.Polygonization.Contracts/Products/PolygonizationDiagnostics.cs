using System.Collections.Immutable;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

/// <summary>
/// Diagnostic result from polygonization validation.
/// RFC-V2-0041 §8.2.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PolygonizationDiagnostics(
    bool IsValid,
    ImmutableArray<OpenBoundaryDiagnostic> OpenBoundaries,
    ImmutableArray<NonManifoldJunctionDiagnostic> NonManifoldJunctions,
    ImmutableArray<DisconnectedComponentDiagnostic> DisconnectedComponents
)
{
    public static PolygonizationDiagnostics Valid()
        => new(true,
            ImmutableArray<OpenBoundaryDiagnostic>.Empty,
            ImmutableArray<NonManifoldJunctionDiagnostic>.Empty,
            ImmutableArray<DisconnectedComponentDiagnostic>.Empty);
}

/// <summary>
/// Diagnostic for an open (non-closed) boundary.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct OpenBoundaryDiagnostic(
    BoundaryId BoundaryId,
    Point3 OpenEndpoint,
    string Message
);

/// <summary>
/// Diagnostic for a non-manifold junction (invalid topology).
/// </summary>
public readonly record struct NonManifoldJunctionDiagnostic(
    JunctionId JunctionId,
    Point3 Position,
    int IncidentCount,
    string Message
);

/// <summary>
/// Diagnostic for a disconnected topology component.
/// </summary>
public readonly record struct DisconnectedComponentDiagnostic(
    int ComponentIndex,
    ImmutableArray<BoundaryId> Boundaries,
    string Message
);
